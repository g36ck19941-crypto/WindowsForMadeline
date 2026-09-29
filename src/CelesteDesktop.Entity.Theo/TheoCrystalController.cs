using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Theo;

public sealed class TheoCrystalController
{
    private readonly List<TheoCrystalEvent> _events = [];
    private bool _initialized;
    private bool _wasGrounded;
    private long _lastUpdatedTick = -1;

    public TheoCrystalController(
        Actor actor,
        TheoCrystalTuning? tuning = null,
        SimVector initialSpeed = default)
    {
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Tuning = tuning ?? TheoCrystalTuning.PartialBaseline;
        ValidateTuning(Tuning);
        SpeedX = initialSpeed.X;
        SpeedY = initialSpeed.Y;
    }

    public Actor Actor { get; }
    public TheoCrystalTuning Tuning { get; }
    public TheoCrystalState State { get; private set; }
    public decimal SpeedX { get; private set; }
    public decimal SpeedY { get; private set; }
    public string? HolderId { get; private set; }

    public TheoCrystalSnapshot Step(
        TheoCrystalInput input,
        SimulationWorld world,
        Action<SimulationWorld>? beforeTheo = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        TheoCrystalSnapshot? result = null;
        world.Step(currentWorld =>
        {
            beforeTheo?.Invoke(currentWorld);
            result = Update(input, currentWorld);
        });
        return result ?? throw new InvalidOperationException("Theo step did not produce a snapshot.");
    }

    public TheoCrystalSnapshot Update(TheoCrystalInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Theo update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Theo can only update once per simulation tick.");
        }
        _lastUpdatedTick = world.Tick;
        _events.Clear();

        if (Actor.IsSquished)
        {
            EnterSquished(world.Tick);
            return Capture(world);
        }

        switch (State)
        {
            case TheoCrystalState.Free:
                UpdateFree(input, world);
                break;
            case TheoCrystalState.Held:
                UpdateHeld(input, world);
                break;
            case TheoCrystalState.Squished:
                break;
            default:
                throw new InvalidOperationException("Unknown Theo state.");
        }

