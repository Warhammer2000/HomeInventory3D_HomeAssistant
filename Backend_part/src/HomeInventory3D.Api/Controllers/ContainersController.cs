using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeInventory3D.Api.Controllers;

/// <summary>
/// REST API for container management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContainersController(ContainerService containerService) : ControllerBase
{
    /// <summary>
    /// List all containers.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ContainerDto>>> GetAll(CancellationToken ct)
    {
        return await containerService.GetAllAsync(ct);
    }

    /// <summary>
    /// Get container by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContainerDto>> GetById(Guid id, CancellationToken ct)
    {
        var container = await containerService.GetByIdAsync(id, ct);
        return container is null ? NotFound() : container;
    }

    /// <summary>
    /// Get 3D scene data for Unity (container mesh + all items with positions).
    /// </summary>
    [HttpGet("{id:guid}/scene")]
    public async Task<ActionResult<SceneDto>> GetScene(Guid id, CancellationToken ct)
    {
        var scene = await containerService.GetSceneAsync(id, ct);
        return scene is null ? NotFound() : scene;
    }

    /// <summary>
    /// Find container by NFC tag ID.
    /// </summary>
    [HttpGet("nfc/{nfcId}")]
    public async Task<ActionResult<ContainerDto>> GetByNfc(string nfcId, CancellationToken ct)
    {
        var container = await containerService.GetByNfcIdAsync(nfcId, ct);
        return container is null ? NotFound() : container;
    }

    /// <summary>
    /// Create a new container.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ContainerDto>> Create(CreateContainerDto dto, CancellationToken ct)
    {
        var container = await containerService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = container.Id }, container);
    }

    /// <summary>
    /// Update an existing container.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContainerDto>> Update(Guid id, UpdateContainerDto dto, CancellationToken ct)
    {
        var container = await containerService.UpdateAsync(id, dto, ct);
        return container is null ? NotFound() : container;
    }

    /// <summary>
    /// Delete a container and all its items.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await containerService.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
