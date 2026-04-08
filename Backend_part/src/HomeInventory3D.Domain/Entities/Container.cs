namespace HomeInventory3D.Domain.Entities;

/// <summary>
/// Physical container (box, chest) that holds inventory items.
/// </summary>
public class Container
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? NfcId { get; set; }
    public string? QrCode { get; set; }
    public required string Location { get; set; }
    public string? Description { get; set; }

    /// <summary>Width in millimeters.</summary>
    public float? WidthMm { get; set; }

    /// <summary>Height in millimeters.</summary>
    public float? HeightMm { get; set; }

    /// <summary>Depth in millimeters.</summary>
    public float? DepthMm { get; set; }

    public string? MeshFilePath { get; set; }
    public string? ThumbnailPath { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastScannedAt { get; set; }

    public ICollection<InventoryItem> Items { get; set; } = [];
    public ICollection<ScanSession> ScanSessions { get; set; } = [];
}
