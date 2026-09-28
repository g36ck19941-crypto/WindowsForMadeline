namespace CelesteDesktop.Player;

public enum PlayerNormalEventKind
{
    LeftGround,
    Landed,
    Jumped,
    HorizontalBlocked,
    VerticalBlocked
}

public sealed record PlayerNormalEvent(
    long Tick,
    PlayerNormalEventKind Kind,
    string? SolidId);
