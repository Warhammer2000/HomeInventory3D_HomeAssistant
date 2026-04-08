using HomeInventory3D.Domain.Entities;

namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Repository for scan session persistence operations.
/// </summary>
public interface IScanSessionRepository
{
    Task<List<ScanSession>> GetByContainerIdAsync(Guid containerId, CancellationToken ct);
    Task<ScanSession?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ScanSession> AddAsync(ScanSession session, CancellationToken ct);
    Task UpdateAsync(ScanSession session, CancellationToken ct);
}
