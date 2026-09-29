using System.Collections.ObjectModel;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Theo;

public sealed class TheoCrystalSnapshot
{
    public TheoCrystalSnapshot(
        long tick,
        TheoCrystalState state,
        SimPoint position,
        SimVector speed,
        bool grounded,
        string? holderId,
        SimVector liftSpeed,
        IEnumerable<TheoCrystalEvent> events)
    {
        Tick = tick;
        State = state;
        Position = position;
        Speed = speed;
        Grounded = grounded;
        HolderId = holderId;
        LiftSpeed = liftSpeed;
        Events = new ReadOnlyCollection<TheoCrystalEvent>(events.ToArray());
    }

    public long Tick { get; }
    public TheoCrystalState State { get; }
    public SimPoint Position { get; }
    public SimVector Speed { get; }
    public bool Grounded { get; }
    public string? HolderId { get; }
    public SimVector LiftSpeed { get; }
    public IReadOnlyList<TheoCrystalEvent> Events { get; }
}
