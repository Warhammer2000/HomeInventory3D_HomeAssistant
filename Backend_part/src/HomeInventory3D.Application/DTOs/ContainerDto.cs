namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Container data returned by the API.
/// </summary>
public record ContainerDto(
    Guid Id,
    string Name,
    string? NfcId,
    string? QrCode,
    string Location,
    string? Description,
    float? WidthMm,
    float? HeightMm,
    float? DepthMm,
    string? MeshFilePath,
    string? ThumbnailPath,
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastScannedAt);
