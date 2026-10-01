using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Seeker;

public enum SeekerEventKind
{
    Patrolled,
    Turned,
    Alerted,
    ChaseStarted,
    Chased,
    TargetLost,
    WindupStarted,
    DashStarted,
    Dashed,
    TargetHit,
    WallHit,
    Stunned,
    Recovered,
    CenterFallbackUsed,
    Disabled,
    Enabled,
    TargetIgnored
}

public sealed record SeekerEvent(
    long Tick,
    string EventId,
    SeekerEventKind Kind,
    string EntityId,
    SimVector Center,
    string? TargetId);

public static class SeekerEventIds
{
    public const string Patrolled = "SEEKER_PATROLLED";
    public const string Turned = "SEEKER_TURNED";
    public const string Alerted = "SEEKER_ALERTED";
    public const string ChaseStarted = "SEEKER_CHASE_STARTED";
    public const string Chased = "SEEKER_CHASED";
    public const string TargetLost = "SEEKER_TARGET_LOST";
    public const string WindupStarted = "SEEKER_WINDUP_STARTED";
    public const string DashStarted = "SEEKER_DASH_STARTED";
    public const string Dashed = "SEEKER_DASHED";
    public const string TargetHit = "SEEKER_TARGET_HIT";
    public const string WallHit = "SEEKER_WALL_HIT";
    public const string Stunned = "SEEKER_STUNNED";
    public const string Recovered = "SEEKER_RECOVERED";
    public const string CenterFallbackUsed = "SEEKER_CENTER_FALLBACK_USED";
    public const string Disabled = "SEEKER_DISABLED";
    public const string Enabled = "SEEKER_ENABLED";
    public const string TargetIgnored = "SEEKER_TARGET_IGNORED";

    public static string For(SeekerEventKind kind) => kind switch
    {
        SeekerEventKind.Patrolled => Patrolled,
        SeekerEventKind.Turned => Turned,
        SeekerEventKind.Alerted => Alerted,
        SeekerEventKind.ChaseStarted => ChaseStarted,
        SeekerEventKind.Chased => Chased,
        SeekerEventKind.TargetLost => TargetLost,
        SeekerEventKind.WindupStarted => WindupStarted,
        SeekerEventKind.DashStarted => DashStarted,
        SeekerEventKind.Dashed => Dashed,
        SeekerEventKind.TargetHit => TargetHit,
        SeekerEventKind.WallHit => WallHit,
        SeekerEventKind.Stunned => Stunned,
        SeekerEventKind.Recovered => Recovered,
        SeekerEventKind.CenterFallbackUsed => CenterFallbackUsed,
        SeekerEventKind.Disabled => Disabled,
        SeekerEventKind.Enabled => Enabled,
        SeekerEventKind.TargetIgnored => TargetIgnored,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
