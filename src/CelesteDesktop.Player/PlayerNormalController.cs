using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Player;

public sealed class PlayerNormalController
{
    private readonly List<PlayerNormalEvent> _events = [];
    private bool _initialized;
    private bool _wasGrounded;
    private decimal _variableJumpSpeed;
    private long _lastUpdatedTick = -1;

    public PlayerNormalController(
        Actor actor,
        NormalJumpTuning? tuning = null,
        SimVector initialSpeed = default)
    {
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Tuning = tuning ?? NormalJumpTuning.ReferencePartial;
        ValidateTuning(Tuning);
        SpeedX = initialSpeed.X;
        SpeedY = initialSpeed.Y;
        MaxFall = Tuning.NormalMaxFall;
    }

    public Actor Actor { get; }
    public NormalJumpTuning Tuning { get; }
    public decimal SpeedX { get; private set; }
    public decimal SpeedY { get; private set; }
    public decimal MaxFall { get; private set; }
    public int Facing { get; private set; } = 1;
    public int CoyoteTicksRemaining { get; private set; }
    public int JumpBufferTicksRemaining { get; private set; }
    public int VariableJumpTicksRemaining { get; private set; }

    public PlayerNormalSnapshot Step(
        PlayerInput input,
        SimulationWorld world,
        Action<SimulationWorld>? beforePlayer = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        PlayerNormalSnapshot? result = null;
        world.Step(currentWorld =>
        {
            beforePlayer?.Invoke(currentWorld);
            result = Update(input, currentWorld);
        });

        return result ?? throw new InvalidOperationException("Player step did not produce a snapshot.");
    }

    public PlayerNormalSnapshot Update(PlayerInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Player update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Player can only update once per simulation tick.");
        }
        _lastUpdatedTick = world.Tick;
        _events.Clear();

        var groundedAtStart = SpeedY >= 0m && world.IsGrounded(Actor);
        RecordGroundTransition(world.Tick, groundedAtStart);
        UpdateCoyote(groundedAtStart);
        UpdateJumpBuffer(input);
        UpdateHorizontal(input, groundedAtStart);
        UpdateVertical(input, groundedAtStart);
        TryJump(input, world.Tick);
        Move(world);

        var groundedAtEnd = SpeedY >= 0m && world.IsGrounded(Actor);
        if (_initialized && groundedAtEnd && !_wasGrounded)
        {
            _events.Add(new PlayerNormalEvent(world.Tick, PlayerNormalEventKind.Landed, null));
        }
        _wasGrounded = groundedAtEnd;
        _initialized = true;
        TickTimers(groundedAtStart);

