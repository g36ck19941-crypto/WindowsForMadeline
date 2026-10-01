using System.Collections.ObjectModel;
using CelesteDesktop.Animation;
using CelesteDesktop.Entity.Bumper;
using CelesteDesktop.Entity.Glider;
using CelesteDesktop.Entity.Puffer;
using CelesteDesktop.Entity.Refill;
using CelesteDesktop.Entity.Seeker;
using CelesteDesktop.Entity.Spring;
using CelesteDesktop.Entity.Theo;
using CelesteDesktop.Entity.Water;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.App;

public sealed class OfflineAppSnapshot
{
    public OfflineAppSnapshot(
        long tick,
        AppLifecycleState lifecycle,
        SimulationSnapshot simulation,
        PlayerTraversalSnapshot player,
        TheoCrystalSnapshot? theo,
        GliderSnapshot? glider,
        SpringSnapshot? spring,
        RefillSnapshot? refill,
        WaterSnapshot? water,
        BumperSnapshot? bumper,
        PufferSnapshot? puffer,
        SeekerSnapshot? seeker,
        OfflineAnimationPresentationResult? presentation,
        IEnumerable<string> disabledComponents,
        IEnumerable<AppDiagnosticEvent> events)
    {
        Tick = tick;
        Lifecycle = lifecycle;
        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Theo = theo;
        Glider = glider;
        Spring = spring;
        Refill = refill;
        Water = water;
        Bumper = bumper;
        Puffer = puffer;
        Seeker = seeker;
        Presentation = presentation;
        DisabledComponents = new ReadOnlyCollection<string>(disabledComponents.Order(StringComparer.Ordinal).ToArray());
        Events = new ReadOnlyCollection<AppDiagnosticEvent>(events.ToArray());
    }

    public long Tick { get; }
    public AppLifecycleState Lifecycle { get; }
    public SimulationSnapshot Simulation { get; }
    public PlayerTraversalSnapshot Player { get; }
    public TheoCrystalSnapshot? Theo { get; }
    public GliderSnapshot? Glider { get; }
    public SpringSnapshot? Spring { get; }
    public RefillSnapshot? Refill { get; }
    public WaterSnapshot? Water { get; }
    public BumperSnapshot? Bumper { get; }
    public PufferSnapshot? Puffer { get; }
    public SeekerSnapshot? Seeker { get; }
    public OfflineAnimationPresentationResult? Presentation { get; }
    public IReadOnlyList<string> DisabledComponents { get; }
    public IReadOnlyList<AppDiagnosticEvent> Events { get; }
}
