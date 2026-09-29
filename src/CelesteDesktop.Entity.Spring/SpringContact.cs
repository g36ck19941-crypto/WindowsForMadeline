using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Spring;

public sealed record SpringContact
{
    public SpringContact(
        string targetId,
        SpringTargetKind targetKind,
        SimPoint targetPosition,
        SimVector incomingVelocity,
        bool canActivate = true)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Target ID is required.", nameof(targetId));
        }

        TargetId = targetId;
        TargetKind = targetKind;
        TargetPosition = targetPosition;
        IncomingVelocity = incomingVelocity;
        CanActivate = canActivate;
    }

    public string TargetId { get; }
    public SpringTargetKind TargetKind { get; }
    public SimPoint TargetPosition { get; }
    public SimVector IncomingVelocity { get; }
    public bool CanActivate { get; }
}
