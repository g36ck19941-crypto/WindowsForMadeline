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

public sealed record OneWayPlatformSnapshot(
    string Id,
    SimPoint Position,
    int Width,
    int Height);

public sealed class SimulationSnapshot
{
    public SimulationSnapshot(
        long tick,
        IEnumerable<ActorSnapshot> actors,
        IEnumerable<SolidSnapshot> solids,
        IEnumerable<OneWayPlatformSnapshot>? oneWayPlatforms = null)
    {
        Tick = tick;
        Actors = new ReadOnlyCollection<ActorSnapshot>(actors.ToArray());
        Solids = new ReadOnlyCollection<SolidSnapshot>(solids.ToArray());
        OneWayPlatforms = new ReadOnlyCollection<OneWayPlatformSnapshot>(
            (oneWayPlatforms ?? []).ToArray());
    }

    public long Tick { get; }
    public IReadOnlyList<ActorSnapshot> Actors { get; }
    public IReadOnlyList<SolidSnapshot> Solids { get; }
    public IReadOnlyList<OneWayPlatformSnapshot> OneWayPlatforms { get; }
}
