namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Result of AI vision recognition for a single object, including predicted physics.
/// </summary>
public record RecognizedItemDto(
    string Name,
    string[] Tags,
    string? Description,
    float Confidence,
    float? PositionX,
    float? PositionY,
    float? BboxMinX,
    float? BboxMinY,
    float? BboxMaxX,
    float? BboxMaxY,
    PhysicsPropertiesDto? Physics);
