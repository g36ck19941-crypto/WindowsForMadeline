using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Glider;

public sealed record GliderHolderEffect(
    string HolderId,
    decimal MaximumFallSpeed,
    bool LimitRequired);

public sealed class GliderSnapshot
{
    public GliderSnapshot(
        long tick,
        GliderState state,
        SimPoint position,
        SimVector speed,
        bool grounded,
        bool isOpen,
        string? holderId,
        GliderHolderEffect? holderEffect,
        SimVector liftSpeed,
        IEnumerable<GliderEvent> events)
    {
        Tick = tick;
        State = state;
        Position = position;
        Speed = speed;
        Grounded = grounded;
        IsOpen = isOpen;
        HolderId = holderId;
        HolderEffect = holderEffect;
        LiftSpeed = liftSpeed;
        Events = new ReadOnlyCollection<GliderEvent>(events.ToArray());
    }

    public long Tick { get; }
    public GliderState State { get; }
    public SimPoint Position { get; }
    public SimVector Speed { get; }
    public bool Grounded { get; }
    public bool IsOpen { get; }
    public string? HolderId { get; }
    public GliderHolderEffect? HolderEffect { get; }
    public SimVector LiftSpeed { get; }
    public IReadOnlyList<GliderEvent> Events { get; }
}
