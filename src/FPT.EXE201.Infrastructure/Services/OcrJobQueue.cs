using System.Threading.Channels;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Channel-based in-process job queue.
/// Capacity: 100 pending jobs. If full, waits (backpressure).
/// ⚠️ Jobs are lost on app restart — acceptable for this app scale.
/// For durability, upgrade to Redis Queue or database-backed queue.
/// </summary>
public class OcrJobQueue : IOcrJobQueue
{
    private readonly Channel<OcrJobItem> _channel;

    public OcrJobQueue()
    {
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<OcrJobItem>(options);
    }

    public async ValueTask EnqueueAsync(OcrJobItem job, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<OcrJobItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
