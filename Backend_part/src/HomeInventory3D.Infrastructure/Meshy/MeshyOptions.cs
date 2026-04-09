namespace HomeInventory3D.Infrastructure.Meshy;

/// <summary>
/// Configuration for Meshy AI Image-to-3D API.
/// </summary>
public class MeshyOptions
{
    public const string SectionName = "Meshy";

    public required string ApiKey { get; set; }
    public string AiModel { get; set; } = "meshy-6";
    public int PollIntervalSeconds { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 600;
}
