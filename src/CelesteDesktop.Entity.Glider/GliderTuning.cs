namespace CelesteDesktop.Entity.Glider;

public sealed record GliderTuning(
    decimal Gravity,
    decimal MaximumFallSpeed,
    decimal HorizontalFriction,
    decimal ThrowSpeedX,
    decimal ThrowSpeedY,
    decimal HorizontalBounceFactor,
    decimal LandingBounceFactor,
    decimal MinimumHorizontalBounceSpeed,
    decimal MinimumLandingBounceSpeed,
    decimal MaximumHolderFallSpeed,
    decimal OpenFallSpeedThreshold)
{
    // Independently designed deterministic baseline. The official public
    // Celeste repository does not publish the shipped Glider entity behavior.
    public static GliderTuning PartialBaseline { get; } = new(
        Gravity: 300m,
        MaximumFallSpeed: 30m,
        HorizontalFriction: 100m,
        ThrowSpeedX: 120m,
        ThrowSpeedY: -40m,
        HorizontalBounceFactor: 0.5m,
        LandingBounceFactor: 0.25m,
        MinimumHorizontalBounceSpeed: 30m,
        MinimumLandingBounceSpeed: 20m,
        MaximumHolderFallSpeed: 40m,
        OpenFallSpeedThreshold: 0m);
}
