using HomeInventory3D.Domain.Enums;

namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Inventory item data returned by the API.
/// </summary>
public record ItemDto(
    Guid Id,
    Guid ContainerId,
    string Name,
    string[] Tags,
    string? Description,
    float? PositionX,
    float? PositionY,
    float? PositionZ,
    float? BboxMinX,
    float? BboxMinY,
    float? BboxMinZ,
    float? BboxMaxX,
    float? BboxMaxY,
    float? BboxMaxZ,
    float? RotationX,
    float? RotationY,
    float? RotationZ,
    string? PhotoPath,
    string? MeshFilePath,
    string? ThumbnailPath,
    float? Confidence,
    RecognitionSource? RecognitionSource,
    ItemStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
