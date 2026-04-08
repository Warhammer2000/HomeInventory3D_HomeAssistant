using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeInventory3D.Api.Controllers;

/// <summary>
/// REST API for inventory item management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ItemsController(ItemService itemService) : ControllerBase
{
    /// <summary>
    /// List items in a container.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ItemDto>>> GetByContainer(
        [FromQuery] Guid containerId, CancellationToken ct)
    {
        return await itemService.GetByContainerIdAsync(containerId, ct);
    }

    /// <summary>
    /// Get item by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ItemDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await itemService.GetByIdAsync(id, ct);
        return item is null ? NotFound() : item;
    }

    /// <summary>
    /// Full-text search across items (pg_trgm).
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<ItemDto>>> Search(
        [FromQuery] string q, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        return await itemService.SearchAsync(q, limit, ct);
    }

    /// <summary>
    /// Create an item manually.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ItemDto>> Create(CreateItemDto dto, CancellationToken ct)
    {
        var item = await itemService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    /// <summary>
    /// Update an existing item.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ItemDto>> Update(Guid id, UpdateItemDto dto, CancellationToken ct)
    {
        var item = await itemService.UpdateAsync(id, dto, ct);
        return item is null ? NotFound() : item;
    }

    /// <summary>
    /// Change item status (Present/Removed/Moved).
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ItemDto>> UpdateStatus(
        Guid id, UpdateItemStatusDto dto, CancellationToken ct)
    {
        var item = await itemService.UpdateStatusAsync(id, dto, ct);
        return item is null ? NotFound() : item;
    }

    /// <summary>
    /// Delete an item.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await itemService.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
