using Microsoft.AspNetCore.SignalR;

namespace HomeInventory3D.Api.Hubs;

/// <summary>
/// SignalR hub for real-time inventory updates.
/// Each container is a SignalR group.
/// </summary>
public class InventoryHub : Hub<IInventoryClient>
{
    /// <summary>
    /// Join a container's real-time update group.
    /// </summary>
    public async Task JoinContainer(Guid containerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, containerId.ToString());
    }

    /// <summary>
    /// Leave a container's real-time update group.
    /// </summary>
    public async Task LeaveContainer(Guid containerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, containerId.ToString());
    }
}
