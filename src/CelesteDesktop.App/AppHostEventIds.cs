namespace CelesteDesktop.App;

public static class AppHostEventIds
{
    public const string Started = "APP_HOST_STARTED";
    public const string TickDispatched = "APP_HOST_TICK_DISPATCHED";
    public const string BacklogDropped = "APP_HOST_BACKLOG_DROPPED";
    public const string Cancelled = "APP_HOST_CANCELLED";
    public const string Stopped = "APP_HOST_STOPPED";
    public const string Faulted = "APP_HOST_FAULTED";
    public const string Disposed = "APP_HOST_DISPOSED";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        Started,
        TickDispatched,
        BacklogDropped,
        Cancelled,
        Stopped,
        Faulted,
        Disposed
    });
}
