using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Seeker;

public sealed record SeekerHitEffect(string TargetId, SimVector ImpactDirection);

public sealed class SeekerSnapshot
{
    public SeekerSnapshot(
        long tick,
        string entityId,
        SimVector spawnCenter,
        SimVector center,
        SeekerState state,
        int patrolDirection,
        int stateTicksRemaining,
        int lostSightTicks,
        string? lockedTargetId,
        SimVector dashDirection,
        SeekerHitEffect? hitEffect,
        IEnumerable<SeekerEvent> events)
    {
        Tick = tick;
        EntityId = entityId;
        SpawnCenter = spawnCenter;
        Center = center;
        State = state;
        PatrolDirection = patrolDirection;
        StateTicksRemaining = stateTicksRemaining;
        LostSightTicks = lostSightTicks;
        LockedTargetId = lockedTargetId;
        DashDirection = dashDirection;
        HitEffect = hitEffect;
        Events = new ReadOnlyCollection<SeekerEvent>(events.ToArray());
    }

    public long Tick { get; }
    public string EntityId { get; }
    public SimVector SpawnCenter { get; }
    public SimVector Center { get; }
    public SeekerState State { get; }
    public int PatrolDirection { get; }
    public int StateTicksRemaining { get; }
    public int LostSightTicks { get; }
    public string? LockedTargetId { get; }
    public SimVector DashDirection { get; }
    public SeekerHitEffect? HitEffect { get; }
    public IReadOnlyList<SeekerEvent> Events { get; }
}
