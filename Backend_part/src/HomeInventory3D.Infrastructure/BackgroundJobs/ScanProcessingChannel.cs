using System.Threading.Channels;
using HomeInventory3D.Application.BackgroundJobs;

namespace HomeInventory3D.Infrastructure.BackgroundJobs;

/// <summary>
/// Channel-based implementation of the scan processing queue.
/// </summary>
public class ScanProcessingChannel : IScanProcessingChannel
{
    private readonly Channel<ScanProcessingRequest> _channel =
        Channel.CreateUnbounded<ScanProcessingRequest>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public async ValueTask EnqueueAsync(ScanProcessingRequest request, CancellationToken ct)
    {
        await _channel.Writer.WriteAsync(request, ct);
    }

    public IAsyncEnumerable<ScanProcessingRequest> ReadAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
