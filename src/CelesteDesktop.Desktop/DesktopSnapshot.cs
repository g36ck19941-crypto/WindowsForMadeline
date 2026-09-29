namespace CelesteDesktop.Desktop;

public sealed record DesktopSnapshot(
    long Sequence,
    TimeSpan MonotonicTimestamp,
    IReadOnlyList<DesktopSurfaceObservation> Surfaces);
