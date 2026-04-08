namespace HomeInventory3D.Application.BackgroundJobs;

/// <summary>
/// Channel abstraction for queuing scan processing jobs.
/// </summary>
public interface IScanProcessingChannel
{
    /// <summary>
    /// Enqueues a scan for background processing.
    /// </summary>
    ValueTask EnqueueAsync(ScanProcessingRequest request, CancellationToken ct);

    /// <summary>
    /// Reads queued requests (used by the background worker).
    /// </summary>
    IAsyncEnumerable<ScanProcessingRequest> ReadAllAsync(CancellationToken ct);
}
