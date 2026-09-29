using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Refill;

public sealed class RefillController
{
    private readonly List<RefillEvent> _events = [];
    private long _lastUpdatedTick = -1;
    private bool _armed = true;

    public RefillController(string entityId, SimPoint position, RefillTuning? tuning = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }

        EntityId = entityId;
        Position = position;
        Tuning = tuning ?? RefillTuning.PartialBaseline;
        if (Tuning.RespawnTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }

    public string EntityId { get; }
    public SimPoint Position { get; }
    public RefillTuning Tuning { get; }
    public RefillState State { get; private set; }
    public int RespawnTicksRemaining { get; private set; }

    public RefillSnapshot Step(RefillInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        RefillSnapshot? result = null;
        world.Step(current => result = Update(input, current));
        return result ?? throw new InvalidOperationException("Refill step did not produce a snapshot.");
    }

    public RefillSnapshot Update(RefillInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Refill update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Refill can only update once per simulation tick.");
        }

        _lastUpdatedTick = world.Tick;
        _events.Clear();

        if (input.DisableRequested)
        {
            Disable(world.Tick);
            return Capture(world.Tick, null);
        }
        if (State == RefillState.Disabled)
        {
            State = RefillState.Available;
            _armed = input.Contact is null;
            AddEvent(world.Tick, RefillEventKind.Enabled, input.Contact);
            return Capture(world.Tick, null);
        }

        AdvanceLifecycle(world.Tick);
        if (input.Contact is null)
        {
            _armed = true;
            return Capture(world.Tick, null);
        }

        if (State != RefillState.Available || !_armed || !input.Contact.CanActivate || !input.Contact.NeedsResources)
        {
            AddEvent(world.Tick, RefillEventKind.ContactIgnored, input.Contact);
            return Capture(world.Tick, null);
        }

        _armed = false;
        State = RefillState.Cooldown;
        RespawnTicksRemaining = Tuning.RespawnTicks;
        AddEvent(world.Tick, RefillEventKind.Collected, input.Contact);
        AddEvent(world.Tick, RefillEventKind.CooldownStarted, input.Contact);
        var effect = new RefillRestoreEffect(
            input.Contact.TargetId,
            new ExternalResourceEffect(input.Contact.MaximumDashes, input.Contact.MaximumStamina));
        AddEvent(world.Tick, RefillEventKind.RestoreIssued, input.Contact);
        return Capture(world.Tick, effect);
    }

    private void AdvanceLifecycle(long tick)
    {
        if (State != RefillState.Cooldown)
        {
            return;
        }

        RespawnTicksRemaining--;
        if (RespawnTicksRemaining <= 0)
        {
            RespawnTicksRemaining = 0;
            State = RefillState.Available;
            AddEvent(tick, RefillEventKind.Respawned, null);
        }
    }

    private void Disable(long tick)
    {
        if (State == RefillState.Disabled)
        {
            return;
        }

        State = RefillState.Disabled;
        RespawnTicksRemaining = 0;
        _armed = false;
        AddEvent(tick, RefillEventKind.Disabled, null);
    }

    private RefillSnapshot Capture(long tick, RefillRestoreEffect? effect) => new(
        tick,
        EntityId,
        Position,
        State,
        RespawnTicksRemaining,
        _armed,
        effect,
        _events);

    private void AddEvent(long tick, RefillEventKind kind, RefillContact? contact) =>
        _events.Add(new RefillEvent(
            tick,
            RefillEventIds.For(kind),
            kind,
            EntityId,
            Position,
            contact?.TargetId));
}
