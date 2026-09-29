namespace CelesteDesktop.Entity.Spring;

public sealed record SpringTuning(
    decimal LaunchSpeed,
    int RetractedTicks,
    int CooldownTicks)
{
    // Independently designed deterministic baseline. Original commercial
    // Spring timing and numeric behavior are not established by public facts.
    public static SpringTuning PartialBaseline { get; } = new(
        LaunchSpeed: 240m,
        RetractedTicks: 3,
        CooldownTicks: 5);
}
