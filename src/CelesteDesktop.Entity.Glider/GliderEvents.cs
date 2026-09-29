using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Glider;

public enum GliderEventKind
{
    PickedUp,
    Carried,
    HoldBlocked,
    Dropped,
    Thrown,
    HolderFallLimited,
    Opened,
    Closed,
    HorizontalBounced,
    VerticalBlocked,
    Landed,
    Bounced,
    LiftCarried,
    LiftInherited,
    Destroyed,
    Squished
}

public sealed record GliderEvent(
    long Tick,
    string EventId,
    GliderEventKind Kind,
    string EntityId,
    SimPoint Position,
    SimVector Speed,
    string? HolderId,
    string? SolidId);

public static class GliderEventIds
{
    public const string PickedUp = "GLIDER_PICKED_UP";
    public const string Carried = "GLIDER_CARRIED";
    public const string HoldBlocked = "GLIDER_HOLD_BLOCKED";
    public const string Dropped = "GLIDER_DROPPED";
    public const string Thrown = "GLIDER_THROWN";
    public const string HolderFallLimited = "GLIDER_HOLDER_FALL_LIMITED";
    public const string Opened = "GLIDER_OPENED";
    public const string Closed = "GLIDER_CLOSED";
    public const string HorizontalBounced = "GLIDER_HORIZONTAL_BOUNCED";
    public const string VerticalBlocked = "GLIDER_VERTICAL_BLOCKED";
    public const string Landed = "GLIDER_LANDED";
    public const string Bounced = "GLIDER_BOUNCED";
    public const string LiftCarried = "GLIDER_LIFT_CARRIED";
    public const string LiftInherited = "GLIDER_LIFT_INHERITED";
    public const string Destroyed = "GLIDER_DESTROYED";
    public const string Squished = "GLIDER_SQUISHED";

    public static string For(GliderEventKind kind) => kind switch
    {
        GliderEventKind.PickedUp => PickedUp,
        GliderEventKind.Carried => Carried,
        GliderEventKind.HoldBlocked => HoldBlocked,
        GliderEventKind.Dropped => Dropped,
        GliderEventKind.Thrown => Thrown,
        GliderEventKind.HolderFallLimited => HolderFallLimited,
        GliderEventKind.Opened => Opened,
        GliderEventKind.Closed => Closed,
        GliderEventKind.HorizontalBounced => HorizontalBounced,
        GliderEventKind.VerticalBlocked => VerticalBlocked,
        GliderEventKind.Landed => Landed,
        GliderEventKind.Bounced => Bounced,
        GliderEventKind.LiftCarried => LiftCarried,
        GliderEventKind.LiftInherited => LiftInherited,
        GliderEventKind.Destroyed => Destroyed,
        GliderEventKind.Squished => Squished,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
