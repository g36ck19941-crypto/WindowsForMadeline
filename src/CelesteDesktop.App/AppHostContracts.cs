using System.Diagnostics;

namespace CelesteDesktop.App;

public interface IAppHostClock
{
    long Frequency { get; }
    long GetTimestamp();
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public interface IAppTickInputSource
{
    OfflineAppTickInput GetInput(long tick);
}

public sealed class SystemMonotonicAppHostClock : IAppHostClock
{
    public long Frequency => Stopwatch.Frequency;
    public long GetTimestamp() => Stopwatch.GetTimestamp();
    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        new(Task.Delay(delay, cancellationToken));
}

public sealed class DelegateAppTickInputSource(Func<long, OfflineAppTickInput> factory) : IAppTickInputSource
{
    private readonly Func<long, OfflineAppTickInput> _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    public OfflineAppTickInput GetInput(long tick) => _factory(tick) ?? throw new InvalidOperationException("App input source returned null.");
}

public sealed record OfflineAppHostOptions
{
    public OfflineAppHostOptions(int maximumCatchUpTicks = 4)
    {
        if (maximumCatchUpTicks is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(maximumCatchUpTicks));
        MaximumCatchUpTicks = maximumCatchUpTicks;
    }

    public int MaximumCatchUpTicks { get; }
}
