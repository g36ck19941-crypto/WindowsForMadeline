using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Puffer;

public sealed record PufferContact
{
    public PufferContact(string targetId, SimVector targetCenter, bool canTrigger = true)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Target ID is required.", nameof(targetId));
        }

        TargetId = targetId;
        TargetCenter = targetCenter;
        CanTrigger = canTrigger;
    }

    public string TargetId { get; }
    public SimVector TargetCenter { get; }
    public bool CanTrigger { get; }
}
