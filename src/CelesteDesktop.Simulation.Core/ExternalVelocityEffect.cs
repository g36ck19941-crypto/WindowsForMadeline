namespace CelesteDesktop.Simulation.Core;

public readonly record struct ExternalVelocityEffect
{
    public ExternalVelocityEffect(decimal? speedX, decimal? speedY)
    {
        if (speedX is null && speedY is null)
        {
            throw new ArgumentException("At least one velocity axis must be supplied.");
        }

        SpeedX = speedX;
        SpeedY = speedY;
    }

    public decimal? SpeedX { get; }
    public decimal? SpeedY { get; }

    public SimVector Apply(SimVector current) => new(
        SpeedX ?? current.X,
        SpeedY ?? current.Y);
}
