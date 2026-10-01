namespace CelesteDesktop.Entity.Bumper;

public sealed record BumperTuning(
    int ContactRadius,
    decimal LaunchSpeed,
    int CooldownTicks)
{
    // Independently designed deterministic baseline. Original commercial
    // Bumper collision, timing and numeric behavior are not established here.
    public static BumperTuning PartialBaseline { get; } = new(
        ContactRadius: 12,
        LaunchSpeed: 280m,
        CooldownTicks: 36);
}
