using HomeInventory3D.Domain.Enums;

namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Data for uploading a new 3D scan.
/// </summary>
public record UploadScanDto(
    Guid ContainerId,
    ScanType ScanType);
