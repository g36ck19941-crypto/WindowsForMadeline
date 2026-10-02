namespace CelesteDesktop.Player;

public sealed record NormalJumpTuning(
    decimal MaxRun,
    decimal RunAcceleration,
    decimal OverspeedReduction,
    decimal AirControlMultiplier,
    decimal Gravity,
    decimal HalfGravityThreshold,
    decimal NormalMaxFall,
    decimal FastMaxFall,
    decimal FastFallAcceleration,
    decimal JumpSpeed,
    decimal JumpHorizontalBoost,
    decimal MaximumHorizontalLiftSpeed,
    decimal MaximumUpwardLiftSpeed,
    int CoyoteTicks,
    int JumpBufferTicks,
    int VariableJumpTicks)
{
    public static NormalJumpTuning ReferencePartial { get; } = new(
        MaxRun: 90m,
        RunAcceleration: 1000m,
        OverspeedReduction: 400m,
        AirControlMultiplier: 0.65m,
        Gravity: 900m,
        HalfGravityThreshold: 40m,
        NormalMaxFall: 160m,
        FastMaxFall: 240m,
        FastFallAcceleration: 300m,
        JumpSpeed: -105m,
        JumpHorizontalBoost: 40m,
        MaximumHorizontalLiftSpeed: 250m,
        MaximumUpwardLiftSpeed: 130m,
        CoyoteTicks: 6,
        JumpBufferTicks: 5,
        VariableJumpTicks: 12);
}
