using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Seeker;

public sealed record SeekerTarget
{
    public SeekerTarget(
        string targetId,
        SimVector center,
        bool canBeDetected = true,
        bool isTouching = false)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Target ID is required.", nameof(targetId));
        }

        TargetId = targetId;
        Center = center;
        CanBeDetected = canBeDetected;
        IsTouching = isTouching;
    }

    public string TargetId { get; }
    public SimVector Center { get; }
    public bool CanBeDetected { get; }
    public bool IsTouching { get; }
}
