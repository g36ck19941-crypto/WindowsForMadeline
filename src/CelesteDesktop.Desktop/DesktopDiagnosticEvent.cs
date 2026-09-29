namespace CelesteDesktop.Desktop;

public sealed record DesktopDiagnosticEvent(
    long Sequence,
    string EventId,
    string Subsystem,
    string Severity,
    string Stage,
    string Outcome,
    int SurfaceCount,
    int FilteredCount,
    uint MinimumDpi,
    uint MaximumDpi);
