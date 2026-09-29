using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Glider;

public sealed class GliderController
{
    private readonly List<GliderEvent> _events = [];
    private bool _initialized;
    private bool _wasGrounded;
    private bool _isOpen;
    private long _lastUpdatedTick = -1;

    public GliderController(Actor actor, GliderTuning? tuning = null, SimVector initialSpeed = default)
    {
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Tuning = tuning ?? GliderTuning.PartialBaseline;
        ValidateTuning(Tuning);
        SpeedX = initialSpeed.X;
        SpeedY = initialSpeed.Y;
    }

    public Actor Actor { get; }
    public GliderTuning Tuning { get; }
    public GliderState State { get; private set; }
    public decimal SpeedX { get; private set; }
    public decimal SpeedY { get; private set; }
    public string? HolderId { get; private set; }

    public GliderSnapshot Step(GliderInput input, SimulationWorld world, Action<SimulationWorld>? beforeGlider = null)
        => Step(input, null, world, beforeGlider);

    public GliderSnapshot Step(
        GliderInput input,
        ExternalVelocityEffect? externalVelocity,
        SimulationWorld world,
        Action<SimulationWorld>? beforeGlider = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        GliderSnapshot? result = null;
        world.Step(currentWorld =>
        {
            beforeGlider?.Invoke(currentWorld);
            result = Update(input, externalVelocity, currentWorld);
        });
        return result ?? throw new InvalidOperationException("Glider step did not produce a snapshot.");
    }

    public GliderSnapshot Update(GliderInput input, SimulationWorld world) =>
        Update(input, null, world);

    public GliderSnapshot Update(
        GliderInput input,
        ExternalVelocityEffect? externalVelocity,
        SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Glider update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Glider can only update once per simulation tick.");
        }
        _lastUpdatedTick = world.Tick;
        _events.Clear();

        if (State is GliderState.Destroyed or GliderState.Squished)
        {
            return Capture(world, null);
        }
        if (Actor.IsSquished)
        {
            EnterTerminal(GliderState.Squished, GliderEventKind.Squished, world.Tick);
            return Capture(world, null);
        }
        if (input.DestroyRequested)
        {
            EnterTerminal(GliderState.Destroyed, GliderEventKind.Destroyed, world.Tick);
            return Capture(world, null);
        }

        GliderHolderEffect? holderEffect = null;
        switch (State)
        {
            case GliderState.Free:
                UpdateFree(input, externalVelocity, world);
                break;
            case GliderState.Held:
                if (externalVelocity is not null)
                {
                    throw new InvalidOperationException("Held Glider cannot accept external velocity.");
                }
                holderEffect = UpdateHeld(input, world);
                break;
            default:
                throw new InvalidOperationException("Unknown Glider state.");
        }

