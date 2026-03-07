using System.Threading.Channels;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// Bounded channel-based job queue for meal plan generation.
/// Singleton — shared across all requests.
/// Same pattern as OcrJobQueue (Week 5).
/// </summary>
public class MealPlanJobQueue : IMealPlanJobQueue
{
    private readonly Channel<MealPlanJobItem> _channel;

    public MealPlanJobQueue()
    {
        var options = new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<MealPlanJobItem>(options);
    }

    public async ValueTask EnqueueAsync(MealPlanJobItem job, CancellationToken ct = default)
        => await _channel.Writer.WriteAsync(job, ct);

    public async ValueTask<MealPlanJobItem> DequeueAsync(CancellationToken ct = default)
        => await _channel.Reader.ReadAsync(ct);
}
