using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Water;

public sealed record WaterContact
{
    public WaterContact(
        string targetId,
        SimRect bounds,
        SimVector velocity,
        int moveX,
        int moveY,
        bool canSwim = true)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Target ID is required.", nameof(targetId));
        }
        if (moveX is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moveX));
        }
        if (moveY is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moveY));
        }

        TargetId = targetId;
        Bounds = bounds;
        Velocity = velocity;
        MoveX = moveX;
        MoveY = moveY;
        CanSwim = canSwim;
    }

    public string TargetId { get; }
    public SimRect Bounds { get; }
    public SimVector Velocity { get; }
    public int MoveX { get; }
    public int MoveY { get; }
    public bool CanSwim { get; }
}
