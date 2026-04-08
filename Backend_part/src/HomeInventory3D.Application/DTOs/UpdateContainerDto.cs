namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Data for updating an existing container.
/// </summary>
public record UpdateContainerDto
{
    public required string Name { get; init; }
    public required string Location { get; init; }
    public string? NfcId { get; init; }
    public string? QrCode { get; init; }
    public string? Description { get; init; }
    public float? WidthMm { get; init; }
    public float? HeightMm { get; init; }
    public float? DepthMm { get; init; }
}
