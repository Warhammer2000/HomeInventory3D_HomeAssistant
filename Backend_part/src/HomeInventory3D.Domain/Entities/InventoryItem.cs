using HomeInventory3D.Domain.Enums;

namespace HomeInventory3D.Domain.Entities;

/// <summary>
/// An item detected inside a container.
/// </summary>
public class InventoryItem
{
    public Guid Id { get; set; }
    public Guid ContainerId { get; set; }
    public required string Name { get; set; }
    public string[] Tags { get; set; } = [];
    public string? Description { get; set; }

    /// <summary>Relative position inside container (0.0–1.0).</summary>
    public float? PositionX { get; set; }
    public float? PositionY { get; set; }
    public float? PositionZ { get; set; }

    /// <summary>Bounding box min corner (relative).</summary>
    public float? BboxMinX { get; set; }
    public float? BboxMinY { get; set; }
    public float? BboxMinZ { get; set; }

    /// <summary>Bounding box max corner (relative).</summary>
    public float? BboxMaxX { get; set; }
    public float? BboxMaxY { get; set; }
    public float? BboxMaxZ { get; set; }

    /// <summary>Rotation in euler degrees.</summary>
    public float? RotationX { get; set; }
    public float? RotationY { get; set; }
    public float? RotationZ { get; set; }

    public string? PhotoPath { get; set; }
    public string? MeshFilePath { get; set; }
    public string? ThumbnailPath { get; set; }

    /// <summary>AI recognition confidence (0.0–1.0).</summary>
    public float? Confidence { get; set; }
    public RecognitionSource? RecognitionSource { get; set; }

    // Physics properties (predicted by Claude Vision)
    public float? MassKg { get; set; }
    public float? RealSizeCm { get; set; }
    public string? ColliderType { get; set; }
    public float? Bounciness { get; set; }
    public float? Friction { get; set; }
    public string? MaterialType { get; set; }
    public bool? IsFragile { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.Present;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Container Container { get; set; } = null!;
}
