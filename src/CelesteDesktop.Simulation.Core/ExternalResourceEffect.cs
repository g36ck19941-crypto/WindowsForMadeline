namespace CelesteDesktop.Simulation.Core;

public readonly record struct ExternalResourceEffect
{
    public ExternalResourceEffect(int? chargeCount, decimal? stamina)
    {
        if (chargeCount is null && stamina is null)
        {
            throw new ArgumentException("At least one resource value must be supplied.");
        }
        if (chargeCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chargeCount));
        }
        if (stamina is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(stamina));
        }

        ChargeCount = chargeCount;
        Stamina = stamina;
    }

    public int? ChargeCount { get; }
    public decimal? Stamina { get; }
}