        if (Actor.IsSquished)
        {
            EnterSquished(world.Tick);
        }
        return Capture(world);
    }

    private void UpdateFree(TheoCrystalInput input, SimulationWorld world)
    {
        if (input.Action == TheoCrystalAction.Pickup)
        {
            PickUp(input.Holder!, world);
            return;
        }
        if (input.Action is TheoCrystalAction.Drop or TheoCrystalAction.Throw)
        {
            throw new InvalidOperationException("A free Theo crystal cannot be dropped or thrown.");
        }
        if (input.Holder is not null)
        {
            throw new InvalidOperationException("A free Theo crystal accepts a holder snapshot only for pickup.");
        }

        var grounded = SpeedY >= 0m && world.IsGrounded(Actor);
        if (_initialized && grounded && !_wasGrounded)
        {
            AddEvent(world.Tick, TheoCrystalEventKind.Landed, null, world.FirstSolidAt(Actor, 0, 1)?.Id);
        }
        if (Actor.LiftSpeed != SimVector.Zero)
        {
            AddEvent(world.Tick, TheoCrystalEventKind.LiftCarried, null, null);
            if (!grounded)
            {
                SpeedX += Actor.LiftSpeed.X;
                SpeedY += Actor.LiftSpeed.Y;
                AddEvent(world.Tick, TheoCrystalEventKind.LiftInherited, null, null);
            }
        }

        SpeedX = Approach(
            SpeedX,
            0m,
            Tuning.HorizontalFriction / SimulationConstants.TicksPerSecond);
        SpeedY = grounded
            ? 0m
            : Approach(
                SpeedY,
                Tuning.MaximumFallSpeed,
                Tuning.Gravity / SimulationConstants.TicksPerSecond);

        MoveFree(world);
        var groundedAtEnd = SpeedY >= 0m && world.IsGrounded(Actor);
        if (groundedAtEnd && !grounded && _events.All(item => item.Kind is not TheoCrystalEventKind.Landed and not TheoCrystalEventKind.Bounced))
        {
            SpeedY = 0m;
            AddEvent(world.Tick, TheoCrystalEventKind.Landed, null, world.FirstSolidAt(Actor, 0, 1)?.Id);
        }
        _wasGrounded = groundedAtEnd;
        _initialized = true;
    }

    private void UpdateHeld(TheoCrystalInput input, SimulationWorld world)
    {
        if (input.Action == TheoCrystalAction.Pickup)
        {
            throw new InvalidOperationException("Theo is already held.");
        }
        var holder = input.Holder ?? throw new InvalidOperationException("Held Theo requires a holder snapshot every tick.");
        if (!string.Equals(holder.HolderId, HolderId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Held Theo received a different holder ID.");
        }

        MoveToHolder(holder, world);
        if (input.Action == TheoCrystalAction.Drop)
        {
            Release(holder, thrown: false, world.Tick);
        }
        else if (input.Action == TheoCrystalAction.Throw)
        {
            Release(holder, thrown: true, world.Tick);
        }
    }

    private void PickUp(TheoHolderSnapshot holder, SimulationWorld world)
    {
        State = TheoCrystalState.Held;
        _wasGrounded = false;
        _initialized = true;
        HolderId = holder.HolderId;
        SpeedX = 0m;
        SpeedY = 0m;
        AddEvent(world.Tick, TheoCrystalEventKind.PickedUp, holder.HolderId, null);
        MoveToHolder(holder, world);
    }

    private void MoveToHolder(TheoHolderSnapshot holder, SimulationWorld world)
    {
        var startX = Actor.X;
        var startY = Actor.Y;
        var horizontal = Actor.MoveX(
            checked(holder.HoldPosition.X - Actor.X) - Actor.XSubpixel,
            world);
        var vertical = Actor.MoveY(
            checked(holder.HoldPosition.Y - Actor.Y) - Actor.YSubpixel,
            world);

        SpeedX = 0m;
        SpeedY = 0m;
        if (horizontal.Blocked || vertical.Blocked)
        {
            AddEvent(
                world.Tick,
                TheoCrystalEventKind.HoldBlocked,
                holder.HolderId,
                horizontal.BlockingSolidId ?? vertical.BlockingSolidId);
        }
        if (Actor.X != startX || Actor.Y != startY)
        {
            AddEvent(world.Tick, TheoCrystalEventKind.Carried, holder.HolderId, null);
        }
    }

    private void Release(TheoHolderSnapshot holder, bool thrown, long tick)
    {
        State = TheoCrystalState.Free;
        _wasGrounded = false;
        HolderId = null;
        SpeedX = holder.LiftSpeed.X;
        SpeedY = holder.LiftSpeed.Y;
        if (holder.LiftSpeed != SimVector.Zero)
        {
            AddEvent(tick, TheoCrystalEventKind.LiftInherited, holder.HolderId, null);
        }

        if (thrown)
        {
            SpeedX += Tuning.ThrowSpeedX * holder.Facing;
            SpeedY += Tuning.ThrowSpeedY;
            AddEvent(tick, TheoCrystalEventKind.Thrown, holder.HolderId, null);
        }
        else
        {
            AddEvent(tick, TheoCrystalEventKind.Dropped, holder.HolderId, null);
        }
    }

    private void MoveFree(SimulationWorld world)
    {
        var incomingX = SpeedX;
        var horizontal = Actor.MoveX(incomingX / SimulationConstants.TicksPerSecond, world);
        if (horizontal.Blocked)
        {
            SpeedX = Math.Abs(incomingX) >= Tuning.MinimumHorizontalBounceSpeed
                ? -incomingX * Tuning.HorizontalBounceFactor
                : 0m;
            AddEvent(world.Tick, TheoCrystalEventKind.HorizontalBounced, null, horizontal.BlockingSolidId);
        }

        var incomingY = SpeedY;
        var vertical = Actor.MoveY(incomingY / SimulationConstants.TicksPerSecond, world);
        if (!vertical.Blocked)
        {
            return;
        }

        if (incomingY > 0m)
        {
            if (incomingY >= Tuning.MinimumLandingBounceSpeed)
            {
                SpeedY = -incomingY * Tuning.LandingBounceFactor;
                AddEvent(world.Tick, TheoCrystalEventKind.Bounced, null, vertical.BlockingSolidId);
            }
            else
            {
                SpeedY = 0m;
                AddEvent(world.Tick, TheoCrystalEventKind.Landed, null, vertical.BlockingSolidId);
            }
        }
        else
        {
            SpeedY = 0m;
            AddEvent(world.Tick, TheoCrystalEventKind.VerticalBlocked, null, vertical.BlockingSolidId);
        }
    }

    private void EnterSquished(long tick)
    {
        if (State == TheoCrystalState.Squished)
        {
            return;
        }
        State = TheoCrystalState.Squished;
        HolderId = null;
        SpeedX = 0m;
        SpeedY = 0m;
        AddEvent(tick, TheoCrystalEventKind.Squished, null, null);
    }

    private TheoCrystalSnapshot Capture(SimulationWorld world) => new(
        world.Tick,
        State,
        new SimPoint(Actor.X, Actor.Y),
        new SimVector(SpeedX, SpeedY),
        State == TheoCrystalState.Free && !Actor.IsSquished && world.IsGrounded(Actor),
        HolderId,
        Actor.LiftSpeed,
        _events);

    private void AddEvent(long tick, TheoCrystalEventKind kind, string? holderId, string? solidId) =>
        _events.Add(new TheoCrystalEvent(
            tick,
            TheoCrystalEventIds.For(kind),
            kind,
            Actor.Id,
            new SimPoint(Actor.X, Actor.Y),
            new SimVector(SpeedX, SpeedY),
            holderId,
            solidId));

    private static decimal Approach(decimal value, decimal target, decimal maximumDelta)
    {
        if (value < target)
        {
            return Math.Min(value + maximumDelta, target);
        }
        return Math.Max(value - maximumDelta, target);
    }

    private static void ValidateTuning(TheoCrystalTuning tuning)
    {
        if (tuning.Gravity <= 0m || tuning.MaximumFallSpeed <= 0m ||
            tuning.HorizontalFriction < 0m || tuning.ThrowSpeedX <= 0m ||
            tuning.ThrowSpeedY >= 0m || tuning.HorizontalBounceFactor is < 0m or > 1m ||
            tuning.LandingBounceFactor is < 0m or > 1m ||
            tuning.MinimumHorizontalBounceSpeed < 0m || tuning.MinimumLandingBounceSpeed < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }
}
