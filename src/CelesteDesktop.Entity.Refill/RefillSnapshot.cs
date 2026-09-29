using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Refill;

public sealed record RefillRestoreEffect(
    string TargetId,
    ExternalResourceEffect Resources);

public sealed class RefillSnapshot
{
    public RefillSnapshot(
        long tick,
        string entityId,
        SimPoint position,
        RefillState state,
        int respawnTicksRemaining,
        bool armed,
        RefillRestoreEffect? restoreEffect,
        IEnumerable<RefillEvent> events)
    {
        Tick = tick;
        EntityId = entityId;
        Position = position;
        State = state;
        RespawnTicksRemaining = respawnTicksRemaining;
        Armed = armed;
        RestoreEffect = restoreEffect;
        Events = new ReadOnlyCollection<RefillEvent>(events.ToArray());
    }

    public long Tick { get; }
    public string EntityId { get; }
    public SimPoint Position { get; }
    public RefillState State { get; }
    public int RespawnTicksRemaining { get; }
    public bool Armed { get; }
    public RefillRestoreEffect? RestoreEffect { get; }
    public IReadOnlyList<RefillEvent> Events { get; }
}
