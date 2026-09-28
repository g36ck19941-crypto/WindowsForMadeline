using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Player;

public sealed class PlayerTraversalSnapshot
{
    public PlayerTraversalSnapshot(
        long tick,
        SimPoint position,
        SimVector speed,
        int facing,
        bool grounded,
        PlayerTraversalState state,
        int dashes,
        decimal stamina,
        SimVector dashDirection,
        int dashTicks,
        int dashCooldownTicks,
        int dashAttackTicks,
        int wallSlideTicks,
        int climbNoMoveTicks,
        decimal tiredThreshold,
        IEnumerable<PlayerTraversalEvent> events)
    {
        Tick = tick;
        Position = position;
        Speed = speed;
        Facing = facing;
        Grounded = grounded;
        State = state;
        Dashes = dashes;
        Stamina = stamina;
        DashDirection = dashDirection;
        DashTicks = dashTicks;
        DashCooldownTicks = dashCooldownTicks;
        DashAttackTicks = dashAttackTicks;
        WallSlideTicks = wallSlideTicks;
        ClimbNoMoveTicks = climbNoMoveTicks;
        IsTired = stamina < tiredThreshold;
        Events = new ReadOnlyCollection<PlayerTraversalEvent>(events.ToArray());
    }

    public long Tick { get; }
    public SimPoint Position { get; }
    public SimVector Speed { get; }
    public int Facing { get; }
    public bool Grounded { get; }
    public PlayerTraversalState State { get; }
    public int Dashes { get; }
    public decimal Stamina { get; }
    public SimVector DashDirection { get; }
    public int DashTicks { get; }
    public int DashCooldownTicks { get; }
    public int DashAttackTicks { get; }
    public int WallSlideTicks { get; }
    public int ClimbNoMoveTicks { get; }
    public bool IsTired { get; }
    public IReadOnlyList<PlayerTraversalEvent> Events { get; }
}
