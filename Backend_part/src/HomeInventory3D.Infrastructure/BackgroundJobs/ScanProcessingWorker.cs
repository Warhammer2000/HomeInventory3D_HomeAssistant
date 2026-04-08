using HomeInventory3D.Application.BackgroundJobs;
using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;
using HomeInventory3D.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeInventory3D.Infrastructure.BackgroundJobs;

/// <summary>
/// Background worker that processes 3D scan files from the queue.
/// </summary>
public class ScanProcessingWorker(
    IScanProcessingChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<ScanProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scan processing worker started");

        await foreach (var request in channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessScanAsync(request, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process scan {ScanId}", request.ScanSessionId);
                await TryMarkFailedAsync(request.ScanSessionId, ex.Message, stoppingToken);
            }
        }
    }

    private async Task ProcessScanAsync(ScanProcessingRequest request, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var scanRepo = scope.ServiceProvider.GetRequiredService<IScanSessionRepository>();
        var itemRepo = scope.ServiceProvider.GetRequiredService<IItemRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var notifications = scope.ServiceProvider.GetRequiredService<IInventoryNotificationService>();

        var session = await scanRepo.GetByIdAsync(request.ScanSessionId, ct);
        if (session is null)
        {
            logger.LogWarning("Scan session {ScanId} not found", request.ScanSessionId);
            return;
        }

        session.Status = ScanStatus.Processing;
        await scanRepo.UpdateAsync(session, ct);

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 10, "Uploading", ct);

        // Phase: Parse 3D file
        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 30, "Parsing 3D file", ct);

        // TODO: AssimpNet parsing will be implemented in Phase 2
        // For now, mark as completed with zero items
        logger.LogInformation("Scan {ScanId}: 3D parsing pipeline not yet implemented", session.Id);

        session.Status = ScanStatus.Completed;
        session.ItemsDetected = 0;
        session.ItemsAdded = 0;
        session.ItemsRemoved = 0;
        await scanRepo.UpdateAsync(session, ct);

        await notifications.NotifyScanCompletedAsync(
            session.Id, session.ContainerId, 0, 0, 0, ct);
    }

    private async Task TryMarkFailedAsync(Guid scanId, string error, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var scanRepo = scope.ServiceProvider.GetRequiredService<IScanSessionRepository>();
            var notifications = scope.ServiceProvider.GetRequiredService<IInventoryNotificationService>();

            var session = await scanRepo.GetByIdAsync(scanId, ct);
            if (session is null) return;

            session.Status = ScanStatus.Failed;
            session.ErrorMessage = error;
            await scanRepo.UpdateAsync(session, ct);

            await notifications.NotifyScanFailedAsync(scanId, error, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark scan {ScanId} as failed", scanId);
        }
    }
}
