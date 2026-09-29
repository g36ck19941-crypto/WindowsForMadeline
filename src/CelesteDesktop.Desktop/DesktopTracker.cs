namespace CelesteDesktop.Desktop;

public sealed class DesktopTracker
{
    private readonly IDesktopSurfaceProvider _provider;
    private readonly Action<DesktopDiagnosticEvent> _emit;
    private readonly Dictionary<Guid, TrackedSurface> _tracked = [];
    private long _snapshotSequence;
    private long _diagnosticSequence;
    private int _nextAnonymousId;
    private TimeSpan? _lastTimestamp;

    public DesktopTracker(IDesktopSurfaceProvider provider, Action<DesktopDiagnosticEvent>? emit = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _emit = emit ?? (_ => { });
    }

    public DesktopSnapshot Capture(TimeSpan monotonicTimestamp)
    {
        if (monotonicTimestamp < TimeSpan.Zero || (_lastTimestamp is { } last && monotonicTimestamp <= last))
        {
            throw new ArgumentOutOfRangeException(nameof(monotonicTimestamp), "Capture time must increase monotonically.");
        }

        var candidates = _provider.Capture() ?? throw new InvalidOperationException("Desktop provider returned null.");
        if (candidates.Count > 4096)
        {
            throw new InvalidOperationException("DESKTOP_SURFACE_BUDGET_EXCEEDED");
        }

        var visible = candidates.Where(item => item.IsVisible && !item.IsCloaked).ToArray();
        if (visible.Select(item => item.SessionToken).Distinct().Count() != visible.Length)
        {
            throw new InvalidOperationException("DESKTOP_DUPLICATE_SESSION_TOKEN");
        }

        var elapsedSeconds = _lastTimestamp is null ? 0d : (monotonicTimestamp - _lastTimestamp.Value).TotalSeconds;
        var observations = new List<DesktopSurfaceObservation>(visible.Length);
        var currentTokens = new HashSet<Guid>();
        foreach (var item in visible)
        {
            currentTokens.Add(item.SessionToken);
            if (!_tracked.TryGetValue(item.SessionToken, out var prior))
            {
                prior = new TrackedSurface($"surface-{++_nextAnonymousId:D6}", item.Bounds);
            }

            var velocityX = elapsedSeconds == 0d ? 0d : (item.Bounds.Left - prior.Bounds.Left) / elapsedSeconds;
            var velocityY = elapsedSeconds == 0d ? 0d : (item.Bounds.Top - prior.Bounds.Top) / elapsedSeconds;
            _tracked[item.SessionToken] = prior with { Bounds = item.Bounds };
            observations.Add(new DesktopSurfaceObservation(prior.AnonymousId, item.Bounds, item.Dpi, velocityX, velocityY));
        }

        foreach (var token in _tracked.Keys.Where(token => !currentTokens.Contains(token)).ToArray())
        {
            _tracked.Remove(token);
        }

        observations.Sort((left, right) => StringComparer.Ordinal.Compare(left.AnonymousId, right.AnonymousId));
        _lastTimestamp = monotonicTimestamp;
        var snapshot = new DesktopSnapshot(++_snapshotSequence, monotonicTimestamp, observations.AsReadOnly());
        _emit(new DesktopDiagnosticEvent(
            ++_diagnosticSequence,
            "DESKTOP_SNAPSHOT_CAPTURED",
            "Desktop",
            "Info",
            "capture",
            "succeeded",
            observations.Count,
            candidates.Count - observations.Count,
            observations.Count == 0 ? 0 : observations.Min(item => item.Dpi),
            observations.Count == 0 ? 0 : observations.Max(item => item.Dpi)));
        return snapshot;
    }

    private sealed record TrackedSurface(string AnonymousId, DesktopRect Bounds);
}
