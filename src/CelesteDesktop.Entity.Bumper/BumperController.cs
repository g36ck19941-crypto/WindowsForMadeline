using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Bumper;

public sealed class BumperController
{
    private readonly List<BumperEvent> _events = [];
    private long _lastUpdatedTick = -1;
    private bool _armed = true;

    public BumperController(string entityId, SimPoint center, BumperTuning? tuning = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }

        EntityId = entityId;
        Center = center;
        Tuning = tuning ?? BumperTuning.PartialBaseline;
        if (Tuning.ContactRadius <= 0 || Tuning.LaunchSpeed <= 0m || Tuning.CooldownTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }

    public string EntityId { get; }
    public SimPoint Center { get; }
    public BumperTuning Tuning { get; }
    public BumperState State { get; private set; }
    public int CooldownTicksRemaining { get; private set; }

    public BumperSnapshot Step(BumperInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        BumperSnapshot? result = null;
        world.Step(current => result = Update(input, current));
        return result ?? throw new InvalidOperationException("Bumper step did not produce a snapshot.");
    }

    public BumperSnapshot Update(BumperInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Bumper update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Bumper can only update once per simulation tick.");
        }

        _lastUpdatedTick = world.Tick;
        _events.Clear();

        if (input.DisableRequested)
        {
            Disable(world.Tick);
            return Capture(world.Tick, null);
        }

        if (State == BumperState.Disabled)
        {
            State = BumperState.Ready;
            _armed = input.Contact is null;
            AddEvent(world.Tick, BumperEventKind.Enabled, input.Contact);
            return Capture(world.Tick, null);
        }

        AdvanceLifecycle(world.Tick);
        if (input.Contact is null)
        {
            _armed = true;
            return Capture(world.Tick, null);
        }

        if (State != BumperState.Ready || !_armed || !input.Contact.CanActivate || !IsInRange(input.Contact))
        {
            AddEvent(world.Tick, BumperEventKind.ContactIgnored, input.Contact);
            return Capture(world.Tick, null);
        }

        _armed = false;
        State = BumperState.Cooldown;
        CooldownTicksRemaining = Tuning.CooldownTicks;
        AddEvent(world.Tick, BumperEventKind.Activated, input.Contact);
        var direction = CalculateDirection(input.Contact, world.Tick);
        var effect = new BumperLaunchEffect(
            input.Contact.TargetId,
            direction,
            new ExternalVelocityEffect(direction.X * Tuning.LaunchSpeed, direction.Y * Tuning.LaunchSpeed));
        AddEvent(world.Tick, BumperEventKind.LaunchIssued, input.Contact);
        AddEvent(world.Tick, BumperEventKind.CooldownStarted, input.Contact);
        return Capture(world.Tick, effect);
    }

    private bool IsInRange(BumperContact contact)
    {
        var deltaX = (decimal)contact.TargetCenter.X - Center.X;
        var deltaY = (decimal)contact.TargetCenter.Y - Center.Y;
        var radius = (decimal)Tuning.ContactRadius;
        return (deltaX * deltaX) + (deltaY * deltaY) <= radius * radius;
    }

    private SimVector CalculateDirection(BumperContact contact, long tick)
    {
        var deltaX = (decimal)contact.TargetCenter.X - Center.X;
        var deltaY = (decimal)contact.TargetCenter.Y - Center.Y;
        if (deltaX == 0m && deltaY == 0m)
        {
            AddEvent(tick, BumperEventKind.CenterFallbackUsed, contact);
            return new SimVector(0m, -1m);
        }

        var magnitude = SquareRoot((deltaX * deltaX) + (deltaY * deltaY));
        return new SimVector(deltaX / magnitude, deltaY / magnitude);
    }

    private void AdvanceLifecycle(long tick)
    {
        if (State != BumperState.Cooldown)
        {
            return;
        }

        CooldownTicksRemaining--;
        if (CooldownTicksRemaining <= 0)
        {
            CooldownTicksRemaining = 0;
            State = BumperState.Ready;
            AddEvent(tick, BumperEventKind.Ready, null);
        }
    }

    private void Disable(long tick)
    {
        if (State == BumperState.Disabled)
        {
            return;
        }

        State = BumperState.Disabled;
        CooldownTicksRemaining = 0;
        _armed = false;
        AddEvent(tick, BumperEventKind.Disabled, null);
    }

    private BumperSnapshot Capture(long tick, BumperLaunchEffect? effect) => new(
        tick,
        EntityId,
        Center,
        State,
        CooldownTicksRemaining,
        _armed,
        effect,
        _events);

    private void AddEvent(long tick, BumperEventKind kind, BumperContact? contact) =>
        _events.Add(new BumperEvent(
            tick,
            BumperEventIds.For(kind),
            kind,
            EntityId,
            Center,
            contact?.TargetId));

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
