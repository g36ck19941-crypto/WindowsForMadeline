using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Refill;

public sealed record RefillContact
{
    public RefillContact(
        string targetId,
        SimPoint position,
        int currentDashes,
        int maximumDashes,
        decimal currentStamina,
        decimal maximumStamina,
        bool canActivate = true)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Target ID is required.", nameof(targetId));
        }
        if (maximumDashes <= 0 || currentDashes < 0 || currentDashes > maximumDashes)
        {
            throw new ArgumentOutOfRangeException(nameof(currentDashes));
        }
        if (maximumStamina <= 0m || currentStamina < 0m || currentStamina > maximumStamina)
        {
            throw new ArgumentOutOfRangeException(nameof(currentStamina));
        }

        TargetId = targetId;
        Position = position;
        CurrentDashes = currentDashes;
        MaximumDashes = maximumDashes;
        CurrentStamina = currentStamina;
        MaximumStamina = maximumStamina;
        CanActivate = canActivate;
    }

    public string TargetId { get; }
    public SimPoint Position { get; }
    public int CurrentDashes { get; }
    public int MaximumDashes { get; }
    public decimal CurrentStamina { get; }
    public decimal MaximumStamina { get; }
    public bool CanActivate { get; }
    public bool NeedsResources => CurrentDashes < MaximumDashes || CurrentStamina < MaximumStamina;
}