        return new PlayerNormalSnapshot(
            world.Tick,
            new SimPoint(Actor.X, Actor.Y),
            new SimVector(SpeedX, SpeedY),
            Facing,
            groundedAtEnd,
            MaxFall,
            CoyoteTicksRemaining,
            JumpBufferTicksRemaining,
            VariableJumpTicksRemaining,
            _events);
    }

    private void RecordGroundTransition(long tick, bool grounded)
    {
        if (_initialized && _wasGrounded && !grounded)
        {
            _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.LeftGround, null));
        }
    }

    private void UpdateCoyote(bool grounded)
    {
        if (grounded)
        {
            CoyoteTicksRemaining = Tuning.CoyoteTicks;
        }
    }

    private void UpdateJumpBuffer(PlayerInput input)
    {
        if (input.JumpPressed)
        {
            JumpBufferTicksRemaining = Tuning.JumpBufferTicks;
        }
    }

    private void UpdateHorizontal(PlayerInput input, bool grounded)
    {
        if (input.MoveX != 0)
        {
            Facing = input.MoveX;
        }

        var multiplier = grounded ? 1m : Tuning.AirControlMultiplier;
        var target = Tuning.MaxRun * input.MoveX;
        var rate = Math.Abs(SpeedX) > Tuning.MaxRun && Math.Sign(SpeedX) == input.MoveX
            ? Tuning.OverspeedReduction
            : Tuning.RunAcceleration;
        SpeedX = Approach(SpeedX, target, (rate * multiplier) / SimulationConstants.TicksPerSecond);
    }

    private void UpdateVertical(PlayerInput input, bool grounded)
    {
        if (input.MoveY == 1 && SpeedY >= Tuning.NormalMaxFall)
        {
            MaxFall = Approach(
                MaxFall,
                Tuning.FastMaxFall,
                Tuning.FastFallAcceleration / SimulationConstants.TicksPerSecond);
        }
        else
        {
            MaxFall = Approach(
                MaxFall,
                Tuning.NormalMaxFall,
                Tuning.FastFallAcceleration / SimulationConstants.TicksPerSecond);
        }

        if (!grounded)
        {
            var multiplier = Math.Abs(SpeedY) < Tuning.HalfGravityThreshold && input.JumpHeld
                ? 0.5m
                : 1m;
            SpeedY = Approach(
                SpeedY,
                MaxFall,
                (Tuning.Gravity * multiplier) / SimulationConstants.TicksPerSecond);
        }

        if (VariableJumpTicksRemaining > 0)
        {
            if (input.JumpHeld)
            {
                SpeedY = Math.Min(SpeedY, _variableJumpSpeed);
            }
            else
            {
                VariableJumpTicksRemaining = 0;
            }
        }
    }

    private void TryJump(PlayerInput input, long tick)
    {
        if (JumpBufferTicksRemaining <= 0 || CoyoteTicksRemaining <= 0)
        {
            return;
        }

        JumpBufferTicksRemaining = 0;
        CoyoteTicksRemaining = 0;
        VariableJumpTicksRemaining = Tuning.VariableJumpTicks;
        SpeedX += Tuning.JumpHorizontalBoost * input.MoveX;
        SpeedY = Tuning.JumpSpeed;
        _variableJumpSpeed = SpeedY;
        _wasGrounded = false;
        _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.Jumped, null));
    }

    private void Move(SimulationWorld world)
    {
        var horizontal = Actor.MoveX(SpeedX / SimulationConstants.TicksPerSecond, world);
        if (horizontal.Blocked)
        {
            SpeedX = 0m;
            _events.Add(new PlayerNormalEvent(
                world.Tick,
                PlayerNormalEventKind.HorizontalBlocked,
                horizontal.BlockingSolidId));
        }

        var vertical = Actor.MoveY(SpeedY / SimulationConstants.TicksPerSecond, world);
        if (vertical.Blocked)
        {
            SpeedY = 0m;
            VariableJumpTicksRemaining = 0;
            _events.Add(new PlayerNormalEvent(
                world.Tick,
                PlayerNormalEventKind.VerticalBlocked,
                vertical.BlockingSolidId));
        }
    }

    private void TickTimers(bool groundedAtStart)
    {
        if (!groundedAtStart && CoyoteTicksRemaining > 0)
        {
            CoyoteTicksRemaining--;
        }
        if (JumpBufferTicksRemaining > 0)
        {
            JumpBufferTicksRemaining--;
        }
        if (VariableJumpTicksRemaining > 0)
        {
            VariableJumpTicksRemaining--;
        }
    }

    private static decimal Approach(decimal value, decimal target, decimal maximumDelta)
    {
        if (maximumDelta < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDelta));
        }
        if (value < target)
        {
            return Math.Min(value + maximumDelta, target);
        }
        return Math.Max(value - maximumDelta, target);
    }

    private static void ValidateTuning(NormalJumpTuning tuning)
    {
        if (tuning.MaxRun <= 0m || tuning.RunAcceleration <= 0m ||
            tuning.OverspeedReduction <= 0m || tuning.AirControlMultiplier <= 0m ||
            tuning.Gravity <= 0m || tuning.HalfGravityThreshold <= 0m ||
            tuning.NormalMaxFall <= 0m || tuning.FastMaxFall < tuning.NormalMaxFall ||
            tuning.FastFallAcceleration <= 0m || tuning.JumpSpeed >= 0m ||
            tuning.CoyoteTicks <= 0 || tuning.JumpBufferTicks <= 0 ||
            tuning.VariableJumpTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }
}
