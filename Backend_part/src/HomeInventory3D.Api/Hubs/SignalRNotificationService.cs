using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace HomeInventory3D.Api.Hubs;

/// <summary>
/// SignalR implementation of the notification service.
/// </summary>
public class SignalRNotificationService(
    IHubContext<InventoryHub, IInventoryClient> hubContext) : IInventoryNotificationService
{
    public async Task NotifyScanProgressAsync(
        Guid scanId, Guid containerId, int progressPercent, string stage, CancellationToken ct)
    {
        await hubContext.Clients.Group(containerId.ToString())
            .ScanProgress(scanId, containerId, progressPercent, stage);
    }

    public async Task NotifyScanCompletedAsync(
        Guid scanId, Guid containerId, int itemsDetected, int itemsAdded, int itemsRemoved, CancellationToken ct)
    {
        await hubContext.Clients.Group(containerId.ToString())
            .ScanCompleted(scanId, containerId, itemsDetected, itemsAdded, itemsRemoved);
    }

    public async Task NotifyItemAddedAsync(ItemAddedDto item, CancellationToken ct)
    {
        await hubContext.Clients.Group(item.ContainerId.ToString())
            .ItemAdded(item);
    }

    public async Task NotifyItemRemovedAsync(Guid itemId, Guid containerId, CancellationToken ct)
    {
        await hubContext.Clients.Group(containerId.ToString())
            .ItemRemoved(itemId, containerId);
    }

    public async Task NotifyScanFailedAsync(Guid scanId, string errorMessage, CancellationToken ct)
    {
        await hubContext.Clients.All.ScanFailed(scanId, errorMessage);
    }

    public async Task NotifyVoiceSearchResultAsync(
        Guid itemId, Guid containerId, string itemName, string containerName, string answer, CancellationToken ct)
    {
        await hubContext.Clients.All.VoiceSearchResult(
            itemId.ToString(), containerId.ToString(), itemName, containerName, answer);
    }

    public async Task NotifyItemProgressAsync(
        Guid scanId, string itemName, int index, int total, int percent, string stage, CancellationToken ct)
    {
        await hubContext.Clients.All.ItemProgress(
            scanId.ToString(), itemName, index, total, percent, stage);
    }
}
