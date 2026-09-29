namespace CelesteDesktop.Animation;

public sealed record AnimationDiagnosticEvent(
    long Sequence,
    string EventId,
    string Subsystem,
    string Severity,
    string Stage,
    string Outcome,
    long Tick,
    string EntityId,
    string AnimationId,
    int? FrameIndex = null,
    string? FrameFingerprint = null,
    string? DetailCode = null,
    string? ExceptionType = null,
    string? Message = null,
    int? HResult = null,
    string? Stack = null,
    string? Inner = null,
    bool? Recoverable = null,
    string? RecoveryAction = null);
