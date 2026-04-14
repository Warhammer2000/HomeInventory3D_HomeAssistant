using HomeInventory3D.Application.DTOs;

namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Abstraction for real-time notifications (SignalR).
/// </summary>
public interface IInventoryNotificationService
{
    Task NotifyScanProgressAsync(Guid scanId, Guid containerId, int progressPercent, string stage, CancellationToken ct);
    Task NotifyScanCompletedAsync(Guid scanId, Guid containerId, int itemsDetected, int itemsAdded, int itemsRemoved, CancellationToken ct);
    Task NotifyItemAddedAsync(ItemAddedDto item, CancellationToken ct);
    Task NotifyItemRemovedAsync(Guid itemId, Guid containerId, CancellationToken ct);
    Task NotifyScanFailedAsync(Guid scanId, string errorMessage, CancellationToken ct);
    Task NotifyItemProgressAsync(Guid scanId, string itemName, int index, int total, int percent, string stage, CancellationToken ct);
    Task NotifyVoiceSearchResultAsync(Guid itemId, Guid containerId, string itemName, string containerName, string answer, CancellationToken ct);
}
