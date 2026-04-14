using System.Net.Http.Json;
using System.Text.Json;
using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeInventory3D.Infrastructure.Vision;

/// <summary>
/// Claude Vision API implementation for object recognition.
/// </summary>
public class ClaudeVisionService(
    HttpClient httpClient,
    IOptions<ClaudeOptions> options,
    ILogger<ClaudeVisionService> logger) : IVisionRecognitionService
{
    private readonly ClaudeOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<List<RecognizedItemDto>> RecognizeItemsAsync(
        Stream photo, string? containerContext, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms, ct);
        var base64 = Convert.ToBase64String(ms.ToArray());

        var prompt = """
            Analyze this image and identify each SEPARATE distinct physical object that can be PICKED UP and PLACED into a box.

            IMPORTANT RULES:
            - ONLY include objects that are portable items (things you can hold in your hands)
            - SKIP surfaces, floors, carpets, rugs, mats, tables, walls, backgrounds
            - SKIP objects that are part of the room/furniture (unless they are small portable items ON the furniture)
            - Each object MUST have its own TIGHT bounding box covering ONLY that single object
            - Do NOT group multiple objects into one bounding box — each item separate
            - bbox must be TIGHT — minimal rectangle around the object with NO extra space

            BOUNDING BOX FORMAT:
            - bbox_min_x/y = top-left corner, bbox_max_x/y = bottom-right corner
            - Coordinates are 0.0 to 1.0 relative to image dimensions
            - TIGHT means the bbox edges touch the object edges

            For the Meshy AI 3D generator, provide a DETAILED shape description in the "description" field:
            - Include the EXACT 3D shape (cylinder, rectangular box, round bottle, etc.)
            - Include color and material
            - Example: "Высокая черно-зеленая алюминиевая банка цилиндрической формы с крышкой"

            JSON format:
            [
              {
                "name": "item name in Russian",
                "tags": ["tag1", "tag2"],
                "description": "DETAILED shape + color + material description for 3D generation",
                "confidence": 0.95,
                "position_x": 0.5,
                "position_y": 0.5,
                "bbox_min_x": 0.1, "bbox_min_y": 0.2,
                "bbox_max_x": 0.4, "bbox_max_y": 0.7,
                "physics": {
                  "mass_kg": 0.3,
                  "real_size_cm": 12.0,
                  "collider_type": "capsule",
                  "bounciness": 0.1,
                  "friction": 0.6,
                  "is_fragile": false,
                  "material_type": "metal"
                }
              }
            ]

            Physics guidelines:
            - mass_kg: realistic weight (mug ~0.3, can ~0.35, phone ~0.2, bottle ~0.5, book ~0.4)
            - real_size_cm: LARGEST dimension in cm (mug ~10, energy can ~17, phone ~15, bottle ~25)
            - collider_type: "box" for rectangular, "sphere" for round, "capsule" for tall cylindrical (cans, bottles)
            - bounciness: 0-1 (metal=0.3, glass=0.1, plastic=0.2, rubber=0.8)
            - friction: 0-1 (metal=0.4, glass=0.3, rubber=0.9, fabric=0.7)
            - is_fragile: true for glass, ceramics, electronics
            - material_type: "metal", "wood", "plastic", "glass", "rubber", "ceramic", "fabric", "paper", "electronics"

            Return ONLY valid JSON array, no markdown.
            """;

        if (containerContext is not null)
        {
            prompt += $"\nContainer context: {containerContext}";
        }

        var request = new
        {
            model = _options.Model,
            max_tokens = 4096,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = "image/jpeg",
                                data = base64
                            }
                        },
                        new { type = "text", text = prompt }
                    }
                }
            }
        };

        // Retry up to 3 times on 429/529 (rate limit / overloaded)
        HttpResponseMessage response = null!;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
            {
                logger.LogWarning("Claude API retry {Attempt}/3 after {Delay}s...", attempt + 1, attempt * 3);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 3), ct);
            }

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = JsonContent.Create(request)
            };
            req.Headers.Add("x-api-key", _options.ApiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");

            response = await httpClient.SendAsync(req, ct);
            if ((int)response.StatusCode != 429 && (int)response.StatusCode != 529)
                break;
        }
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var textContent = responseJson
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(textContent))
        {
            logger.LogWarning("Claude Vision returned empty response");
            return [];
        }

        // Strip markdown code fences if Claude wraps response in ```json ... ```
        var json = textContent.Trim();
        if (json.StartsWith("```"))
        {
            var firstNewline = json.IndexOf('\n');
            if (firstNewline > 0)
                json = json[(firstNewline + 1)..];
            if (json.EndsWith("```"))
                json = json[..^3];
            json = json.Trim();
        }

        logger.LogInformation("Claude Vision response ({Length} chars): {Json}",
            json.Length, json.Length > 500 ? json[..500] + "..." : json);

        try
        {
            var items = JsonSerializer.Deserialize<List<RecognizedItemDto>>(json, JsonOptions);
            return items ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Claude Vision JSON. Raw: {Json}", json);
            throw;
        }
    }
}
