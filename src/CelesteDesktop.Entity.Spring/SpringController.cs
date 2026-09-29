using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Spring;

public sealed class SpringController
{
    private readonly List<SpringEvent> _events = [];
    private long _lastUpdatedTick = -1;
    private bool _armed = true;

    public SpringController(
        string entityId,
        SimPoint position,
        SpringOrientation orientation,
        SpringTuning? tuning = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }

        EntityId = entityId;
        Position = position;
        Orientation = orientation;
        Tuning = tuning ?? SpringTuning.PartialBaseline;
        if (Tuning.LaunchSpeed <= 0m || Tuning.RetractedTicks <= 0 || Tuning.CooldownTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }

    public string EntityId { get; }
    public SimPoint Position { get; }
    public SpringOrientation Orientation { get; }
    public SpringTuning Tuning { get; }
    public SpringState State { get; private set; }
    public int RetractedTicksRemaining { get; private set; }
    public int CooldownTicksRemaining { get; private set; }

    public SpringSnapshot Step(SpringInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        SpringSnapshot? result = null;
        world.Step(current => result = Update(input, current));
        return result ?? throw new InvalidOperationException("Spring step did not produce a snapshot.");
    }

    public SpringSnapshot Update(SpringInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Spring update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Spring can only update once per simulation tick.");
        }

        _lastUpdatedTick = world.Tick;
        _events.Clear();

        if (input.DisableRequested)
        {
            Disable(world.Tick);
            return Capture(world.Tick, null);
        }

        if (State == SpringState.Disabled)
        {
            State = SpringState.Ready;
            _armed = input.Contact is null;
            AddEvent(world.Tick, SpringEventKind.Enabled, input.Contact);
            return Capture(world.Tick, null);
        }

        AdvanceLifecycle(world.Tick);
        if (input.Contact is null)
        {
            _armed = true;
            return Capture(world.Tick, null);
        }

        if (State != SpringState.Ready || !_armed || !input.Contact.CanActivate)
        {
            AddEvent(world.Tick, SpringEventKind.ContactIgnored, input.Contact);
            return Capture(world.Tick, null);
        }

        _armed = false;
        State = SpringState.Retracted;
        RetractedTicksRemaining = Tuning.RetractedTicks;
        AddEvent(world.Tick, SpringEventKind.Activated, input.Contact);
        AddEvent(world.Tick, SpringEventKind.Retracted, input.Contact);
        var effect = new SpringLaunchEffect(
            input.Contact.TargetId,
            input.Contact.TargetKind,
            CreateVelocityEffect());
        AddEvent(world.Tick, SpringEventKind.LaunchIssued, input.Contact);
        return Capture(world.Tick, effect);
    }

    private void AdvanceLifecycle(long tick)
    {
        if (State == SpringState.Retracted)
        {
            RetractedTicksRemaining--;
            if (RetractedTicksRemaining <= 0)
            {
                RetractedTicksRemaining = 0;
                CooldownTicksRemaining = Tuning.CooldownTicks;
                State = SpringState.Cooldown;
                AddEvent(tick, SpringEventKind.CooldownStarted, null);
            }
        }
        else if (State == SpringState.Cooldown)
        {
            CooldownTicksRemaining--;
            if (CooldownTicksRemaining <= 0)
            {
                CooldownTicksRemaining = 0;
                State = SpringState.Ready;
                AddEvent(tick, SpringEventKind.Ready, null);
            }
        }
    }

    private void Disable(long tick)
    {
        if (State == SpringState.Disabled)
        {
            return;
        }

        State = SpringState.Disabled;
        RetractedTicksRemaining = 0;
        CooldownTicksRemaining = 0;
        _armed = false;
        AddEvent(tick, SpringEventKind.Disabled, null);
    }

    private ExternalVelocityEffect CreateVelocityEffect() => Orientation switch
    {
        SpringOrientation.Up => new ExternalVelocityEffect(null, -Tuning.LaunchSpeed),
        SpringOrientation.Right => new ExternalVelocityEffect(Tuning.LaunchSpeed, null),
        SpringOrientation.Down => new ExternalVelocityEffect(null, Tuning.LaunchSpeed),
        SpringOrientation.Left => new ExternalVelocityEffect(-Tuning.LaunchSpeed, null),
        _ => throw new InvalidOperationException("Unknown Spring orientation.")
    };

    private SpringSnapshot Capture(long tick, SpringLaunchEffect? effect) => new(
        tick,
        EntityId,
        Position,
        Orientation,
        State,
        RetractedTicksRemaining,
        CooldownTicksRemaining,
        _armed,
        effect,
        _events);

    private void AddEvent(long tick, SpringEventKind kind, SpringContact? contact) =>
        _events.Add(new SpringEvent(
            tick,
            SpringEventIds.For(kind),
            kind,
            EntityId,
            Position,
            contact?.TargetId,
            contact?.TargetKind));
}
