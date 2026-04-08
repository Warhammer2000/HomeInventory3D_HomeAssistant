namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Data required to create a new container.
/// </summary>
public record CreateContainerDto
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
