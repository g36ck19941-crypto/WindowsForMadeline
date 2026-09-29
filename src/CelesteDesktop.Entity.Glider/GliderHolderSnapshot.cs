using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Glider;

public sealed class GliderHolderSnapshot
{
    public GliderHolderSnapshot(
        string holderId,
        SimPoint holdPosition,
        int facing,
        SimVector liftSpeed = default,
        decimal holderVerticalSpeed = 0m)
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
        HolderVerticalSpeed = holderVerticalSpeed;
    }

    public string HolderId { get; }
    public SimPoint HoldPosition { get; }
    public int Facing { get; }
    public SimVector LiftSpeed { get; }
    public decimal HolderVerticalSpeed { get; }
}
