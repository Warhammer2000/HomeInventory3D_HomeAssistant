using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeInventory3D.Api.Controllers;

/// <summary>
/// REST API for voice search (Home Assistant / Алиса / Google Home).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VoiceController(
    VoiceService voiceService,
    IInventoryNotificationService notifications) : ControllerBase
{
    /// <summary>
    /// Voice search — returns plain text answer + structured results (no side effects).
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<VoiceSearchResultDto>> Search(
        [FromQuery] string q, CancellationToken ct)
    {
        return await voiceService.SearchAsync(q, ct);
    }

    /// <summary>
    /// Voice search + SignalR broadcast — used by Home Assistant / voice assistants.
    /// Returns the answer AND notifies Unity to navigate to the found item.
    /// </summary>
    [HttpPost("search-and-notify")]
    public async Task<ActionResult<VoiceSearchResultDto>> SearchAndNotify(
        [FromBody] VoiceSearchNotifyRequestDto request, CancellationToken ct)
    {
        var result = await voiceService.SearchAsync(request.Query, ct);

        if (result.Items.Count > 0)
        {
            var first = result.Items[0];
            await notifications.NotifyVoiceSearchResultAsync(
                first.Id, first.ContainerId, first.Name,
                first.ContainerName, result.Answer, ct);
        }

        return result;
    }
}
