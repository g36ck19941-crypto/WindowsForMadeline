namespace CelesteDesktop.App;

public sealed record AppHostDiagnosticEvent(
    long Sequence,
    string EventId,
    string Stage,
    string Outcome,
    long AppTick,
    long ClockTimestamp,
    int ExecutedTicks,
    int DroppedIntervals,
    string? Detail,
    AppExceptionInfo? Exception);
