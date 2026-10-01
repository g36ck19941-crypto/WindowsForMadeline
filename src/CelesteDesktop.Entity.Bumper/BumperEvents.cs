using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Bumper;

public enum BumperEventKind
{
    Activated,
    LaunchIssued,
    CenterFallbackUsed,
    CooldownStarted,
    Ready,
    Disabled,
    Enabled,
    ContactIgnored
}

public sealed record BumperEvent(
    long Tick,
    string EventId,
    BumperEventKind Kind,
    string EntityId,
    SimPoint Center,
    string? TargetId);

public static class BumperEventIds
{
    public const string Activated = "BUMPER_ACTIVATED";
    public const string LaunchIssued = "BUMPER_LAUNCH_ISSUED";
    public const string CenterFallbackUsed = "BUMPER_CENTER_FALLBACK_USED";
    public const string CooldownStarted = "BUMPER_COOLDOWN_STARTED";
    public const string Ready = "BUMPER_READY";
    public const string Disabled = "BUMPER_DISABLED";
    public const string Enabled = "BUMPER_ENABLED";
    public const string ContactIgnored = "BUMPER_CONTACT_IGNORED";

    public static string For(BumperEventKind kind) => kind switch
    {
        BumperEventKind.Activated => Activated,
        BumperEventKind.LaunchIssued => LaunchIssued,
        BumperEventKind.CenterFallbackUsed => CenterFallbackUsed,
        BumperEventKind.CooldownStarted => CooldownStarted,
        BumperEventKind.Ready => Ready,
        BumperEventKind.Disabled => Disabled,
        BumperEventKind.Enabled => Enabled,
        BumperEventKind.ContactIgnored => ContactIgnored,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
