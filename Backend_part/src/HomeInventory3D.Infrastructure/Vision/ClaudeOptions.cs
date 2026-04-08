namespace HomeInventory3D.Infrastructure.Vision;

/// <summary>
/// Configuration options for Claude Vision API.
/// </summary>
public class ClaudeOptions
{
    public const string SectionName = "Claude";

    public required string ApiKey { get; set; }
    public string Model { get; set; } = "claude-sonnet-4-20250514";
}
