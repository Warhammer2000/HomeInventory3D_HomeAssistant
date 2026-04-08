using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;
using HomeInventory3D.Domain.Enums;

namespace HomeInventory3D.Application.Services;

/// <summary>
/// Application service for scan session operations.
/// </summary>
public class ScanService(
    IScanSessionRepository scanSessionRepository,
    IContainerRepository containerRepository,
    IFileStorageService fileStorageService)
{
    /// <summary>
    /// Returns scan history for a container.
    /// </summary>
    public async Task<List<ScanSessionDto>> GetByContainerIdAsync(Guid containerId, CancellationToken ct)
    {
        var sessions = await scanSessionRepository.GetByContainerIdAsync(containerId, ct);
        return sessions.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Returns a single scan session by ID.
    /// </summary>
    public async Task<ScanSessionDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var session = await scanSessionRepository.GetByIdAsync(id, ct);
        return session is null ? null : MapToDto(session);
    }

    /// <summary>
    /// Handles scan file upload: validates, saves file, creates session, returns session for background processing.
    /// </summary>
    public async Task<ScanSession?> UploadScanAsync(
        UploadScanDto dto, Stream fileStream, string fileName, CancellationToken ct)
    {
        var container = await containerRepository.GetByIdAsync(dto.ContainerId, ct);
        if (container is null) return null;

        var folder = $"scans/{dto.ContainerId}";
        var savedPath = await fileStorageService.SaveAsync(fileStream, folder, fileName, ct);

        var session = new ScanSession
        {
            Id = Guid.CreateVersion7(),
            ContainerId = dto.ContainerId,
            ScanType = dto.ScanType,
            PointCloudPath = savedPath,
            Status = ScanStatus.Pending,
            ScannedAt = DateTime.UtcNow
        };

        session = await scanSessionRepository.AddAsync(session, ct);

        container.LastScannedAt = session.ScannedAt;
        container.UpdatedAt = DateTime.UtcNow;
        await containerRepository.UpdateAsync(container, ct);

        return session;
    }

    internal static ScanSessionDto MapToDto(ScanSession s) => new(
        s.Id, s.ContainerId, s.ScanType,
        s.ItemsDetected, s.ItemsAdded, s.ItemsRemoved,
        s.Status, s.ErrorMessage, s.ScannedAt);
}