        if (Actor.IsSquished)
        {
            EnterTerminal(GliderState.Squished, GliderEventKind.Squished, world.Tick);
            holderEffect = null;
        }
        return Capture(world, holderEffect);
    }

    private void UpdateFree(
        GliderInput input,
        ExternalVelocityEffect? externalVelocity,
        SimulationWorld world)
    {
        if (input.Action == GliderAction.Pickup)
        {
            PickUp(input.Holder!, world);
            return;
        }
        if (input.Action is GliderAction.Drop or GliderAction.Throw)
        {
            throw new InvalidOperationException("A free Glider cannot be dropped or thrown.");
        }
        if (input.Holder is not null)
        {
            throw new InvalidOperationException("A free Glider accepts a holder snapshot only for pickup.");
        }

        var grounded = SpeedY >= 0m && world.IsGrounded(Actor);
        if (_initialized && grounded && !_wasGrounded)
        {
            AddEvent(world.Tick, GliderEventKind.Landed, null, world.FirstSolidAt(Actor, 0, 1)?.Id);
        }
        if (Actor.LiftSpeed != SimVector.Zero)
        {
            AddEvent(world.Tick, GliderEventKind.LiftCarried, null, null);
            if (!grounded)
            {
                SpeedX += Actor.LiftSpeed.X;
                SpeedY += Actor.LiftSpeed.Y;
                AddEvent(world.Tick, GliderEventKind.LiftInherited, null, null);
            }
        }

        SpeedX = Approach(SpeedX, 0m, Tuning.HorizontalFriction / SimulationConstants.TicksPerSecond);
        SpeedY = grounded ? 0m : Approach(SpeedY, Tuning.MaximumFallSpeed, Tuning.Gravity / SimulationConstants.TicksPerSecond);
        if (externalVelocity is { } velocity)
        {
            var next = velocity.Apply(new SimVector(SpeedX, SpeedY));
            SpeedX = next.X;
            SpeedY = next.Y;
            AddEvent(world.Tick, GliderEventKind.ExternalVelocityApplied, null, null);
        }
        MoveFree(world);

        var groundedAtEnd = SpeedY >= 0m && world.IsGrounded(Actor);
        if (groundedAtEnd && !grounded && _events.All(item => item.Kind is not GliderEventKind.Landed and not GliderEventKind.Bounced))
        {
            SpeedY = 0m;
            AddEvent(world.Tick, GliderEventKind.Landed, null, world.FirstSolidAt(Actor, 0, 1)?.Id);
        }
        SetOpen(!groundedAtEnd && SpeedY > Tuning.OpenFallSpeedThreshold, world.Tick);
        _wasGrounded = groundedAtEnd;
        _initialized = true;
    }

    private GliderHolderEffect? UpdateHeld(GliderInput input, SimulationWorld world)
    {
        if (input.Action == GliderAction.Pickup)
        {
            throw new InvalidOperationException("Glider is already held.");
        }
        var holder = input.Holder ?? throw new InvalidOperationException("Held Glider requires a holder snapshot every tick.");
        if (!string.Equals(holder.HolderId, HolderId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Held Glider received a different holder ID.");
        }

        MoveToHolder(holder, world);
        var limitRequired = holder.HolderVerticalSpeed > Tuning.MaximumHolderFallSpeed;
        if (limitRequired)
        {
            AddEvent(world.Tick, GliderEventKind.HolderFallLimited, holder.HolderId, null);
        }
        var effect = new GliderHolderEffect(holder.HolderId, Tuning.MaximumHolderFallSpeed, limitRequired);
        if (input.Action == GliderAction.Drop)
        {
            Release(holder, thrown: false, world.Tick);
            return null;
        }
        if (input.Action == GliderAction.Throw)
        {
            Release(holder, thrown: true, world.Tick);
            return null;
        }
        return effect;
    }

    private void PickUp(GliderHolderSnapshot holder, SimulationWorld world)
    {
        State = GliderState.Held;
        HolderId = holder.HolderId;
        SpeedX = 0m;
        SpeedY = 0m;
        _initialized = true;
        _wasGrounded = false;
        AddEvent(world.Tick, GliderEventKind.PickedUp, holder.HolderId, null);
        SetOpen(true, world.Tick);
        MoveToHolder(holder, world);
    }

    private void MoveToHolder(GliderHolderSnapshot holder, SimulationWorld world)
    {
        var startX = Actor.X;
        var startY = Actor.Y;
        var horizontal = Actor.MoveX(checked(holder.HoldPosition.X - Actor.X) - Actor.XSubpixel, world);
        var vertical = Actor.MoveY(checked(holder.HoldPosition.Y - Actor.Y) - Actor.YSubpixel, world);
        SpeedX = 0m;
        SpeedY = 0m;
        if (horizontal.Blocked || vertical.Blocked)
        {
            AddEvent(world.Tick, GliderEventKind.HoldBlocked, holder.HolderId, horizontal.BlockingSolidId ?? vertical.BlockingSolidId);
        }
        if (Actor.X != startX || Actor.Y != startY)
        {
            AddEvent(world.Tick, GliderEventKind.Carried, holder.HolderId, null);
        }
    }

    private void Release(GliderHolderSnapshot holder, bool thrown, long tick)
    {
        State = GliderState.Free;
        HolderId = null;
        _wasGrounded = false;
        SpeedX = holder.LiftSpeed.X;
        SpeedY = holder.LiftSpeed.Y;
        if (holder.LiftSpeed != SimVector.Zero)
        {
            AddEvent(tick, GliderEventKind.LiftInherited, holder.HolderId, null);
        }
        if (thrown)
        {
            SpeedX += Tuning.ThrowSpeedX * holder.Facing;
            SpeedY += Tuning.ThrowSpeedY;
            AddEvent(tick, GliderEventKind.Thrown, holder.HolderId, null);
            SetOpen(false, tick);
        }
        else
        {
            AddEvent(tick, GliderEventKind.Dropped, holder.HolderId, null);
        }
    }

    private void MoveFree(SimulationWorld world)
    {
        var incomingX = SpeedX;
        var horizontal = Actor.MoveX(incomingX / SimulationConstants.TicksPerSecond, world);
        if (horizontal.Blocked)
        {
            SpeedX = Math.Abs(incomingX) >= Tuning.MinimumHorizontalBounceSpeed ? -incomingX * Tuning.HorizontalBounceFactor : 0m;
            AddEvent(world.Tick, GliderEventKind.HorizontalBounced, null, horizontal.BlockingSolidId);
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
                AddEvent(world.Tick, GliderEventKind.Bounced, null, vertical.BlockingSolidId);
            }
            else
            {
                SpeedY = 0m;
                AddEvent(world.Tick, GliderEventKind.Landed, null, vertical.BlockingSolidId);
            }
        }
        else
        {
            SpeedY = 0m;
            AddEvent(world.Tick, GliderEventKind.VerticalBlocked, null, vertical.BlockingSolidId);
        }
    }

    private void SetOpen(bool value, long tick)
    {
        if (_isOpen == value)
        {
            return;
        }
        _isOpen = value;
        AddEvent(tick, value ? GliderEventKind.Opened : GliderEventKind.Closed, HolderId, null);
    }

    private void EnterTerminal(GliderState state, GliderEventKind kind, long tick)
    {
        if (State == state)
        {
            return;
        }
        State = state;
        HolderId = null;
        SpeedX = 0m;
        SpeedY = 0m;
        SetOpen(false, tick);
        AddEvent(tick, kind, null, null);
    }

    private GliderSnapshot Capture(SimulationWorld world, GliderHolderEffect? holderEffect) => new(
        world.Tick,
        State,
        new SimPoint(Actor.X, Actor.Y),
        new SimVector(SpeedX, SpeedY),
        State == GliderState.Free && !Actor.IsSquished && world.IsGrounded(Actor),
        _isOpen,
        HolderId,
        holderEffect,
        Actor.LiftSpeed,
        _events);

    private void AddEvent(long tick, GliderEventKind kind, string? holderId, string? solidId) =>
        _events.Add(new GliderEvent(tick, GliderEventIds.For(kind), kind, Actor.Id,
            new SimPoint(Actor.X, Actor.Y), new SimVector(SpeedX, SpeedY), holderId, solidId));

    private static decimal Approach(decimal value, decimal target, decimal maximumDelta) =>
        value < target ? Math.Min(value + maximumDelta, target) : Math.Max(value - maximumDelta, target);

    private static void ValidateTuning(GliderTuning tuning)
    {
        if (tuning.Gravity <= 0m || tuning.MaximumFallSpeed <= 0m || tuning.HorizontalFriction < 0m ||
            tuning.ThrowSpeedX <= 0m || tuning.ThrowSpeedY >= 0m ||
            tuning.HorizontalBounceFactor is < 0m or > 1m || tuning.LandingBounceFactor is < 0m or > 1m ||
            tuning.MinimumHorizontalBounceSpeed < 0m || tuning.MinimumLandingBounceSpeed < 0m ||
            tuning.MaximumHolderFallSpeed <= 0m || tuning.OpenFallSpeedThreshold < 0m ||
            tuning.OpenFallSpeedThreshold >= tuning.MaximumFallSpeed)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }
}
