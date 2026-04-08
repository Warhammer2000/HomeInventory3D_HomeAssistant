namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Data required to create an item manually.
/// </summary>
public record CreateItemDto
{
    public required Guid ContainerId { get; init; }
    public required string Name { get; init; }
    public string[]? Tags { get; init; }
    public string? Description { get; init; }
    public float? PositionX { get; init; }
    public float? PositionY { get; init; }
    public float? PositionZ { get; init; }
}
