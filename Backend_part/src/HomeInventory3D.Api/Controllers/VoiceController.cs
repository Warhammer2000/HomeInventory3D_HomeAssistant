using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeInventory3D.Api.Controllers;

/// <summary>
/// REST API for voice search (Home Assistant / Алиса).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VoiceController(VoiceService voiceService) : ControllerBase
{
    /// <summary>
    /// Voice search — returns plain text answer + structured results.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<VoiceSearchResultDto>> Search(
        [FromQuery] string q, CancellationToken ct)
    {
        return await voiceService.SearchAsync(q, ct);
    }
}
