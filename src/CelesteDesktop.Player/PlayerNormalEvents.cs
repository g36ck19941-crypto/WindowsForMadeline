namespace CelesteDesktop.Player;

public enum PlayerNormalEventKind
{
    LeftGround,
    Landed,
    Jumped,
    LiftVelocityApplied,
    WallSpeedRetained,
    WallSpeedRestored,
    WallSpeedRetentionCancelled,
    WallSpeedRetentionExpired,
    UpwardCornerCorrected,
    HorizontalBlocked,
    VerticalBlocked,
    ExternalFallSpeedLimited,
    ExternalVelocityApplied
}

public sealed record PlayerNormalEvent(
    long Tick,
    PlayerNormalEventKind Kind,
    string? SolidId);
