using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Theo;

public sealed class TheoHolderSnapshot
{
    public TheoHolderSnapshot(
        string holderId,
        SimPoint holdPosition,
        int facing,
        SimVector liftSpeed = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(holderId);
        if (facing is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(facing));
        }

        HolderId = holderId;
        HoldPosition = holdPosition;
        Facing = facing;
        LiftSpeed = liftSpeed;
    }

    public string HolderId { get; }
    public SimPoint HoldPosition { get; }
    public int Facing { get; }
    public SimVector LiftSpeed { get; }
}
