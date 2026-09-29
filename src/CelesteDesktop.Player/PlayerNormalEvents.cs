namespace CelesteDesktop.Player;

public enum PlayerNormalEventKind
{
    LeftGround,
    Landed,
    Jumped,
    HorizontalBlocked,
    VerticalBlocked,
    ExternalFallSpeedLimited
}

public sealed record PlayerNormalEvent(
    long Tick,
    PlayerNormalEventKind Kind,
    string? SolidId);
