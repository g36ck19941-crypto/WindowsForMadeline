using System.Collections.ObjectModel;

namespace CelesteDesktop.App;

public sealed class OfflineAppHostResult
{
    public OfflineAppHostResult(
        int executedTicks,
        int droppedIntervals,
        bool cancelled,
        AppLifecycleState finalLifecycle,
        IEnumerable<AppHostDiagnosticEvent> events)
    {
        ExecutedTicks = executedTicks;
        DroppedIntervals = droppedIntervals;
        Cancelled = cancelled;
        FinalLifecycle = finalLifecycle;
        Events = new ReadOnlyCollection<AppHostDiagnosticEvent>(events.ToArray());
    }

    public int ExecutedTicks { get; }
    public int DroppedIntervals { get; }
    public bool Cancelled { get; }
    public AppLifecycleState FinalLifecycle { get; }
    public IReadOnlyList<AppHostDiagnosticEvent> Events { get; }
}
