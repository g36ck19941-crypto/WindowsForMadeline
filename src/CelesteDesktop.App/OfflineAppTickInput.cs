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

namespace CelesteDesktop.App;

public sealed record OfflineAppTickInput
{
    public OfflineAppTickInput(
        long tick,
        PlayerInput player,
        AnimationTickInput animation,
        TheoCrystalInput theo = default,
        GliderInput glider = default,
        SpringInput spring = default,
        RefillInput refill = default,
        WaterInput? water = null,
        BumperInput bumper = default,
        PufferInput puffer = default,
        SeekerInput seeker = default)
    {
        if (tick <= 0) throw new ArgumentOutOfRangeException(nameof(tick));
        ArgumentNullException.ThrowIfNull(animation);
        if (animation.Tick != tick) throw new ArgumentException("Animation tick must equal the App tick.", nameof(animation));
        Tick = tick;
        Player = player;
        Animation = animation;
        Theo = theo;
        Glider = glider;
        Spring = spring;
        Refill = refill;
        Water = water ?? WaterInput.None;
        Bumper = bumper;
        Puffer = puffer;
        Seeker = seeker;
    }

    public long Tick { get; }
    public PlayerInput Player { get; }
    public AnimationTickInput Animation { get; }
    public TheoCrystalInput Theo { get; }
    public GliderInput Glider { get; }
    public SpringInput Spring { get; }
    public RefillInput Refill { get; }
    public WaterInput Water { get; }
    public BumperInput Bumper { get; }
    public PufferInput Puffer { get; }
    public SeekerInput Seeker { get; }
}
