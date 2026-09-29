namespace CelesteDesktop.Entity.Refill;

public sealed record RefillTuning(int RespawnTicks)
{
    public static RefillTuning PartialBaseline { get; } = new(150);
}
