namespace CelesteDesktop.App;

public static class AppEventIds
{
    public const string Started = "APP_STARTED";
    public const string TickStarted = "APP_TICK_STARTED";
    public const string SimulationCompleted = "APP_SIMULATION_COMPLETED";
    public const string EffectRouted = "APP_EFFECT_ROUTED";
    public const string EffectConflict = "APP_EFFECT_CONFLICT";
    public const string EffectTargetUnresolved = "APP_EFFECT_TARGET_UNRESOLVED";
    public const string SeekerHitObserved = "APP_SEEKER_HIT_OBSERVED";
    public const string PresentationCompleted = "APP_PRESENTATION_COMPLETED";
    public const string ComponentDisabled = "APP_COMPONENT_DISABLED";
    public const string TickCompleted = "APP_TICK_COMPLETED";
    public const string Paused = "APP_PAUSED";
    public const string Resumed = "APP_RESUMED";
    public const string Stopped = "APP_STOPPED";
    public const string Faulted = "APP_FAULTED";
    public const string Disposed = "APP_DISPOSED";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        Started,
        TickStarted,
        SimulationCompleted,
        EffectRouted,
        EffectConflict,
        EffectTargetUnresolved,
        SeekerHitObserved,
        PresentationCompleted,
        ComponentDisabled,
        TickCompleted,
        Paused,
        Resumed,
        Stopped,
        Faulted,
        Disposed
    });
}
