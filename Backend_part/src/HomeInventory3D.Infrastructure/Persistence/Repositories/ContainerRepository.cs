using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory3D.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the container repository.
/// </summary>
public class ContainerRepository(InventoryDbContext db) : IContainerRepository
{
    public async Task<List<Container>> GetAllAsync(CancellationToken ct)
    {
        return await db.Containers
            .Include(c => c.Items)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<Container?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Containers
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Container?> GetByNfcIdAsync(string nfcId, CancellationToken ct)
    {
        return await db.Containers
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.NfcId == nfcId, ct);
    }

    public async Task<Container> AddAsync(Container container, CancellationToken ct)
    {
        db.Containers.Add(container);
        await db.SaveChangesAsync(ct);
        return container;
    }

    public async Task UpdateAsync(Container container, CancellationToken ct)
    {
        db.Containers.Update(container);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await db.Containers.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
    }
}
