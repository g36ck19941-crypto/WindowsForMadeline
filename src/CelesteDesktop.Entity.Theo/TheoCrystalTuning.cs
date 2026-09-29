namespace CelesteDesktop.Entity.Theo;

public sealed record TheoCrystalTuning(
    decimal Gravity,
    decimal MaximumFallSpeed,
    decimal HorizontalFriction,
    decimal ThrowSpeedX,
    decimal ThrowSpeedY,
    decimal HorizontalBounceFactor,
    decimal LandingBounceFactor,
    decimal MinimumHorizontalBounceSpeed,
    decimal MinimumLandingBounceSpeed)
{
    // These values are an independently designed offline baseline. The official
    // public Celeste repository does not include TheoCrystal entity source, so
    // CDR-040 deliberately classifies behavior fidelity as partial.
    public static TheoCrystalTuning PartialBaseline { get; } = new(
        Gravity: 800m,
        MaximumFallSpeed: 200m,
        HorizontalFriction: 350m,
        ThrowSpeedX: 200m,
        ThrowSpeedY: -80m,
        HorizontalBounceFactor: 0.4m,
        LandingBounceFactor: 0.2m,
        MinimumHorizontalBounceSpeed: 40m,
        MinimumLandingBounceSpeed: 40m);
}
