using HomeInventory3D.Application.BackgroundJobs;
using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Services;
using HomeInventory3D.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HomeInventory3D.Api.Controllers;

/// <summary>
/// REST API for scan operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ScansController(
    ScanService scanService,
    IScanProcessingChannel processingChannel) : ControllerBase
{
    /// <summary>
    /// Upload a 3D scan file (multipart: file + containerId + scanType).
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(500_000_000)]
    public async Task<ActionResult<ScanSessionDto>> Upload(
        IFormFile file,
        [FromForm] Guid containerId,
        [FromForm] ScanType scanType,
        CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest("File is empty");

        var dto = new UploadScanDto(containerId, scanType);

        await using var stream = file.OpenReadStream();
        var session = await scanService.UploadScanAsync(dto, stream, file.FileName, ct);

        if (session is null)
            return NotFound("Container not found");

        await processingChannel.EnqueueAsync(
            new ScanProcessingRequest(session.Id, session.ContainerId), ct);

        var result = await scanService.GetByIdAsync(session.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, result);
    }

    /// <summary>
    /// List scan history for a container.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ScanSessionDto>>> GetByContainer(
        [FromQuery] Guid containerId, CancellationToken ct)
    {
        return await scanService.GetByContainerIdAsync(containerId, ct);
    }

    /// <summary>
    /// Get scan session details.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScanSessionDto>> GetById(Guid id, CancellationToken ct)
    {
        var session = await scanService.GetByIdAsync(id, ct);
        return session is null ? NotFound() : session;
    }
}
