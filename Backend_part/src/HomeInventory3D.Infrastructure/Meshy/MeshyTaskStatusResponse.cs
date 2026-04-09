using System.Text.Json.Serialization;

namespace HomeInventory3D.Infrastructure.Meshy;

internal record MeshyTaskStatusResponse(
    string Id,
    string Status,
    int Progress,
    [property: JsonPropertyName("model_urls")] MeshyModelUrls? ModelUrls);

internal record MeshyModelUrls(string? Glb);
