namespace HomeInventory3D.Application.BackgroundJobs;

/// <summary>
/// Message queued for background scan processing.
/// </summary>
public record ScanProcessingRequest(Guid ScanSessionId, Guid ContainerId);
