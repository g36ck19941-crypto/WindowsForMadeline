using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Water;

public sealed record WaterMotionEffect(
    string TargetId,
    ExternalVelocityEffect Velocity);

public sealed class WaterSnapshot
{
    public WaterSnapshot(
        long tick,
        string entityId,
        SimRect bounds,
        WaterState state,
        IEnumerable<string> occupants,
        IEnumerable<WaterMotionEffect> motionEffects,
        IEnumerable<WaterEvent> events)
    {
        Tick = tick;
        EntityId = entityId;
        Bounds = bounds;
        State = state;
        Occupants = new ReadOnlyCollection<string>(occupants.ToArray());
        MotionEffects = new ReadOnlyCollection<WaterMotionEffect>(motionEffects.ToArray());
        Events = new ReadOnlyCollection<WaterEvent>(events.ToArray());
    }

    public long Tick { get; }
    public string EntityId { get; }
    public SimRect Bounds { get; }
    public WaterState State { get; }
    public IReadOnlyList<string> Occupants { get; }
    public IReadOnlyList<WaterMotionEffect> MotionEffects { get; }
    public IReadOnlyList<WaterEvent> Events { get; }
}
