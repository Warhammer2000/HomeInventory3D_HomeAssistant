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
            Analyze this image of items in a storage container.
            For each distinct object visible, return a JSON array:
            [
              {
                "name": "item name in Russian",
                "tags": ["tag1", "tag2"],
                "description": "brief description",
                "confidence": 0.95,
                "position_x": 0.3,
                "position_y": 0.7,
                "bbox_min_x": 0.1, "bbox_min_y": 0.5,
                "bbox_max_x": 0.5, "bbox_max_y": 0.9
              }
            ]
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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        var response = await httpClient.SendAsync(httpRequest, ct);
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

        var items = JsonSerializer.Deserialize<List<RecognizedItemDto>>(textContent, JsonOptions);
        return items ?? [];
    }
}
