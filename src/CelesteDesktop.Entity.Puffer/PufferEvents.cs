using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Puffer;

public enum PufferEventKind
{
    Swam,
    Turned,
    WarningStarted,
    Exploded,
    LaunchIssued,
    CenterFallbackUsed,
    SpentStarted,
    Respawned,
    Disabled,
    Enabled,
    ContactIgnored
}

public sealed record PufferEvent(
    long Tick,
    string EventId,
    PufferEventKind Kind,
    string EntityId,
    SimVector Center,
    string? TargetId);

public static class PufferEventIds
{
    public const string Swam = "PUFFER_SWAM";
    public const string Turned = "PUFFER_TURNED";
    public const string WarningStarted = "PUFFER_WARNING_STARTED";
    public const string Exploded = "PUFFER_EXPLODED";
    public const string LaunchIssued = "PUFFER_LAUNCH_ISSUED";
    public const string CenterFallbackUsed = "PUFFER_CENTER_FALLBACK_USED";
    public const string SpentStarted = "PUFFER_SPENT_STARTED";
    public const string Respawned = "PUFFER_RESPAWNED";
    public const string Disabled = "PUFFER_DISABLED";
    public const string Enabled = "PUFFER_ENABLED";
    public const string ContactIgnored = "PUFFER_CONTACT_IGNORED";

    public static string For(PufferEventKind kind) => kind switch
    {
        PufferEventKind.Swam => Swam,
        PufferEventKind.Turned => Turned,
        PufferEventKind.WarningStarted => WarningStarted,
        PufferEventKind.Exploded => Exploded,
        PufferEventKind.LaunchIssued => LaunchIssued,
        PufferEventKind.CenterFallbackUsed => CenterFallbackUsed,
        PufferEventKind.SpentStarted => SpentStarted,
        PufferEventKind.Respawned => Respawned,
        PufferEventKind.Disabled => Disabled,
        PufferEventKind.Enabled => Enabled,
        PufferEventKind.ContactIgnored => ContactIgnored,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
