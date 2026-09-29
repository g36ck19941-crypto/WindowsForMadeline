namespace CelesteDesktop.Rendering;

public sealed record RenderDiagnosticEvent(
    DateTimeOffset TimestampUtc,
    Guid RunId,
    long Sequence,
    string EventId,
    string Subsystem,
    string Severity,
    string Stage,
    string Outcome,
    double DurationMs,
    string? FrameFingerprint = null,
    int? Width = null,
    int? Height = null,
    string? Backend = null,
    string? ExceptionType = null,
    string? Message = null,
    int? HResult = null,
    string? Stack = null,
    string? Inner = null,
    bool? Recoverable = null,
    string? RecoveryAction = null);
