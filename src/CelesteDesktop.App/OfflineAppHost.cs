namespace CelesteDesktop.App;

public sealed class OfflineAppHost : IDisposable
{
    private const int TickRate = 60;
    private readonly OfflineAppSession _session;
    private readonly IAppHostClock _clock;
    private readonly IAppTickInputSource _inputSource;
    private readonly OfflineAppHostOptions _options;
    private readonly Action<AppHostDiagnosticEvent>? _diagnostics;
    private readonly List<AppHostDiagnosticEvent> _events = [];
    private long _sequence;
    private bool _runStarted;
    private bool _disposed;

    public OfflineAppHost(
        OfflineAppSession session,
        IAppHostClock clock,
        IAppTickInputSource inputSource,
        OfflineAppHostOptions? options = null,
        Action<AppHostDiagnosticEvent>? diagnostics = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _inputSource = inputSource ?? throw new ArgumentNullException(nameof(inputSource));
        _options = options ?? new OfflineAppHostOptions();
        _diagnostics = diagnostics;
        if (_clock.Frequency <= 0) throw new ArgumentOutOfRangeException(nameof(clock), "Clock frequency must be positive.");
    }

    public async Task<OfflineAppHostResult> RunAsync(int? maximumTicks = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_runStarted) throw new InvalidOperationException("Headless App Host can run only once.");
        if (maximumTicks is <= 0) throw new ArgumentOutOfRangeException(nameof(maximumTicks));
        if (_session.State != AppLifecycleState.Created) throw new InvalidOperationException("Headless App Host requires a newly created App session.");
        _runStarted = true;

        var executed = 0;
        var dropped = 0;
        var cancelled = false;
        long scaledAccumulator = 0;
        var lastTimestamp = _clock.GetTimestamp();
        try
        {
            _session.Start();
            Emit(AppHostEventIds.Started, "lifecycle", "succeeded", executed, dropped, lastTimestamp, null, null);
            while (!maximumTicks.HasValue || executed < maximumTicks.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var now = _clock.GetTimestamp();
                if (now < lastTimestamp) throw new InvalidOperationException("Monotonic clock moved backwards.");
                var elapsed = checked(now - lastTimestamp);
                lastTimestamp = now;
                scaledAccumulator = checked(scaledAccumulator + checked(elapsed * TickRate));

                var due = scaledAccumulator / _clock.Frequency;
                if (due == 0)
                {
                    var remainingScaled = checked(_clock.Frequency - scaledAccumulator);
                    var timestampDelay = checked((remainingScaled + TickRate - 1) / TickRate);
                    var delay = TimeSpan.FromSeconds((double)timestampDelay / _clock.Frequency);
                    await _clock.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (due > _options.MaximumCatchUpTicks)
                {
                    var discarded = checked((int)(due - _options.MaximumCatchUpTicks));
                    dropped = checked(dropped + discarded);
                    scaledAccumulator -= checked((long)discarded * _clock.Frequency);
                    due = _options.MaximumCatchUpTicks;
                    Emit(AppHostEventIds.BacklogDropped, "schedule", "bounded", executed, dropped, now, $"dropped={discarded}", null);
                }

                for (var index = 0; index < due && (!maximumTicks.HasValue || executed < maximumTicks.Value); index++)
                {
                    var nextTick = checked(_session.Tick + 1);
                    var input = _inputSource.GetInput(nextTick);
                    _session.Step(input);
                    scaledAccumulator -= _clock.Frequency;
                    executed = checked(executed + 1);
                    Emit(AppHostEventIds.TickDispatched, "schedule", "succeeded", executed, dropped, now, null, null);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
            Emit(AppHostEventIds.Cancelled, "lifecycle", "cancelled", executed, dropped, SafeTimestamp(), null, null);
        }
        catch (Exception exception)
        {
            Emit(AppHostEventIds.Faulted, "host", "failed", executed, dropped, SafeTimestamp(), exception.Message, exception);
            SafeStop();
            throw new AppHostException("host", "Headless App Host failed.", exception);
        }

        SafeStop();
        Emit(AppHostEventIds.Stopped, "lifecycle", "succeeded", executed, dropped, SafeTimestamp(), null, null);
        return new OfflineAppHostResult(executed, dropped, cancelled, _session.State, _events);
    }

    public void Dispose()
    {
        if (_disposed) return;
        SafeStop();
        _session.Dispose();
        _disposed = true;
        Emit(AppHostEventIds.Disposed, "lifecycle", "succeeded", 0, 0, SafeTimestamp(), null, null);
    }

    private void SafeStop()
    {
        if (_session.State is AppLifecycleState.Running or AppLifecycleState.Paused or AppLifecycleState.Faulted)
            _session.Stop();
    }

    private long SafeTimestamp()
    {
        try { return _clock.GetTimestamp(); }
        catch { return -1; }
    }

    private void Emit(
        string eventId,
        string stage,
        string outcome,
        int executed,
        int dropped,
        long timestamp,
        string? detail,
        Exception? exception)
    {
        var item = new AppHostDiagnosticEvent(
            checked(++_sequence), eventId, stage, outcome, _session.Tick, timestamp, executed, dropped, detail,
            exception is null ? null : AppExceptionInfo.Capture(exception));
        _events.Add(item);
        _diagnostics?.Invoke(item);
    }
}
