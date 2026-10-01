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

public sealed class OfflineAppComponents
{
    public OfflineAppComponents(
        SimulationWorld world,
        PlayerTraversalController player,
        TheoCrystalController theo,
        GliderController glider,
        SpringController spring,
        RefillController refill,
        WaterController water,
        BumperController bumper,
        PufferController puffer,
        SeekerController seeker)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Theo = theo ?? throw new ArgumentNullException(nameof(theo));
        Glider = glider ?? throw new ArgumentNullException(nameof(glider));
        Spring = spring ?? throw new ArgumentNullException(nameof(spring));
        Refill = refill ?? throw new ArgumentNullException(nameof(refill));
        Water = water ?? throw new ArgumentNullException(nameof(water));
        Bumper = bumper ?? throw new ArgumentNullException(nameof(bumper));
        Puffer = puffer ?? throw new ArgumentNullException(nameof(puffer));
        Seeker = seeker ?? throw new ArgumentNullException(nameof(seeker));

        foreach (var actor in new[] { Player.Actor, Theo.Actor, Glider.Actor })
        {
            if (!World.Actors.Contains(actor))
            {
                throw new ArgumentException($"Actor {actor.Id} must be registered in the App simulation world.", nameof(world));
            }
        }

        var ids = new[]
        {
            Player.Actor.Id,
            Theo.Actor.Id,
            Glider.Actor.Id,
            Spring.EntityId,
            Refill.EntityId,
            Water.EntityId,
            Bumper.EntityId,
            Puffer.EntityId,
            Seeker.EntityId
        };
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            throw new ArgumentException("App component IDs must be unique.");
        }
    }

    public SimulationWorld World { get; }
    public PlayerTraversalController Player { get; }
    public TheoCrystalController Theo { get; }
    public GliderController Glider { get; }
    public SpringController Spring { get; }
    public RefillController Refill { get; }
    public WaterController Water { get; }
    public BumperController Bumper { get; }
    public PufferController Puffer { get; }
    public SeekerController Seeker { get; }
}
