using HomeInventory3D.Application.DTOs;

namespace HomeInventory3D.Api.Hubs;

/// <summary>
/// Strongly-typed SignalR client interface for inventory events.
/// </summary>
public interface IInventoryClient
{
    Task ScanProgress(Guid scanId, Guid containerId, int progressPercent, string stage);
    Task ScanCompleted(Guid scanId, Guid containerId, int itemsDetected, int itemsAdded, int itemsRemoved);
    Task ItemAdded(ItemAddedDto item);
    Task ItemRemoved(Guid itemId, Guid containerId);
    Task ScanFailed(Guid scanId, string errorMessage);
}
