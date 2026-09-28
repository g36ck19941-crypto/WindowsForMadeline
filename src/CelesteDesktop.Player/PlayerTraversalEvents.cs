namespace CelesteDesktop.Player;

public enum PlayerTraversalEventKind
{
    Jumped,
    Landed,
    DashStarted,
    DashEnded,
    DashBlocked,
    DashRefilled,
    WallSlideStarted,
    WallSlideEnded,
    WallJumped,
    ClimbStarted,
    ClimbReleased,
    ClimbJumped,
    ClimbHop,
    StaminaDepleted
}

public sealed record PlayerTraversalEvent(
    long Tick,
    PlayerTraversalEventKind Kind,
    string? SolidId = null);
