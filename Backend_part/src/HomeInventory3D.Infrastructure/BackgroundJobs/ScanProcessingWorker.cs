using HomeInventory3D.Application.BackgroundJobs;
using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;
using HomeInventory3D.Domain.Enums;
using HomeInventory3D.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeInventory3D.Infrastructure.BackgroundJobs;

/// <summary>
/// Background worker that processes 3D scan files: parse → segment → export → recognize → notify.
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
        var containerRepo = scope.ServiceProvider.GetRequiredService<IContainerRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var notifications = scope.ServiceProvider.GetRequiredService<IInventoryNotificationService>();
        var meshProcessor = scope.ServiceProvider.GetRequiredService<IMeshProcessingService>();
        var glbExporter = scope.ServiceProvider.GetRequiredService<IGlbExportService>();
        var thumbnailService = scope.ServiceProvider.GetRequiredService<IThumbnailService>();
        var visionService = scope.ServiceProvider.GetRequiredService<IVisionRecognitionService>();
        var storageOptions = scope.ServiceProvider.GetRequiredService<IOptions<StorageOptions>>();

        // Step 1: Load session
        var session = await scanRepo.GetByIdAsync(request.ScanSessionId, ct);
        if (session is null)
        {
            logger.LogWarning("Scan session {ScanId} not found", request.ScanSessionId);
            return;
        }

        session.Status = ScanStatus.Processing;
        await scanRepo.UpdateAsync(session, ct);

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 10, "Validating", ct);

        // Step 2: Validate file
        var fullPath = Path.Combine(storageOptions.Value.BasePath, session.PointCloudPath!);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Scan file not found on disk", fullPath);

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 15, "Validated", ct);

        // Branch: Photo mode uses Meshy AI pipeline
        if (session.ScanType == ScanType.Photo)
        {
            await ProcessPhotoScanAsync(session, fullPath, scope, ct);
            return;
        }

        // Step 3: Parse 3D file (Lidar/Manual/Automatic mode)
        var result = await meshProcessor.ProcessFileAsync(fullPath, ct);

        if (result.Meshes.Count == 0)
        {
            logger.LogWarning("No meshes found in scan {ScanId}", session.Id);
            session.Status = ScanStatus.Completed;
            session.ItemsDetected = 0;
            await scanRepo.UpdateAsync(session, ct);
            await notifications.NotifyScanCompletedAsync(session.Id, session.ContainerId, 0, 0, 0, ct);
            return;
        }

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 30, $"Parsed {result.Meshes.Count} objects", ct);

        // Load container for Vision context
        var container = await containerRepo.GetByIdAsync(session.ContainerId, ct);

        // Step 4: Process each mesh
        var itemsDetected = result.Meshes.Count;
        var itemsAdded = 0;
        var failedItems = new List<string>();

        for (var i = 0; i < result.Meshes.Count; i++)
        {
            var meshData = result.Meshes[i];

            try
            {
                ct.ThrowIfCancellationRequested();

                var itemId = Guid.CreateVersion7();

                // 4a: Normalize positions to [0,1]
                var bounds = result.SceneBounds;
                var normCenterX = (meshData.CenterX - bounds.MinX) / bounds.SizeX;
                var normCenterY = (meshData.CenterY - bounds.MinY) / bounds.SizeY;
                var normCenterZ = (meshData.CenterZ - bounds.MinZ) / bounds.SizeZ;
                var normBboxMinX = (meshData.BboxMinX - bounds.MinX) / bounds.SizeX;
                var normBboxMinY = (meshData.BboxMinY - bounds.MinY) / bounds.SizeY;
                var normBboxMinZ = (meshData.BboxMinZ - bounds.MinZ) / bounds.SizeZ;
                var normBboxMaxX = (meshData.BboxMaxX - bounds.MinX) / bounds.SizeX;
                var normBboxMaxY = (meshData.BboxMaxY - bounds.MinY) / bounds.SizeY;
                var normBboxMaxZ = (meshData.BboxMaxZ - bounds.MinZ) / bounds.SizeZ;

                // 4b: Export individual mesh as GLB
                await using var glbStream = await glbExporter.ExportMeshAsync(
                    meshData.Vertices, meshData.Indices, meshData.Name, ct);
                var glbPath = await fileStorage.SaveAsync(
                    glbStream, $"meshes/{session.ContainerId}", $"{itemId}.glb", ct);

                // 4c: Render thumbnail
                await using var thumbStream = await thumbnailService.RenderTopDownAsync(
                    meshData.Vertices, meshData.Indices, 256, 256, ct);
                var thumbPath = await fileStorage.SaveAsync(
                    thumbStream, $"thumbnails/{session.ContainerId}", $"{itemId}.png", ct);

                // 4d: Claude Vision recognition
                var label = await RecognizeWithFallbackAsync(
                    visionService, fileStorage, thumbPath, container?.Name, i, ct);

                // 4e: Create InventoryItem
                var now = DateTime.UtcNow;
                var item = new InventoryItem
                {
                    Id = itemId,
                    ContainerId = session.ContainerId,
                    Name = label.Name,
                    Tags = label.Tags,
                    Description = label.Description,
                    PositionX = normCenterX,
                    PositionY = normCenterY,
                    PositionZ = normCenterZ,
                    BboxMinX = normBboxMinX,
                    BboxMinY = normBboxMinY,
                    BboxMinZ = normBboxMinZ,
                    BboxMaxX = normBboxMaxX,
                    BboxMaxY = normBboxMaxY,
                    BboxMaxZ = normBboxMaxZ,
                    MeshFilePath = glbPath,
                    ThumbnailPath = thumbPath,
                    Confidence = label.Confidence,
                    RecognitionSource = RecognitionSource.ClaudeVision,
                    Status = ItemStatus.Present,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await itemRepo.AddAsync(item, ct);

                // 4f: SignalR notification IMMEDIATELY
                var addedDto = new ItemAddedDto(
                    item.Id, item.ContainerId, item.Name, item.Tags,
                    item.PositionX, item.PositionY, item.PositionZ,
                    item.RotationX, item.RotationY, item.RotationZ,
                    item.BboxMinX, item.BboxMinY, item.BboxMinZ,
                    item.BboxMaxX, item.BboxMaxY, item.BboxMaxZ,
                    fileStorage.GetUrl(glbPath),
                    fileStorage.GetUrl(thumbPath),
                    item.Confidence);

                await notifications.NotifyItemAddedAsync(addedDto, ct);

                itemsAdded++;

                var progressPercent = 30 + (int)(60.0 * (i + 1) / result.Meshes.Count);
                await notifications.NotifyScanProgressAsync(
                    session.Id, session.ContainerId, progressPercent,
                    $"Recognized: {item.Name}", ct);

                logger.LogInformation("Scan {ScanId}: item {Index}/{Total} — {Name} (confidence: {Confidence:P0})",
                    session.Id, i + 1, result.Meshes.Count, item.Name, item.Confidence);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process mesh {MeshName} in scan {ScanId}",
                    meshData.Name, session.Id);
                failedItems.Add(meshData.Name);
            }
        }

        // Step 5: Finalize session
        session.Status = ScanStatus.Completed;
        session.ItemsDetected = itemsDetected;
        session.ItemsAdded = itemsAdded;
        session.ItemsRemoved = 0;

        if (failedItems.Count > 0)
            session.ErrorMessage = $"Partial: {failedItems.Count} mesh(es) failed — {string.Join(", ", failedItems)}";

        await scanRepo.UpdateAsync(session, ct);

        await notifications.NotifyScanCompletedAsync(
            session.Id, session.ContainerId,
            itemsDetected, itemsAdded, 0, ct);

        logger.LogInformation("Scan {ScanId} completed: {Detected} detected, {Added} added, {Failed} failed",
            session.Id, itemsDetected, itemsAdded, failedItems.Count);
    }

    private async Task ProcessPhotoScanAsync(
        ScanSession session, string photoPath, AsyncServiceScope scope, CancellationToken ct)
    {
        var scanRepo = scope.ServiceProvider.GetRequiredService<IScanSessionRepository>();
        var itemRepo = scope.ServiceProvider.GetRequiredService<IItemRepository>();
        var containerRepo = scope.ServiceProvider.GetRequiredService<IContainerRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var notifications = scope.ServiceProvider.GetRequiredService<IInventoryNotificationService>();
        var visionService = scope.ServiceProvider.GetRequiredService<IVisionRecognitionService>();
        var imageTo3D = scope.ServiceProvider.GetRequiredService<IImageTo3DService>();
        var storageOpts = scope.ServiceProvider.GetRequiredService<IOptions<StorageOptions>>();

        var container = await containerRepo.GetByIdAsync(session.ContainerId, ct);

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 20, "Analyzing photo (AI + 3D generation)", ct);

        // Read photo bytes once
        var photoBytes = await File.ReadAllBytesAsync(photoPath, ct);

        // Step 1: Claude Vision → name, tags, description
        RecognizedItemDto label;
        using (var visionStream = new MemoryStream(photoBytes))
        {
            label = await SafeVisionRecognizeAsync(visionService, visionStream, container?.Name, ct);
        }

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 30, $"Recognized: {label.Name}. Generating 3D model...", ct);

        logger.LogInformation("Photo recognized as: {Name} (confidence: {Confidence}). Sending to Meshy with prompt.",
            label.Name, label.Confidence);

        // Step 2: Meshy AI → 3D model, using Claude's label as object_prompt
        var meshyPrompt = label.Confidence > 0
            ? $"{label.Name}. {label.Description ?? ""}"
            : null;

        Stream? glbStream;
        using (var meshyStream = new MemoryStream(photoBytes))
        {
            glbStream = await SafeMeshyGenerateAsync(imageTo3D, meshyStream, meshyPrompt, ct);
        }

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 85, glbStream is not null ? "3D model ready" : "3D generation failed, saving without model", ct);

        var itemId = Guid.CreateVersion7();
        string? glbPath = null;

        // Save GLB if Meshy succeeded
        if (glbStream is not null)
        {
            glbPath = await fileStorage.SaveAsync(
                glbStream, $"meshes/{session.ContainerId}", $"{itemId}.glb", ct);
            await glbStream.DisposeAsync();
            logger.LogInformation("Meshy GLB saved: {Path}", glbPath);
        }

        // Save photo as thumbnail
        using var thumbSource = new MemoryStream(photoBytes);
        var thumbPath = await fileStorage.SaveAsync(
            thumbSource, $"thumbnails/{session.ContainerId}", $"{itemId}.jpg", ct);

        // Create InventoryItem
        var now = DateTime.UtcNow;
        var item = new InventoryItem
        {
            Id = itemId,
            ContainerId = session.ContainerId,
            Name = label.Name,
            Tags = label.Tags,
            Description = label.Description,
            PositionX = 0.5f,
            PositionY = 0f,
            PositionZ = 0.5f,
            MeshFilePath = glbPath,
            ThumbnailPath = thumbPath,
            PhotoPath = session.PointCloudPath,
            Confidence = label.Confidence,
            RecognitionSource = glbPath is not null
                ? RecognitionSource.MeshyAI
                : RecognitionSource.ClaudeVision,
            Status = ItemStatus.Present,
            CreatedAt = now,
            UpdatedAt = now
        };

        await itemRepo.AddAsync(item, ct);

        // SignalR notification
        var addedDto = new ItemAddedDto(
            item.Id, item.ContainerId, item.Name, item.Tags,
            item.PositionX, item.PositionY, item.PositionZ,
            null, null, null,
            null, null, null,
            null, null, null,
            glbPath is not null ? fileStorage.GetUrl(glbPath) : null,
            fileStorage.GetUrl(thumbPath),
            item.Confidence);

        await notifications.NotifyItemAddedAsync(addedDto, ct);

        // Finalize
        session.Status = ScanStatus.Completed;
        session.ItemsDetected = 1;
        session.ItemsAdded = 1;
        await scanRepo.UpdateAsync(session, ct);

        await notifications.NotifyScanCompletedAsync(
            session.Id, session.ContainerId, 1, 1, 0, ct);

        logger.LogInformation("Photo scan {ScanId} completed: {Name} (GLB: {HasGlb})",
            session.Id, item.Name, glbPath is not null);
    }

    private async Task<RecognizedItemDto> SafeVisionRecognizeAsync(
        IVisionRecognitionService visionService, MemoryStream photoStream,
        string? containerName, CancellationToken ct)
    {
        try
        {
            var results = await visionService.RecognizeItemsAsync(photoStream, containerName, ct);
            if (results.Count > 0) return results[0];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Vision recognition failed for photo, using fallback");
        }

        return new RecognizedItemDto("Photo Object", [], null, 0f, null, null, null, null, null, null);
    }

    private async Task<Stream?> SafeMeshyGenerateAsync(
        IImageTo3DService imageTo3D, MemoryStream photoStream, string? objectPrompt, CancellationToken ct)
    {
        try
        {
            return await imageTo3D.GenerateModelAsync(photoStream, objectPrompt, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Meshy 3D generation failed");
            return null;
        }
    }

    private async Task<RecognizedItemDto> RecognizeWithFallbackAsync(
        IVisionRecognitionService visionService,
        IFileStorageService fileStorage,
        string thumbnailPath,
        string? containerName,
        int meshIndex,
        CancellationToken ct)
    {
        try
        {
            var fullThumbPath = thumbnailPath;
            // Read the saved thumbnail for Vision API
            await using var thumbFile = File.OpenRead(
                Path.IsPathRooted(fullThumbPath)
                    ? fullThumbPath
                    : Path.Combine(
                        scopeFactory.CreateScope().ServiceProvider
                            .GetRequiredService<IOptions<StorageOptions>>().Value.BasePath,
                        fullThumbPath));

            var results = await visionService.RecognizeItemsAsync(thumbFile, containerName, ct);
            if (results.Count > 0)
                return results[0];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Vision recognition failed for mesh {Index}, using fallback", meshIndex);
        }

        return new RecognizedItemDto($"Object {meshIndex + 1}", [], null, 0f, null, null, null, null, null, null);
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
