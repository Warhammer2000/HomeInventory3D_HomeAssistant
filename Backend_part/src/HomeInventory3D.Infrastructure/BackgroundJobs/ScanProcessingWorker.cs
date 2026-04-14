using HomeInventory3D.Application.BackgroundJobs;
using HomeInventory3D.Application.DTOs;
using HomeInventory3D.Application.Interfaces;
using HomeInventory3D.Domain.Entities;
using HomeInventory3D.Domain.Enums;
using HomeInventory3D.Infrastructure.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
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
                var itemPhysics = label.Physics ?? PhysicsPropertiesDto.Default;
                var addedDto = new ItemAddedDto(
                    item.Id, item.ContainerId, item.Name, item.Tags,
                    item.PositionX, item.PositionY, item.PositionZ,
                    item.RotationX, item.RotationY, item.RotationZ,
                    item.BboxMinX, item.BboxMinY, item.BboxMinZ,
                    item.BboxMaxX, item.BboxMaxY, item.BboxMaxZ,
                    fileStorage.GetUrl(glbPath),
                    fileStorage.GetUrl(thumbPath),
                    item.Confidence,
                    itemPhysics.MassKg, itemPhysics.RealSizeCm, itemPhysics.ColliderType, itemPhysics.Bounciness,
                    itemPhysics.Friction, itemPhysics.MaterialType, itemPhysics.IsFragile);

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

        var container = await containerRepo.GetByIdAsync(session.ContainerId, ct);

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 10, "📷 Photo uploaded. Starting AI analysis...", ct);

        var photoBytes = await File.ReadAllBytesAsync(photoPath, ct);

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 15, "🧠 Sending to Claude Vision for recognition...", ct);

        // Step 1: Claude Vision → recognize ALL objects
        List<RecognizedItemDto> recognizedItems;
        using (var visionStream = new MemoryStream(photoBytes))
        {
            recognizedItems = await SafeVisionRecognizeAllAsync(visionService, visionStream, container?.Name, ct);
        }

        var count = recognizedItems.Count;
        session.ItemsDetected = count;

        await notifications.NotifyScanProgressAsync(
            session.Id, session.ContainerId, 20,
            $"✅ Recognized {count} object(s): {string.Join(", ", recognizedItems.Select(r => r.Name))}", ct);

        logger.LogInformation("Photo scan {ScanId}: recognized {Count} objects", session.Id, count);

        if (count == 0)
        {
            session.Status = ScanStatus.Completed;
            session.ItemsAdded = 0;
            await scanRepo.UpdateAsync(session, ct);
            await notifications.NotifyScanCompletedAsync(session.Id, session.ContainerId, 0, 0, 0, ct);
            return;
        }

        // Step 2: Launch ALL items in parallel — each async task has its own DI scope
        var tasks = recognizedItems.Select((label, i) =>
        {
            var croppedBytes = CropImageByBbox(photoBytes, label);
            return ProcessSingleItemWithScopeAsync(label, croppedBytes, i, count, session, container, ct);
        }).ToList();

        var results = await Task.WhenAll(tasks);
        var itemsAdded = results.Count(r => r);
        var itemsFailed = count - itemsAdded;

        // Finalize session
        await using var endScope = scopeFactory.CreateAsyncScope();
        var endScanRepo = endScope.ServiceProvider.GetRequiredService<IScanSessionRepository>();
        var endNotify = endScope.ServiceProvider.GetRequiredService<IInventoryNotificationService>();

        session.Status = ScanStatus.Completed;
        session.ItemsAdded = itemsAdded;
        if (itemsFailed > 0)
            session.ErrorMessage = $"{itemsFailed} item(s) failed";
        await endScanRepo.UpdateAsync(session, ct);
        await endNotify.NotifyScanCompletedAsync(session.Id, session.ContainerId, count, itemsAdded, 0, ct);

        logger.LogInformation("Photo scan {ScanId}: {Added}/{Total} items added", session.Id, itemsAdded, count);
    }

    private async Task<bool> ProcessSingleItemWithScopeAsync(
        RecognizedItemDto label, byte[] croppedBytes, int index, int total,
        ScanSession session, Container? container, CancellationToken ct)
    {
        try
        {
            await using var s = scopeFactory.CreateAsyncScope();
            await ProcessSingleItemFromPhotoAsync(
                label, croppedBytes, index, total, session, container,
                s.ServiceProvider.GetRequiredService<IFileStorageService>(),
                s.ServiceProvider.GetRequiredService<IItemRepository>(),
                s.ServiceProvider.GetRequiredService<IImageTo3DService>(),
                s.ServiceProvider.GetRequiredService<IInventoryNotificationService>(), ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed: {Name} in scan {ScanId}", label.Name, session.Id);
            return false;
        }
    }

    private async Task ProcessSingleItemFromPhotoAsync(
        RecognizedItemDto label, byte[] croppedBytes, int index, int total,
        ScanSession session, Container? container,
        IFileStorageService fileStorage, IItemRepository itemRepo,
        IImageTo3DService imageTo3D, IInventoryNotificationService notifications,
        CancellationToken ct)
    {
        var itemId = Guid.CreateVersion7();
        var meshyPrompt = label.Confidence > 0
            ? $"{label.Name}. {label.Description ?? ""}"
            : null;

        // Per-item progress via dedicated SignalR event
        await notifications.NotifyItemProgressAsync(
            session.Id, label.Name, index, total, 0, "Starting 3D generation...", ct);

        var itemProgress = new Progress<int>(meshyPct =>
        {
            var stage = meshyPct switch
            {
                < 15 => "Queued",
                < 50 => "Generating mesh",
                < 80 => "Texturing",
                < 100 => "Finalizing",
                _ => "Complete"
            };
            _ = notifications.NotifyItemProgressAsync(session.Id, label.Name, index, total, meshyPct, stage, ct);
        });

        Stream? glbStream;
        using (var meshyStream = new MemoryStream(croppedBytes))
        {
            glbStream = await SafeMeshyGenerateAsync(imageTo3D, meshyStream, meshyPrompt, itemProgress, ct);
        }

        string? glbPath = null;
        if (glbStream is not null)
        {
            glbPath = await fileStorage.SaveAsync(
                glbStream, $"meshes/{session.ContainerId}", $"{itemId}.glb", ct);
            await glbStream.DisposeAsync();
        }

        // Save cropped image as thumbnail
        using var thumbStream = new MemoryStream(croppedBytes);
        var thumbPath = await fileStorage.SaveAsync(
            thumbStream, $"thumbnails/{session.ContainerId}", $"{itemId}.jpg", ct);

        // Create InventoryItem
        var physics = label.Physics ?? PhysicsPropertiesDto.Default;
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
            MassKg = physics.MassKg,
            RealSizeCm = physics.RealSizeCm,
            ColliderType = physics.ColliderType,
            Bounciness = physics.Bounciness,
            Friction = physics.Friction,
            MaterialType = physics.MaterialType,
            IsFragile = physics.IsFragile,
            Status = ItemStatus.Present,
            CreatedAt = now,
            UpdatedAt = now
        };

        await itemRepo.AddAsync(item, ct);

        // SignalR — immediately notify Unity
        var addedDto = new ItemAddedDto(
            item.Id, item.ContainerId, item.Name, item.Tags,
            item.PositionX, item.PositionY, item.PositionZ,
            null, null, null, null, null, null, null, null, null,
            glbPath is not null ? fileStorage.GetUrl(glbPath) : null,
            fileStorage.GetUrl(thumbPath),
            item.Confidence,
            physics.MassKg, physics.RealSizeCm, physics.ColliderType, physics.Bounciness,
            physics.Friction, physics.MaterialType, physics.IsFragile);

        await notifications.NotifyItemAddedAsync(addedDto, ct);
        await notifications.NotifyItemProgressAsync(session.Id, label.Name, index, total, 100, "Complete", ct);

        logger.LogInformation("Photo item {Index}/{Total}: {Name} (GLB: {HasGlb})",
            index + 1, total, label.Name, glbPath is not null);
    }

    private static byte[] CropImageByBbox(byte[] photoBytes, RecognizedItemDto item, float padding = 0.05f)
    {
        // If no bbox data, return original photo
        if (!item.BboxMinX.HasValue || !item.BboxMaxX.HasValue)
            return photoBytes;

        using var image = SixLabors.ImageSharp.Image.Load(photoBytes);
        var w = image.Width;
        var h = image.Height;

        var bboxMinX = item.BboxMinX ?? 0f;
        var bboxMinY = item.BboxMinY ?? 0f;
        var bboxMaxX = item.BboxMaxX ?? 1f;
        var bboxMaxY = item.BboxMaxY ?? 1f;

        // Add padding (percentage of bbox size)
        var padW = (bboxMaxX - bboxMinX) * padding;
        var padH = (bboxMaxY - bboxMinY) * padding;

        var x1 = Math.Max(0, (int)((bboxMinX - padW) * w));
        var y1 = Math.Max(0, (int)((bboxMinY - padH) * h));
        var x2 = Math.Min(w, (int)((bboxMaxX + padW) * w));
        var y2 = Math.Min(h, (int)((bboxMaxY + padH) * h));

        var cropW = Math.Max(1, x2 - x1);
        var cropH = Math.Max(1, y2 - y1);

        var rect = new SixLabors.ImageSharp.Rectangle(x1, y1, cropW, cropH);
        using var cropped = image.Clone(ctx => ctx.Crop(rect));

        using var ms = new MemoryStream();
        cropped.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    private async Task<List<RecognizedItemDto>> SafeVisionRecognizeAllAsync(
        IVisionRecognitionService visionService, MemoryStream photoStream,
        string? containerName, CancellationToken ct)
    {
        try
        {
            var results = await visionService.RecognizeItemsAsync(photoStream, containerName, ct);
            if (results.Count > 0) return results;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Vision recognition failed for photo, using fallback");
        }

        return [new RecognizedItemDto("Photo Object", [], null, 0f, null, null, null, null, null, null, PhysicsPropertiesDto.Default)];
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

        return new RecognizedItemDto("Photo Object", [], null, 0f, null, null, null, null, null, null, PhysicsPropertiesDto.Default);
    }

    private async Task<Stream?> SafeMeshyGenerateAsync(
        IImageTo3DService imageTo3D, MemoryStream photoStream, string? objectPrompt,
        IProgress<int>? progress, CancellationToken ct)
    {
        try
        {
            return await imageTo3D.GenerateModelAsync(photoStream, objectPrompt, progress, ct);
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

        return new RecognizedItemDto($"Object {meshIndex + 1}", [], null, 0f, null, null, null, null, null, null, PhysicsPropertiesDto.Default);
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
