namespace HomeInventory3D.Infrastructure.Storage;

/// <summary>
/// Configuration options for file storage.
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    public required string BasePath { get; set; }
    public required string BaseUrl { get; set; }
}
