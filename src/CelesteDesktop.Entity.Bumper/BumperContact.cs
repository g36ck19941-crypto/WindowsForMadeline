using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Bumper;

public sealed record BumperContact
{
    public BumperContact(string targetId, SimPoint targetCenter, bool canActivate = true)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Target ID is required.", nameof(targetId));
        }

        TargetId = targetId;
        TargetCenter = targetCenter;
        CanActivate = canActivate;
    }

    public string TargetId { get; }
    public SimPoint TargetCenter { get; }
    public bool CanActivate { get; }
}
