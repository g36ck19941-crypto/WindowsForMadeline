using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Theo;

public enum TheoCrystalEventKind
{
    PickedUp,
    Carried,
    HoldBlocked,
    Dropped,
    Thrown,
    HorizontalBounced,
    VerticalBlocked,
    Landed,
    Bounced,
    LiftCarried,
    LiftInherited,
    Squished
}

public sealed record TheoCrystalEvent(
    long Tick,
    string EventId,
    TheoCrystalEventKind Kind,
    string EntityId,
    SimPoint Position,
    SimVector Speed,
    string? HolderId,
    string? SolidId);

public static class TheoCrystalEventIds
{
    public const string PickedUp = "THEO_PICKED_UP";
    public const string Carried = "THEO_CARRIED";
    public const string HoldBlocked = "THEO_HOLD_BLOCKED";
    public const string Dropped = "THEO_DROPPED";
    public const string Thrown = "THEO_THROWN";
    public const string HorizontalBounced = "THEO_HORIZONTAL_BOUNCED";
    public const string VerticalBlocked = "THEO_VERTICAL_BLOCKED";
    public const string Landed = "THEO_LANDED";
    public const string Bounced = "THEO_BOUNCED";
    public const string LiftCarried = "THEO_LIFT_CARRIED";
    public const string LiftInherited = "THEO_LIFT_INHERITED";
    public const string Squished = "THEO_SQUISHED";

    public static string For(TheoCrystalEventKind kind) => kind switch
    {
        TheoCrystalEventKind.PickedUp => PickedUp,
        TheoCrystalEventKind.Carried => Carried,
        TheoCrystalEventKind.HoldBlocked => HoldBlocked,
        TheoCrystalEventKind.Dropped => Dropped,
        TheoCrystalEventKind.Thrown => Thrown,
        TheoCrystalEventKind.HorizontalBounced => HorizontalBounced,
        TheoCrystalEventKind.VerticalBlocked => VerticalBlocked,
        TheoCrystalEventKind.Landed => Landed,
        TheoCrystalEventKind.Bounced => Bounced,
        TheoCrystalEventKind.LiftCarried => LiftCarried,
        TheoCrystalEventKind.LiftInherited => LiftInherited,
        TheoCrystalEventKind.Squished => Squished,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
