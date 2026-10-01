using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Puffer;

public sealed class PufferController
{
    private readonly List<PufferEvent> _events = [];
    private readonly decimal _leftBound;
    private readonly decimal _rightBound;
    private readonly int _initialDirection;
    private long _lastUpdatedTick = -1;
    private bool _armed = true;
    private string? _lockedTargetId;
    private SimVector _lockedTargetCenter;

    public PufferController(
        string entityId,
        SimVector spawnCenter,
        decimal leftBound,
        decimal rightBound,
        int initialDirection = 1,
        PufferTuning? tuning = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }
        if (leftBound > spawnCenter.X || rightBound < spawnCenter.X || leftBound >= rightBound)
        {
            throw new ArgumentOutOfRangeException(nameof(leftBound), "Swim bounds must contain the spawn center and have positive width.");
        }
        if (initialDirection is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(initialDirection));
        }

        EntityId = entityId;
        SpawnCenter = spawnCenter;
        Center = spawnCenter;
        _leftBound = leftBound;
        _rightBound = rightBound;
        _initialDirection = initialDirection;
        SwimDirection = initialDirection;
        Tuning = tuning ?? PufferTuning.PartialBaseline;
        if (Tuning.SwimSpeed <= 0m || Tuning.TriggerRadius <= 0 || Tuning.WarningTicks <= 0 ||
            Tuning.LaunchSpeed <= 0m || Tuning.RespawnTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }

    public string EntityId { get; }
    public SimVector SpawnCenter { get; }
    public SimVector Center { get; private set; }
    public int SwimDirection { get; private set; }
    public PufferTuning Tuning { get; }
    public PufferState State { get; private set; }
    public int WarningTicksRemaining { get; private set; }
    public int RespawnTicksRemaining { get; private set; }

    public PufferSnapshot Step(PufferInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        PufferSnapshot? result = null;
        world.Step(current => result = Update(input, current));
        return result ?? throw new InvalidOperationException("Puffer step did not produce a snapshot.");
    }

    public PufferSnapshot Update(PufferInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Puffer update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Puffer can only update once per simulation tick.");
        }

        _lastUpdatedTick = world.Tick;
        _events.Clear();

        if (input.DisableRequested)
        {
            Disable(world.Tick);
            return Capture(world.Tick, null);
        }

        if (State == PufferState.Disabled)
        {
            ResetAtSpawn(input.Contact is null);
            AddEvent(world.Tick, PufferEventKind.Enabled, input.Contact?.TargetId);
            return Capture(world.Tick, null);
        }

        if (input.Contact is null && State == PufferState.Swimming)
        {
            _armed = true;
        }

        return State switch
        {
            PufferState.Swimming => UpdateSwimming(input.Contact, world.Tick),
            PufferState.Warning => UpdateWarning(input.Contact, world.Tick),
            PufferState.Spent => UpdateSpent(input.Contact, world.Tick),
            _ => throw new InvalidOperationException($"Unsupported Puffer state {State}.")
        };
    }

    private PufferSnapshot UpdateSwimming(PufferContact? contact, long tick)
    {
        AdvanceSwim(tick);
        if (contact is null)
        {
            return Capture(tick, null);
        }
        if (!_armed || !contact.CanTrigger || !IsInRange(contact.TargetCenter))
        {
            AddEvent(tick, PufferEventKind.ContactIgnored, contact.TargetId);
            return Capture(tick, null);
        }

        State = PufferState.Warning;
        WarningTicksRemaining = Tuning.WarningTicks;
        _armed = false;
        _lockedTargetId = contact.TargetId;
        _lockedTargetCenter = contact.TargetCenter;
        AddEvent(tick, PufferEventKind.WarningStarted, contact.TargetId);
        return Capture(tick, null);
    }

    private PufferSnapshot UpdateWarning(PufferContact? contact, long tick)
    {
        if (contact is not null && string.Equals(contact.TargetId, _lockedTargetId, StringComparison.Ordinal))
        {
            _lockedTargetCenter = contact.TargetCenter;
        }

        WarningTicksRemaining--;
        if (WarningTicksRemaining > 0)
        {
            return Capture(tick, null);
        }

        WarningTicksRemaining = 0;
        var direction = CalculateDirection(_lockedTargetCenter, tick);
        var effect = new PufferLaunchEffect(
            _lockedTargetId!,
            direction,
            new ExternalVelocityEffect(direction.X * Tuning.LaunchSpeed, direction.Y * Tuning.LaunchSpeed));
        State = PufferState.Spent;
        RespawnTicksRemaining = Tuning.RespawnTicks;
        AddEvent(tick, PufferEventKind.Exploded, _lockedTargetId);
        AddEvent(tick, PufferEventKind.LaunchIssued, _lockedTargetId);
        AddEvent(tick, PufferEventKind.SpentStarted, _lockedTargetId);
        return Capture(tick, effect);
    }

    private PufferSnapshot UpdateSpent(PufferContact? contact, long tick)
    {
        RespawnTicksRemaining--;
        if (RespawnTicksRemaining > 0)
        {
            if (contact is not null)
            {
                AddEvent(tick, PufferEventKind.ContactIgnored, contact.TargetId);
            }
            return Capture(tick, null);
        }

        RespawnTicksRemaining = 0;
        ResetAtSpawn(contact is null);
        AddEvent(tick, PufferEventKind.Respawned, contact?.TargetId);
        return Capture(tick, null);
    }

    private void AdvanceSwim(long tick)
    {
        var nextX = Center.X + (SwimDirection * Tuning.SwimSpeed / 60m);
        var turned = false;
        if (nextX >= _rightBound)
        {
            nextX = _rightBound;
            SwimDirection = -1;
            turned = true;
        }
        else if (nextX <= _leftBound)
        {
            nextX = _leftBound;
            SwimDirection = 1;
            turned = true;
        }

        Center = new SimVector(nextX, Center.Y);
        AddEvent(tick, PufferEventKind.Swam, null);
        if (turned)
        {
            AddEvent(tick, PufferEventKind.Turned, null);
        }
    }

    private bool IsInRange(SimVector targetCenter)
    {
        var deltaX = targetCenter.X - Center.X;
        var deltaY = targetCenter.Y - Center.Y;
        var radius = (decimal)Tuning.TriggerRadius;
        return (deltaX * deltaX) + (deltaY * deltaY) <= radius * radius;
    }

    private SimVector CalculateDirection(SimVector targetCenter, long tick)
    {
        var deltaX = targetCenter.X - Center.X;
        var deltaY = targetCenter.Y - Center.Y;
        if (deltaX == 0m && deltaY == 0m)
        {
            AddEvent(tick, PufferEventKind.CenterFallbackUsed, _lockedTargetId);
            return new SimVector(0m, -1m);
        }

        var magnitude = SquareRoot((deltaX * deltaX) + (deltaY * deltaY));
        return new SimVector(deltaX / magnitude, deltaY / magnitude);
    }

    private void Disable(long tick)
    {
        if (State == PufferState.Disabled)
        {
            return;
        }

        State = PufferState.Disabled;
        WarningTicksRemaining = 0;
        RespawnTicksRemaining = 0;
        _armed = false;
        _lockedTargetId = null;
        _lockedTargetCenter = default;
        AddEvent(tick, PufferEventKind.Disabled, null);
    }

    private void ResetAtSpawn(bool armed)
    {
        Center = SpawnCenter;
        SwimDirection = _initialDirection;
        State = PufferState.Swimming;
        WarningTicksRemaining = 0;
        RespawnTicksRemaining = 0;
        _armed = armed;
        _lockedTargetId = null;
        _lockedTargetCenter = default;
    }

    private PufferSnapshot Capture(long tick, PufferLaunchEffect? effect) => new(
        tick,
        EntityId,
        SpawnCenter,
        Center,
        SwimDirection,
        State,
        WarningTicksRemaining,
        RespawnTicksRemaining,
        _armed,
        _lockedTargetId,
        effect,
        _events);

    private void AddEvent(long tick, PufferEventKind kind, string? targetId) =>
        _events.Add(new PufferEvent(tick, PufferEventIds.For(kind), kind, EntityId, Center, targetId));

    private static decimal SquareRoot(decimal value)
    {
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        var current = value >= 1m ? value : 1m;
        for (var iteration = 0; iteration < 32; iteration++)
        {
            var next = (current + (value / current)) / 2m;
            if (next == current)
            {
                break;
            }
            current = next;
        }
        return current;
    }
}
