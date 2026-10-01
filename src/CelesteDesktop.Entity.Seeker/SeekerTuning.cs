namespace CelesteDesktop.Entity.Seeker;

public sealed record SeekerTuning(
    decimal PatrolSpeed,
    decimal ChaseSpeed,
    decimal DashSpeed,
    int DetectionRadius,
    int DashTriggerRadius,
    int AlertTicks,
    int WindupTicks,
    int DashTicks,
    int StunTicks,
    int ForgetTicks)
{
    // Independently designed deterministic baseline. Original commercial
    // Seeker movement, navigation, contact, timing and numeric behavior are not established here.
    public static SeekerTuning PartialBaseline { get; } = new(
        PatrolSpeed: 24m,
        ChaseSpeed: 72m,
        DashSpeed: 240m,
        DetectionRadius: 64,
        DashTriggerRadius: 20,
        AlertTicks: 12,
        WindupTicks: 8,
        DashTicks: 18,
        StunTicks: 45,
        ForgetTicks: 30);
}
