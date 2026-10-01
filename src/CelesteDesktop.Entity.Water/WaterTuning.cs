namespace CelesteDesktop.Entity.Water;

public sealed record WaterTuning(
    decimal MaximumHorizontalSpeed,
    decimal MaximumVerticalSpeed,
    decimal NeutralBuoyancySpeed,
    decimal HorizontalAcceleration,
    decimal VerticalAcceleration)
{
    public static WaterTuning PartialBaseline { get; } = new(
        MaximumHorizontalSpeed: 60m,
        MaximumVerticalSpeed: 60m,
        NeutralBuoyancySpeed: 20m,
        HorizontalAcceleration: 300m,
        VerticalAcceleration: 240m);
}
