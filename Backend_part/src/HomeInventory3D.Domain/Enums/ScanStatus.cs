namespace HomeInventory3D.Domain.Enums;

/// <summary>
/// Processing status of a scan session.
/// </summary>
public enum ScanStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
