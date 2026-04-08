using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;

namespace HomeInventory3D.Application.Services;

/// <summary>
/// Application service for container operations.
/// </summary>
public class ContainerService(
    IContainerRepository containerRepository,
    IItemRepository itemRepository)
{
    /// <summary>
    /// Returns all containers with item counts.
    /// </summary>
    public async Task<List<ContainerDto>> GetAllAsync(CancellationToken ct)
    {
        var containers = await containerRepository.GetAllAsync(ct);
        return containers.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns a single container by ID, or null if not found.
    /// </summary>
    public async Task<ContainerDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var container = await containerRepository.GetByIdAsync(id, ct);
        return container is null ? null : MapToDto(container);
    }

    /// <summary>
    /// Finds a container by its NFC tag ID.
    /// </summary>
    public async Task<ContainerDto?> GetByNfcIdAsync(string nfcId, CancellationToken ct)
    {
        var container = await containerRepository.GetByNfcIdAsync(nfcId, ct);
        return container is null ? null : MapToDto(container);
    }

    /// <summary>
    /// Returns the full 3D scene data for a container (mesh + all items).
    /// </summary>
    public async Task<SceneDto?> GetSceneAsync(Guid containerId, CancellationToken ct)
    {
        var container = await containerRepository.GetByIdAsync(containerId, ct);
        if (container is null) return null;

        var items = await itemRepository.GetByContainerIdAsync(containerId, ct);

        return new SceneDto(
            MapToDto(container),
            items.Select(ItemService.MapToDto).ToList());
    }

    /// <summary>
    /// Creates a new container.
    /// </summary>
    public async Task<ContainerDto> CreateAsync(CreateContainerDto dto, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var container = new Container
        {
            Id = Guid.CreateVersion7(),
            Name = dto.Name,
            Location = dto.Location,
            NfcId = dto.NfcId,
            QrCode = dto.QrCode,
            Description = dto.Description,
            WidthMm = dto.WidthMm,
            HeightMm = dto.HeightMm,
            DepthMm = dto.DepthMm,
            CreatedAt = now,
            UpdatedAt = now
        };

        container = await containerRepository.AddAsync(container, ct);
        return MapToDto(container);
    }

    /// <summary>
    /// Updates an existing container.
    /// </summary>
    public async Task<ContainerDto?> UpdateAsync(Guid id, UpdateContainerDto dto, CancellationToken ct)
    {
        var container = await containerRepository.GetByIdAsync(id, ct);
        if (container is null) return null;

        container.Name = dto.Name;
        container.Location = dto.Location;
        container.NfcId = dto.NfcId;
        container.QrCode = dto.QrCode;
        container.Description = dto.Description;
        container.WidthMm = dto.WidthMm;
        container.HeightMm = dto.HeightMm;
        container.DepthMm = dto.DepthMm;
        container.UpdatedAt = DateTime.UtcNow;

        await containerRepository.UpdateAsync(container, ct);
        return MapToDto(container);
    }

    /// <summary>
    /// Deletes a container and all its items.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var container = await containerRepository.GetByIdAsync(id, ct);
        if (container is null) return false;

        await containerRepository.DeleteAsync(id, ct);
        return true;
    }

    internal static ContainerDto MapToDto(Container c) => new(
        c.Id, c.Name, c.NfcId, c.QrCode, c.Location, c.Description,
        c.WidthMm, c.HeightMm, c.DepthMm,
        c.MeshFilePath, c.ThumbnailPath,
        c.Items.Count,
        c.CreatedAt, c.UpdatedAt, c.LastScannedAt);
}
