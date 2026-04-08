using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeInventory3D.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the scan session repository.
/// </summary>
public class ScanSessionRepository(InventoryDbContext db) : IScanSessionRepository
{
    public async Task<List<ScanSession>> GetByContainerIdAsync(Guid containerId, CancellationToken ct)
    {
        return await db.ScanSessions
            .Where(s => s.ContainerId == containerId)
            .OrderByDescending(s => s.ScannedAt)
            .ToListAsync(ct);
    }

    public async Task<ScanSession?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.ScanSessions
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<ScanSession> AddAsync(ScanSession session, CancellationToken ct)
    {
        db.ScanSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task UpdateAsync(ScanSession session, CancellationToken ct)
    {
        db.ScanSessions.Update(session);
        await db.SaveChangesAsync(ct);
    }
}
