using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Bumper;

public sealed record BumperLaunchEffect(
    string TargetId,
    SimVector Direction,
    ExternalVelocityEffect Velocity);

public sealed class BumperSnapshot
{
    public BumperSnapshot(
        long tick,
        string entityId,
        SimPoint center,
        BumperState state,
        int cooldownTicksRemaining,
        bool armed,
        BumperLaunchEffect? launchEffect,
        IEnumerable<BumperEvent> events)
    {
        Tick = tick;
        EntityId = entityId;
        Center = center;
        State = state;
        CooldownTicksRemaining = cooldownTicksRemaining;
        Armed = armed;
        LaunchEffect = launchEffect;
        Events = new ReadOnlyCollection<BumperEvent>(events.ToArray());
    }

    public long Tick { get; }
    public string EntityId { get; }
    public SimPoint Center { get; }
    public BumperState State { get; }
    public int CooldownTicksRemaining { get; }
    public bool Armed { get; }
    public BumperLaunchEffect? LaunchEffect { get; }
    public IReadOnlyList<BumperEvent> Events { get; }
}
