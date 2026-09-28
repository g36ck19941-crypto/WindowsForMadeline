namespace CelesteDesktop.Simulation.Core;

public sealed class SubpixelAccumulator
{
    private decimal _remainder;

    public decimal Remainder => _remainder;

    public int Consume(decimal displacement)
    {
        var accumulated = checked(_remainder + displacement);
        var rounded = decimal.Round(accumulated, 0, MidpointRounding.AwayFromZero);
        if (rounded > int.MaxValue || rounded < int.MinValue)
        {
            throw new OverflowException("Simulation displacement exceeds the whole-pixel range.");
        }

        var pixels = decimal.ToInt32(rounded);
        _remainder = accumulated - pixels;
        return pixels;
    }

    public void Reset() => _remainder = 0m;
}
