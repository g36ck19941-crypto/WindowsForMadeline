using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Water;

public enum WaterEventKind
{
    Entered,
    Submerged,
    MotionIssued,
    Exited,
    Disabled,
    Enabled,
    ContactIgnored
}

public sealed record WaterEvent(
    long Tick,
    string EventId,
    WaterEventKind Kind,
    string EntityId,
    SimRect Bounds,
    string? TargetId);

public static class WaterEventIds
{
    public const string Entered = "WATER_ENTERED";
    public const string Submerged = "WATER_SUBMERGED";
    public const string MotionIssued = "WATER_MOTION_ISSUED";
    public const string Exited = "WATER_EXITED";
    public const string Disabled = "WATER_DISABLED";
    public const string Enabled = "WATER_ENABLED";
    public const string ContactIgnored = "WATER_CONTACT_IGNORED";

    public static string For(WaterEventKind kind) => kind switch
    {
        WaterEventKind.Entered => Entered,
        WaterEventKind.Submerged => Submerged,
        WaterEventKind.MotionIssued => MotionIssued,
        WaterEventKind.Exited => Exited,
        WaterEventKind.Disabled => Disabled,
        WaterEventKind.Enabled => Enabled,
        WaterEventKind.ContactIgnored => ContactIgnored,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
