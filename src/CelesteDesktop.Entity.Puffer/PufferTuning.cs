namespace CelesteDesktop.Entity.Puffer;

public sealed record PufferTuning(
    decimal SwimSpeed,
    int TriggerRadius,
    int WarningTicks,
    decimal LaunchSpeed,
    int RespawnTicks)
{
    // Independently designed deterministic baseline. Original commercial
    // Puffer movement, contact, timing and numeric behavior are not established here.
    public static PufferTuning PartialBaseline { get; } = new(
        SwimSpeed: 30m,
        TriggerRadius: 16,
        WarningTicks: 12,
        LaunchSpeed: 260m,
        RespawnTicks: 90);
}
