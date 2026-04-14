using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using HomeInventory3D.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeInventory3D.Infrastructure.Meshy;

/// <summary>
/// Meshy AI implementation: sends a photo, polls until 3D model is ready, downloads GLB.
/// </summary>
public class MeshyImageTo3DService(
    HttpClient httpClient,
    IOptions<MeshyOptions> options,
    ILogger<MeshyImageTo3DService> logger) : IImageTo3DService
{
    private const string BaseUrl = "https://api.meshy.ai/openapi/v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public async Task<Stream> GenerateModelAsync(Stream imageStream, string? objectPrompt,
        IProgress<int>? progress, CancellationToken ct)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Meshy API key is not configured");

        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);
        var base64 = Convert.ToBase64String(ms.ToArray());
        var dataUri = $"data:image/jpeg;base64,{base64}";

        progress?.Report(5);
        logger.LogInformation("Submitting photo to Meshy AI ({Bytes} bytes, prompt: {Prompt})", ms.Length, objectPrompt ?? "none");

        var taskId = await CreateTaskAsync(dataUri, objectPrompt, apiKey, ct);
        logger.LogInformation("Meshy task created: {TaskId}", taskId);
        progress?.Report(10);

        var glbUrl = await PollForCompletionAsync(taskId, apiKey, progress, ct);
        logger.LogInformation("Meshy task completed. GLB URL: {Url}", glbUrl);
        progress?.Report(95);

        var glbStream = await DownloadGlbAsync(glbUrl, ct);
        progress?.Report(100);
        return glbStream;
    }

    private async Task<string> CreateTaskAsync(string imageDataUri, string? objectPrompt, string apiKey, CancellationToken ct)
    {
        var requestDict = new Dictionary<string, object>
        {
            ["image_url"] = imageDataUri,
            ["should_remesh"] = true,
            ["should_texture"] = true,
            ["enable_pbr"] = true,
            ["ai_model"] = options.Value.AiModel
        };

        if (!string.IsNullOrWhiteSpace(objectPrompt))
            requestDict["object_prompt"] = objectPrompt;

        var request = requestDict;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/image-to-3d")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await httpClient.SendAsync(httpRequest, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("Meshy create task response: {StatusCode} {Body}", (int)response.StatusCode, responseBody);
        response.EnsureSuccessStatusCode();

        // Meshy returns {"result": "task_id"} format
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Try "result" field first (Meshy v1 format), then "id"
        if (root.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.String)
            return resultProp.GetString()!;
        if (root.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
            return idProp.GetString()!;

        throw new InvalidOperationException($"Meshy returned unexpected format: {responseBody}");
    }

    private async Task<string> PollForCompletionAsync(string taskId, string apiKey, IProgress<int>? progress, CancellationToken ct)
    {
        var pollInterval = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);
        var timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(pollInterval, ct);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/image-to-3d/{taskId}");
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await httpClient.SendAsync(httpRequest, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var statusStr = root.TryGetProperty("status", out var sp) ? sp.GetString() ?? "" : "";
            var meshProgress = root.TryGetProperty("progress", out var pp) ? pp.GetInt32() : 0;

            logger.LogInformation("Meshy task {TaskId}: {Status} ({Progress}%)", taskId, statusStr, meshProgress);

            // Report Meshy progress mapped to 10-90 range
            progress?.Report(10 + (int)(meshProgress * 0.8));

            switch (statusStr.ToUpperInvariant())
            {
                case "SUCCEEDED":
                    // Try model_urls.glb
                    string? glbUrl = null;
                    if (root.TryGetProperty("model_urls", out var urls) && urls.TryGetProperty("glb", out var glb))
                        glbUrl = glb.GetString();

                    if (string.IsNullOrWhiteSpace(glbUrl))
                    {
                        logger.LogWarning("Meshy SUCCEEDED but no GLB URL. Full response: {Body}", body);
                        throw new InvalidOperationException("Meshy task succeeded but no GLB URL in response");
                    }
                    return glbUrl;

                case "FAILED":
                case "CANCELED":
                    logger.LogWarning("Meshy task {Status}. Response: {Body}", statusStr, body);
                    throw new InvalidOperationException($"Meshy task {taskId} {statusStr}");
            }
        }

        throw new TimeoutException($"Meshy task {taskId} did not complete within {timeout.TotalMinutes} minutes");
    }

    private async Task<Stream> DownloadGlbAsync(string url, CancellationToken ct)
    {
        var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var memStream = new MemoryStream();
        await response.Content.CopyToAsync(memStream, ct);
        memStream.Position = 0;

        logger.LogInformation("Downloaded GLB: {Bytes} bytes", memStream.Length);
        return memStream;
    }
}
