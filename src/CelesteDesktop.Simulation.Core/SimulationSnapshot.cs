using System.Collections.ObjectModel;

namespace CelesteDesktop.Simulation.Core;

public sealed record ActorSnapshot(
    string Id,
    SimPoint Position,
    int Width,
    int Height,
    decimal XSubpixel,
    decimal YSubpixel,
    SimVector LiftSpeed,
    bool IsSquished);

public sealed record SolidSnapshot(
    string Id,
    SimPoint Position,
    int Width,
    int Height,
    decimal XSubpixel,
    decimal YSubpixel);

public sealed class SimulationSnapshot
{
    public SimulationSnapshot(long tick, IEnumerable<ActorSnapshot> actors, IEnumerable<SolidSnapshot> solids)
    {
        Tick = tick;
        Actors = new ReadOnlyCollection<ActorSnapshot>(actors.ToArray());
        Solids = new ReadOnlyCollection<SolidSnapshot>(solids.ToArray());
    }

    public long Tick { get; }
    public IReadOnlyList<ActorSnapshot> Actors { get; }
    public IReadOnlyList<SolidSnapshot> Solids { get; }
}
