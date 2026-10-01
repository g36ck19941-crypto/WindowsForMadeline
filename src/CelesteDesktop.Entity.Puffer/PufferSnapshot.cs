using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Puffer;

public sealed record PufferLaunchEffect(
    string TargetId,
    SimVector Direction,
    ExternalVelocityEffect Velocity);

public sealed class PufferSnapshot
{
    public PufferSnapshot(
        long tick,
        string entityId,
        SimVector spawnCenter,
        SimVector center,
        int swimDirection,
        PufferState state,
        int warningTicksRemaining,
        int respawnTicksRemaining,
        bool armed,
        string? lockedTargetId,
        PufferLaunchEffect? launchEffect,
        IEnumerable<PufferEvent> events)
    {
        Tick = tick;
        EntityId = entityId;
        SpawnCenter = spawnCenter;
        Center = center;
        SwimDirection = swimDirection;
        State = state;
        WarningTicksRemaining = warningTicksRemaining;
        RespawnTicksRemaining = respawnTicksRemaining;
        Armed = armed;
        LockedTargetId = lockedTargetId;
        LaunchEffect = launchEffect;
        Events = new ReadOnlyCollection<PufferEvent>(events.ToArray());
    }

    public long Tick { get; }
    public string EntityId { get; }
    public SimVector SpawnCenter { get; }
    public SimVector Center { get; }
    public int SwimDirection { get; }
    public PufferState State { get; }
    public int WarningTicksRemaining { get; }
    public int RespawnTicksRemaining { get; }
    public bool Armed { get; }
    public string? LockedTargetId { get; }
    public PufferLaunchEffect? LaunchEffect { get; }
    public IReadOnlyList<PufferEvent> Events { get; }
}
