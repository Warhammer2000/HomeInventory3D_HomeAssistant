namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// SignalR event data when an item is added during scan processing.
/// </summary>
public record ItemAddedDto(
    Guid Id,
    Guid ContainerId,
    string Name,
    string[] Tags,
    float? PositionX,
    float? PositionY,
    float? PositionZ,
    float? RotationX,
    float? RotationY,
    float? RotationZ,
    float? BboxMinX,
    float? BboxMinY,
    float? BboxMinZ,
    float? BboxMaxX,
    float? BboxMaxY,
    float? BboxMaxZ,
    string? MeshUrl,
    string? ThumbnailUrl,
    float? Confidence,
    float? MassKg,
    float? RealSizeCm,
    string? ColliderType,
    float? Bounciness,
    float? Friction,
    string? MaterialType,
    bool? IsFragile);
