using HomeInventory3D.Domain.Enums;

namespace HomeInventory3D.Application.DTOs;

/// <summary>
/// Scan session data returned by the API.
/// </summary>
public record ScanSessionDto(
    Guid Id,
    Guid ContainerId,
    ScanType ScanType,
    int ItemsDetected,
    int ItemsAdded,
    int ItemsRemoved,
    ScanStatus Status,
    string? ErrorMessage,
    DateTime ScannedAt);
