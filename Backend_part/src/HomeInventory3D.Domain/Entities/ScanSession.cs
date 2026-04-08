using HomeInventory3D.Domain.Enums;

namespace HomeInventory3D.Domain.Entities;

/// <summary>
/// A single scan session performed on a container.
/// </summary>
public class ScanSession
{
    public Guid Id { get; set; }
    public Guid ContainerId { get; set; }
    public ScanType ScanType { get; set; }

    public string? PointCloudPath { get; set; }
    public string? DepthMapPath { get; set; }
    public string? RgbPhotoPath { get; set; }

    public int ItemsDetected { get; set; }
    public int ItemsAdded { get; set; }
    public int ItemsRemoved { get; set; }

    public ScanStatus Status { get; set; } = ScanStatus.Pending;
    public string? ErrorMessage { get; set; }

    public DateTime ScannedAt { get; set; }

    public Container Container { get; set; } = null!;
}
