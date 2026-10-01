using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Water;

public sealed class WaterController
{
    private readonly HashSet<string> _occupants = new(StringComparer.Ordinal);
    private readonly List<WaterMotionEffect> _motionEffects = [];
    private readonly List<WaterEvent> _events = [];
    private long _lastUpdatedTick = -1;

    public WaterController(string entityId, SimRect bounds, WaterTuning? tuning = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }

        EntityId = entityId;
        Bounds = bounds;
        Tuning = tuning ?? WaterTuning.PartialBaseline;
        ValidateTuning(Tuning);
    }

    public string EntityId { get; }
    public SimRect Bounds { get; }
    public WaterTuning Tuning { get; }
    public WaterState State { get; private set; }

    public WaterSnapshot Step(WaterInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(world);
        WaterSnapshot? result = null;
        world.Step(current => result = Update(input, current));
        return result ?? throw new InvalidOperationException("Water step did not produce a snapshot.");
    }

    public WaterSnapshot Update(WaterInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Water update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Water can only update once per simulation tick.");
        }

        _lastUpdatedTick = world.Tick;
        _events.Clear();
        _motionEffects.Clear();

        if (input.DisableRequested)
        {
            Disable(world.Tick);
            return Capture(world.Tick);
        }

        if (State == WaterState.Disabled)
        {
            State = WaterState.Active;
            AddEvent(world.Tick, WaterEventKind.Enabled, null);
        }

        var ordered = input.Contacts.OrderBy(item => item.TargetId, StringComparer.Ordinal).ToArray();
        var active = ordered
            .Where(item => item.CanSwim && Bounds.Intersects(item.Bounds))
            .Select(item => item.TargetId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var exited in _occupants.Except(active, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            AddEvent(world.Tick, WaterEventKind.Exited, exited);
        }

        foreach (var contact in ordered)
        {
            if (!active.Contains(contact.TargetId))
            {
                AddEvent(world.Tick, WaterEventKind.ContactIgnored, contact.TargetId);
                continue;
            }

            if (!_occupants.Contains(contact.TargetId))
            {
                AddEvent(world.Tick, WaterEventKind.Entered, contact.TargetId);
            }
            AddEvent(world.Tick, WaterEventKind.Submerged, contact.TargetId);
            _motionEffects.Add(new WaterMotionEffect(contact.TargetId, CalculateVelocity(contact)));
            AddEvent(world.Tick, WaterEventKind.MotionIssued, contact.TargetId);
        }

        _occupants.Clear();
        _occupants.UnionWith(active);
        return Capture(world.Tick);
    }

    private ExternalVelocityEffect CalculateVelocity(WaterContact contact)
    {
        var targetX = contact.MoveX * Tuning.MaximumHorizontalSpeed;
        var targetY = contact.MoveY switch
        {
            -1 => -Tuning.MaximumVerticalSpeed,
            1 => Tuning.MaximumVerticalSpeed,
            _ => -Tuning.NeutralBuoyancySpeed
        };
        var nextX = Approach(
            contact.Velocity.X,
            targetX,
            Tuning.HorizontalAcceleration / SimulationConstants.TicksPerSecond);
        var nextY = Approach(
            contact.Velocity.Y,
            targetY,
            Tuning.VerticalAcceleration / SimulationConstants.TicksPerSecond);
        nextX = Math.Clamp(nextX, -Tuning.MaximumHorizontalSpeed, Tuning.MaximumHorizontalSpeed);
        nextY = Math.Clamp(nextY, -Tuning.MaximumVerticalSpeed, Tuning.MaximumVerticalSpeed);
        return new ExternalVelocityEffect(nextX, nextY);
    }

    private void Disable(long tick)
    {
        if (State == WaterState.Disabled)
        {
            return;
        }
        foreach (var targetId in _occupants.Order(StringComparer.Ordinal))
        {
            AddEvent(tick, WaterEventKind.Exited, targetId);
        }
        _occupants.Clear();
        State = WaterState.Disabled;
        AddEvent(tick, WaterEventKind.Disabled, null);
    }

    private WaterSnapshot Capture(long tick) => new(
        tick,
        EntityId,
        Bounds,
        State,
        _occupants.Order(StringComparer.Ordinal),
        _motionEffects,
        _events);

    private void AddEvent(long tick, WaterEventKind kind, string? targetId) =>
        _events.Add(new WaterEvent(tick, WaterEventIds.For(kind), kind, EntityId, Bounds, targetId));

    private static decimal Approach(decimal value, decimal target, decimal maximumDelta)
    {
        if (value < target)
        {
            return Math.Min(value + maximumDelta, target);
        }
        return Math.Max(value - maximumDelta, target);
    }

    private static void ValidateTuning(WaterTuning tuning)
    {
        if (tuning.MaximumHorizontalSpeed <= 0m ||
            tuning.MaximumVerticalSpeed <= 0m ||
            tuning.NeutralBuoyancySpeed <= 0m ||
            tuning.NeutralBuoyancySpeed > tuning.MaximumVerticalSpeed ||
            tuning.HorizontalAcceleration <= 0m ||
            tuning.VerticalAcceleration <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }
}
