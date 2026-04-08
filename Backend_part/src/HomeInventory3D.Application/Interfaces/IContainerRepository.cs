using HomeInventory3D.Domain.Entities;

namespace HomeInventory3D.Application.Interfaces;

/// <summary>
/// Repository for container persistence operations.
/// </summary>
public interface IContainerRepository
{
    Task<List<Container>> GetAllAsync(CancellationToken ct);
    Task<Container?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Container?> GetByNfcIdAsync(string nfcId, CancellationToken ct);
    Task<Container> AddAsync(Container container, CancellationToken ct);
    Task UpdateAsync(Container container, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
