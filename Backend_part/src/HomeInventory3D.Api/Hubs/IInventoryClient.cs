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
    Task ItemProgress(string scanId, string itemName, int index, int total, int percent, string stage);
    Task VoiceSearchResult(string itemId, string containerId, string itemName, string containerName, string answer);
}
