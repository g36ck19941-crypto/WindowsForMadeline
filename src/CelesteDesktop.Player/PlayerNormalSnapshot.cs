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
    public int CoyoteTicks { get; }
    public int JumpBufferTicks { get; }
    public int VariableJumpTicks { get; }
    public IReadOnlyList<PlayerNormalEvent> Events { get; }
}
