using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Player;

public sealed class PlayerNormalSnapshot
{
    public PlayerNormalSnapshot(
        long tick,
        SimPoint position,
        SimVector speed,
        int facing,
        bool grounded,
        decimal maxFall,
        decimal appliedMaximumFallSpeed,
        SimVector appliedLiftSpeed,
        decimal wallSpeedRetained,
        int wallSpeedRetentionTicks,
        int upwardCornerCorrectionX,
        string? groundedOneWayPlatformId,
        string? dropThroughPlatformId,
        int dropThroughTicksRemaining,
        int coyoteTicks,
        int jumpBufferTicks,
        int variableJumpTicks,
        IEnumerable<PlayerNormalEvent> events)
    {
        Tick = tick;
        Position = position;
        Speed = speed;
        Facing = facing;
        Grounded = grounded;
        MaxFall = maxFall;
        AppliedMaximumFallSpeed = appliedMaximumFallSpeed;
        AppliedLiftSpeed = appliedLiftSpeed;
        WallSpeedRetained = wallSpeedRetained;
        WallSpeedRetentionTicks = wallSpeedRetentionTicks;
        UpwardCornerCorrectionX = upwardCornerCorrectionX;
        GroundedOneWayPlatformId = groundedOneWayPlatformId;
        DropThroughPlatformId = dropThroughPlatformId;
        DropThroughTicksRemaining = dropThroughTicksRemaining;
        CoyoteTicks = coyoteTicks;
        JumpBufferTicks = jumpBufferTicks;
        VariableJumpTicks = variableJumpTicks;
        Events = new ReadOnlyCollection<PlayerNormalEvent>(events.ToArray());
    }

    public long Tick { get; }
    public SimPoint Position { get; }
    public SimVector Speed { get; }
    public int Facing { get; }
    public bool Grounded { get; }
    public decimal MaxFall { get; }
    public decimal AppliedMaximumFallSpeed { get; }
    public SimVector AppliedLiftSpeed { get; }
    public decimal WallSpeedRetained { get; }
    public int WallSpeedRetentionTicks { get; }
    public int UpwardCornerCorrectionX { get; }
    public string? GroundedOneWayPlatformId { get; }
    public string? DropThroughPlatformId { get; }
    public int DropThroughTicksRemaining { get; }
    public int CoyoteTicks { get; }
    public int JumpBufferTicks { get; }
    public int VariableJumpTicks { get; }
    public IReadOnlyList<PlayerNormalEvent> Events { get; }
}
