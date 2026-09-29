using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Spring;

public enum SpringEventKind
{
    Activated,
    LaunchIssued,
    Retracted,
    CooldownStarted,
    Ready,
    Disabled,
    Enabled,
    ContactIgnored
}

public sealed record SpringEvent(
    long Tick,
    string EventId,
    SpringEventKind Kind,
    string EntityId,
    SimPoint Position,
    string? TargetId,
    SpringTargetKind? TargetKind);

public static class SpringEventIds
{
    public const string Activated = "SPRING_ACTIVATED";
    public const string LaunchIssued = "SPRING_LAUNCH_ISSUED";
    public const string Retracted = "SPRING_RETRACTED";
    public const string CooldownStarted = "SPRING_COOLDOWN_STARTED";
    public const string Ready = "SPRING_READY";
    public const string Disabled = "SPRING_DISABLED";
    public const string Enabled = "SPRING_ENABLED";
    public const string ContactIgnored = "SPRING_CONTACT_IGNORED";

    public static string For(SpringEventKind kind) => kind switch
    {
        SpringEventKind.Activated => Activated,
        SpringEventKind.LaunchIssued => LaunchIssued,
        SpringEventKind.Retracted => Retracted,
        SpringEventKind.CooldownStarted => CooldownStarted,
        SpringEventKind.Ready => Ready,
        SpringEventKind.Disabled => Disabled,
        SpringEventKind.Enabled => Enabled,
        SpringEventKind.ContactIgnored => ContactIgnored,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
