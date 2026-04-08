using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;
using HomeInventory3D.Domain.Enums;

namespace HomeInventory3D.Application.Services;

/// <summary>
/// Application service for inventory item operations.
/// </summary>
public class ItemService(IItemRepository itemRepository)
{
    /// <summary>
    /// Returns items for a given container.
    /// </summary>
    public async Task<List<ItemDto>> GetByContainerIdAsync(Guid containerId, CancellationToken ct)
    {
        var items = await itemRepository.GetByContainerIdAsync(containerId, ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns a single item by ID.
    /// </summary>
    public async Task<ItemDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var item = await itemRepository.GetByIdAsync(id, ct);
        return item is null ? null : MapToDto(item);
    }

    /// <summary>
    /// Creates an item manually.
    /// </summary>
    public async Task<ItemDto> CreateAsync(CreateItemDto dto, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var item = new InventoryItem
        {
            Id = Guid.CreateVersion7(),
            ContainerId = dto.ContainerId,
            Name = dto.Name,
            Tags = dto.Tags ?? [],
            Description = dto.Description,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            PositionZ = dto.PositionZ,
            RecognitionSource = RecognitionSource.Manual,
            Confidence = 1.0f,
            Status = ItemStatus.Present,
            CreatedAt = now,
            UpdatedAt = now
        };

        item = await itemRepository.AddAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    public async Task<ItemDto?> UpdateAsync(Guid id, UpdateItemDto dto, CancellationToken ct)
    {
        var item = await itemRepository.GetByIdAsync(id, ct);
        if (item is null) return null;

        item.Name = dto.Name;
        item.Tags = dto.Tags ?? item.Tags;
        item.Description = dto.Description ?? item.Description;
        item.PositionX = dto.PositionX ?? item.PositionX;
        item.PositionY = dto.PositionY ?? item.PositionY;
        item.PositionZ = dto.PositionZ ?? item.PositionZ;
        item.UpdatedAt = DateTime.UtcNow;

        await itemRepository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    /// <summary>
    /// Deletes an item.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await itemRepository.GetByIdAsync(id, ct);
        if (item is null) return false;

        await itemRepository.DeleteAsync(id, ct);
        return true;
    }

    /// <summary>
    /// Full-text search across item names and tags using pg_trgm.
    /// </summary>
    public async Task<List<ItemDto>> SearchAsync(string query, int limit, CancellationToken ct)
    {
        var items = await itemRepository.SearchAsync(query, limit, ct);
        return items.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Changes item status (Present/Removed/Moved).
    /// </summary>
    public async Task<ItemDto?> UpdateStatusAsync(Guid id, UpdateItemStatusDto dto, CancellationToken ct)
    {
        var item = await itemRepository.GetByIdAsync(id, ct);
        if (item is null) return null;

        item.Status = dto.Status;
        item.UpdatedAt = DateTime.UtcNow;

        await itemRepository.UpdateAsync(item, ct);
        return MapToDto(item);
    }

    internal static ItemDto MapToDto(InventoryItem i) => new(
        i.Id, i.ContainerId, i.Name, i.Tags, i.Description,
        i.PositionX, i.PositionY, i.PositionZ,
        i.BboxMinX, i.BboxMinY, i.BboxMinZ,
        i.BboxMaxX, i.BboxMaxY, i.BboxMaxZ,
        i.RotationX, i.RotationY, i.RotationZ,
        i.PhotoPath, i.MeshFilePath, i.ThumbnailPath,
        i.Confidence, i.RecognitionSource,
        i.Status, i.CreatedAt, i.UpdatedAt);
}
