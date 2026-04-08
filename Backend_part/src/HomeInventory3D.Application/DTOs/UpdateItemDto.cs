namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Data for updating an existing item.
/// </summary>
public record UpdateItemDto
{
    public required string Name { get; init; }
    public string[]? Tags { get; init; }
    public string? Description { get; init; }
    public float? PositionX { get; init; }
    public float? PositionY { get; init; }
    public float? PositionZ { get; init; }
}
