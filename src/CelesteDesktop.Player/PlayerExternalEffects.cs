using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Player;

public readonly record struct PlayerExternalEffects
{
    public PlayerExternalEffects(
        decimal? maximumFallSpeed,
        ExternalVelocityEffect? velocity = null,
        ExternalResourceEffect? resources = null)
    {
        if (maximumFallSpeed is <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFallSpeed));
        }
        MaximumFallSpeed = maximumFallSpeed;
        Velocity = velocity;
        Resources = resources;
    }

    public decimal? MaximumFallSpeed { get; }
    public ExternalVelocityEffect? Velocity { get; }
    public ExternalResourceEffect? Resources { get; }

    public static PlayerExternalEffects None { get; } = new(null, null, null);
}
