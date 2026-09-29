namespace CelesteDesktop.Player;

public readonly record struct PlayerExternalEffects
{
    public PlayerExternalEffects(decimal? maximumFallSpeed)
    {
        if (maximumFallSpeed is <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFallSpeed));
        }
        MaximumFallSpeed = maximumFallSpeed;
    }

    public decimal? MaximumFallSpeed { get; }

    public static PlayerExternalEffects None { get; } = new(null);
}
