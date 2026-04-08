using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory3D.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the item repository with pg_trgm search.
/// </summary>
public class ItemRepository(InventoryDbContext db) : IItemRepository
{
    public async Task<List<InventoryItem>> GetByContainerIdAsync(Guid containerId, CancellationToken ct)
    {
        return await db.InventoryItems
            .Where(i => i.ContainerId == containerId)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct)
    {
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task AddRangeAsync(IEnumerable<InventoryItem> items, CancellationToken ct)
    {
        db.InventoryItems.AddRange(items);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(InventoryItem item, CancellationToken ct)
    {
        db.InventoryItems.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await db.InventoryItems.Where(i => i.Id == id).ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Full-text search using PostgreSQL pg_trgm similarity.
    /// </summary>
    public async Task<List<InventoryItem>> SearchAsync(string query, int limit, CancellationToken ct)
    {
        return await db.InventoryItems
            .Include(i => i.Container)
            .Where(i => EF.Functions.ILike(i.Name, $"%{query}%")
                || i.Tags.Any(t => EF.Functions.ILike(t, $"%{query}%")))
            .OrderBy(i => EF.Functions.TrigramsWordSimilarityDistance(i.Name, query))
            .Take(limit)
            .ToListAsync(ct);
    }
}
