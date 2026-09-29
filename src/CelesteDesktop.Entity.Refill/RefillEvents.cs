using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Refill;

public enum RefillEventKind
{
    Collected,
    RestoreIssued,
    CooldownStarted,
    Respawned,
    Disabled,
    Enabled,
    ContactIgnored
}

public sealed record RefillEvent(
    long Tick,
    string EventId,
    RefillEventKind Kind,
    string EntityId,
    SimPoint Position,
    string? TargetId);

public static class RefillEventIds
{
    public const string Collected = "REFILL_COLLECTED";
    public const string RestoreIssued = "REFILL_RESTORE_ISSUED";
    public const string CooldownStarted = "REFILL_COOLDOWN_STARTED";
    public const string Respawned = "REFILL_RESPAWNED";
    public const string Disabled = "REFILL_DISABLED";
    public const string Enabled = "REFILL_ENABLED";
    public const string ContactIgnored = "REFILL_CONTACT_IGNORED";

    public static string For(RefillEventKind kind) => kind switch
    {
        RefillEventKind.Collected => Collected,
        RefillEventKind.RestoreIssued => RestoreIssued,
        RefillEventKind.CooldownStarted => CooldownStarted,
        RefillEventKind.Respawned => Respawned,
        RefillEventKind.Disabled => Disabled,
        RefillEventKind.Enabled => Enabled,
        RefillEventKind.ContactIgnored => ContactIgnored,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
