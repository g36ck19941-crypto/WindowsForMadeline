using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Spring;

public sealed record SpringLaunchEffect(
    string TargetId,
    SpringTargetKind TargetKind,
    ExternalVelocityEffect Velocity);

public sealed class SpringSnapshot
{
    public SpringSnapshot(
        long tick,
        string entityId,
        SimPoint position,
        SpringOrientation orientation,
        SpringState state,
        int retractedTicksRemaining,
        int cooldownTicksRemaining,
        bool armed,
        SpringLaunchEffect? launchEffect,
        IEnumerable<SpringEvent> events)
    {
        Tick = tick;
        EntityId = entityId;
        Position = position;
        Orientation = orientation;
        State = state;
        RetractedTicksRemaining = retractedTicksRemaining;
        CooldownTicksRemaining = cooldownTicksRemaining;
        Armed = armed;
        LaunchEffect = launchEffect;
        Events = new ReadOnlyCollection<SpringEvent>(events.ToArray());
    }

    public long Tick { get; }
    public string EntityId { get; }
    public SimPoint Position { get; }
    public SpringOrientation Orientation { get; }
    public SpringState State { get; }
    public int RetractedTicksRemaining { get; }
    public int CooldownTicksRemaining { get; }
    public bool Armed { get; }
    public SpringLaunchEffect? LaunchEffect { get; }
    public IReadOnlyList<SpringEvent> Events { get; }
}
