using HomeInventory3D.Domain.Entities;

namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Repository for inventory item persistence operations.
/// </summary>
public interface IItemRepository
{
    Task<List<InventoryItem>> GetByContainerIdAsync(Guid containerId, CancellationToken ct);
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<InventoryItem> items, CancellationToken ct);
    Task UpdateAsync(InventoryItem item, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<List<InventoryItem>> SearchAsync(string query, int limit, CancellationToken ct);
}
