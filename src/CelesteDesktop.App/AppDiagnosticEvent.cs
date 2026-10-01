namespace CelesteDesktop.App;

public sealed record AppDiagnosticEvent(
    long Sequence,
    long Tick,
    string EventId,
    string Stage,
    string Outcome,
    string ComponentId,
    string? TargetId,
    string? Detail,
    AppExceptionInfo? Exception);
