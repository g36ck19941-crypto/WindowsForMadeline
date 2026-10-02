using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Player;

public sealed class PlayerNormalController
{
    private readonly List<PlayerNormalEvent> _events = [];
    private bool _initialized;
    private bool _wasGrounded;
    private decimal _variableJumpSpeed;
    private decimal _wallSpeedRetained;
    private int _wallSpeedRetentionTicks;
    private bool _wallRetentionHandledThisTick;
    private string? _dropThroughPlatformId;
    private int _dropThroughTicksRemaining;
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

    internal void SetExternalKinematics(
        SimVector speed,
        int? facing = null,
        bool preserveVariableJump = false)
    {
        SpeedX = speed.X;
        SpeedY = speed.Y;
        if (facing is not null)
        {
            if (facing is not (-1 or 1))
            {
                throw new ArgumentOutOfRangeException(nameof(facing));
            }
            Facing = facing.Value;
        }
        if (!preserveVariableJump)
        {
            VariableJumpTicksRemaining = 0;
        }
    }

    internal void BeginExternalJump(decimal speedX, decimal speedY, int facing)
    {
        SetExternalKinematics(new SimVector(speedX, speedY), facing);
        CoyoteTicksRemaining = 0;
        JumpBufferTicksRemaining = 0;
        VariableJumpTicksRemaining = Tuning.VariableJumpTicks;
        _variableJumpSpeed = speedY;
        _wasGrounded = false;
    }

    public PlayerNormalSnapshot Step(
        PlayerInput input,
        SimulationWorld world,
        Action<SimulationWorld>? beforePlayer = null) =>
        Step(input, PlayerExternalEffects.None, world, beforePlayer);

    public PlayerNormalSnapshot Step(
        PlayerInput input,
        PlayerExternalEffects effects,
        SimulationWorld world,
        Action<SimulationWorld>? beforePlayer = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        PlayerNormalSnapshot? result = null;
        world.Step(currentWorld =>
        {
            beforePlayer?.Invoke(currentWorld);
            result = Update(input, effects, currentWorld);
        });

        return result ?? throw new InvalidOperationException("Player step did not produce a snapshot.");
    }

    public PlayerNormalSnapshot Update(PlayerInput input, SimulationWorld world) =>
        Update(input, PlayerExternalEffects.None, world);

    public PlayerNormalSnapshot Update(PlayerInput input, PlayerExternalEffects effects, SimulationWorld world)
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
        _wallRetentionHandledThisTick = false;

        RefreshDropThroughState(world, world.Tick);
        TryStartDropThrough(input, world, world.Tick);
        var groundedAtStart = SpeedY >= 0m && world.IsGrounded(Actor, _dropThroughPlatformId);
        RecordGroundTransition(world.Tick, groundedAtStart);
        UpdateCoyote(groundedAtStart);
        UpdateJumpBuffer(input);
        UpdateHorizontal(input, groundedAtStart);
        UpdateWallSpeedRetention(input, world, world.Tick);
        var appliedMaximumFallSpeed = UpdateVertical(input, effects, groundedAtStart, world.Tick);
        var appliedLiftSpeed = TryJump(input, world.Tick);
        ApplyExternalVelocity(effects.Velocity, world.Tick);
        var upwardCornerCorrectionX = Move(input, world);
        AdvanceDropThroughState(world, world.Tick);

        var groundedAtEnd = SpeedY >= 0m && world.IsGrounded(Actor, _dropThroughPlatformId);
        var groundedOneWayPlatformId = groundedAtEnd
            ? world.FirstOneWayPlatformBelow(Actor, _dropThroughPlatformId)?.Id
            : null;
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
            appliedMaximumFallSpeed,
            appliedLiftSpeed,
            _wallSpeedRetained,
            _wallSpeedRetentionTicks,
            upwardCornerCorrectionX,
            groundedOneWayPlatformId,
            _dropThroughPlatformId,
            _dropThroughTicksRemaining,
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

    private decimal UpdateVertical(PlayerInput input, PlayerExternalEffects effects, bool grounded, long tick)
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

            if (effects.MaximumFallSpeed is { } externalMaximum && SpeedY > externalMaximum)
            {
                SpeedY = externalMaximum;
                _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.ExternalFallSpeedLimited, null));
            }
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

        return effects.MaximumFallSpeed is { } maximum
            ? Math.Min(MaxFall, maximum)
            : MaxFall;
    }

    private void UpdateWallSpeedRetention(PlayerInput input, SimulationWorld world, long tick)
    {
        if (_wallSpeedRetentionTicks <= 0)
        {
            return;
        }

        _wallRetentionHandledThisTick = true;
        var retainedDirection = Math.Sign(_wallSpeedRetained);
        if (input.MoveX != 0 && input.MoveX == -retainedDirection)
        {
            ClearWallSpeedRetention();
            _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.WallSpeedRetentionCancelled, null));
            return;
        }

        if (world.FirstSolidAt(Actor, retainedDirection, 0) is null)
        {
            SpeedX = _wallSpeedRetained;
            ClearWallSpeedRetention();
            _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.WallSpeedRestored, null));
            return;
        }

        _wallSpeedRetentionTicks--;
        if (_wallSpeedRetentionTicks == 0)
        {
            _wallSpeedRetained = 0m;
            _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.WallSpeedRetentionExpired, null));
        }
    }

    private void ClearWallSpeedRetention()
    {
        _wallSpeedRetained = 0m;
        _wallSpeedRetentionTicks = 0;
    }

    private void RefreshDropThroughState(SimulationWorld world, long tick)
    {
        if (_dropThroughPlatformId is null)
        {
            return;
        }

        var platform = world.FindOneWayPlatform(_dropThroughPlatformId);
        if (platform is null || Actor.Bounds.Top >= platform.Bounds.Bottom)
        {
            EndDropThrough(tick, PlayerNormalEventKind.OneWayDropThroughCompleted);
        }
    }

    private void TryStartDropThrough(PlayerInput input, SimulationWorld world, long tick)
    {
        if (!input.DropThroughPressed || _dropThroughPlatformId is not null ||
            world.FirstSolidAt(Actor, 0, 1) is not null)
        {
            return;
        }

        var platform = world.FirstOneWayPlatformBelow(Actor);
        if (platform is null)
        {
            return;
        }

        _dropThroughPlatformId = platform.Id;
        _dropThroughTicksRemaining = Tuning.OneWayDropThroughTicks;
        SpeedY = Math.Max(SpeedY, Tuning.OneWayDropThroughSpeed);
        VariableJumpTicksRemaining = 0;
        _events.Add(new PlayerNormalEvent(
            tick,
            PlayerNormalEventKind.OneWayDropThroughStarted,
            platform.Id));
    }

    private void AdvanceDropThroughState(SimulationWorld world, long tick)
    {
        if (_dropThroughPlatformId is null)
        {
            return;
        }

        var platform = world.FindOneWayPlatform(_dropThroughPlatformId);
        if (platform is null || Actor.Bounds.Top >= platform.Bounds.Bottom)
        {
            EndDropThrough(tick, PlayerNormalEventKind.OneWayDropThroughCompleted);
            return;
        }

        _dropThroughTicksRemaining--;
        if (_dropThroughTicksRemaining <= 0)
        {
            EndDropThrough(tick, PlayerNormalEventKind.OneWayDropThroughExpired);
        }
    }

    private void EndDropThrough(long tick, PlayerNormalEventKind eventKind)
    {
        var platformId = _dropThroughPlatformId;
        _dropThroughPlatformId = null;
        _dropThroughTicksRemaining = 0;
        _events.Add(new PlayerNormalEvent(tick, eventKind, platformId));
    }

    private SimVector TryJump(PlayerInput input, long tick)
    {
        if (JumpBufferTicksRemaining <= 0 || CoyoteTicksRemaining <= 0)
        {
            return SimVector.Zero;
        }

        JumpBufferTicksRemaining = 0;
        CoyoteTicksRemaining = 0;
        VariableJumpTicksRemaining = Tuning.VariableJumpTicks;
        CancelWallSpeedRetention(tick);
        SpeedX += Tuning.JumpHorizontalBoost * input.MoveX;
        SpeedY = Tuning.JumpSpeed;
        var lift = BoundedLiftSpeed();
        SpeedX += lift.X;
        SpeedY += lift.Y;
        _variableJumpSpeed = SpeedY;
        _wasGrounded = false;
        _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.Jumped, null));
        if (lift != SimVector.Zero)
        {
            _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.LiftVelocityApplied, null));
        }
        return lift;
    }

    private SimVector BoundedLiftSpeed() => new(
        Math.Clamp(
            Actor.LiftSpeed.X,
            -Tuning.MaximumHorizontalLiftSpeed,
            Tuning.MaximumHorizontalLiftSpeed),
        Math.Clamp(
            Actor.LiftSpeed.Y,
            -Tuning.MaximumUpwardLiftSpeed,
            0m));

    private void ApplyExternalVelocity(ExternalVelocityEffect? effect, long tick)
    {
        if (effect is not { } velocity)
        {
            return;
        }

        CancelWallSpeedRetention(tick);
        var next = velocity.Apply(new SimVector(SpeedX, SpeedY));
        SpeedX = next.X;
        SpeedY = next.Y;
        VariableJumpTicksRemaining = 0;
        _wasGrounded = false;
        _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.ExternalVelocityApplied, null));
    }

    private void CancelWallSpeedRetention(long tick)
    {
        if (_wallSpeedRetentionTicks <= 0)
        {
            return;
        }

        ClearWallSpeedRetention();
        _events.Add(new PlayerNormalEvent(tick, PlayerNormalEventKind.WallSpeedRetentionCancelled, null));
    }

    private int Move(PlayerInput input, SimulationWorld world)
    {
        var incomingSpeedX = SpeedX;
        var horizontal = Actor.MoveX(SpeedX / SimulationConstants.TicksPerSecond, world);
        if (horizontal.Blocked)
        {
            if (_wallSpeedRetentionTicks == 0 && !_wallRetentionHandledThisTick && incomingSpeedX != 0m)
            {
                _wallSpeedRetained = incomingSpeedX;
                _wallSpeedRetentionTicks = Tuning.WallSpeedRetentionTicks;
                _events.Add(new PlayerNormalEvent(
                    world.Tick,
                    PlayerNormalEventKind.WallSpeedRetained,
                    horizontal.BlockingSolidId));
            }
            SpeedX = 0m;
            _events.Add(new PlayerNormalEvent(
                world.Tick,
                PlayerNormalEventKind.HorizontalBlocked,
                horizontal.BlockingSolidId));
        }

        var vertical = Actor.MoveYWithOneWayPlatforms(
            SpeedY / SimulationConstants.TicksPerSecond,
            world,
            _dropThroughPlatformId);
        if (vertical.Blocked)
        {
            var correctionX = TryUpwardCornerCorrection(input, vertical, world);
            if (correctionX != 0)
            {
                _events.Add(new PlayerNormalEvent(
                    world.Tick,
                    PlayerNormalEventKind.UpwardCornerCorrected,
                    vertical.BlockingSolidId));
                return correctionX;
            }

            SpeedY = 0m;
            VariableJumpTicksRemaining = 0;
            _events.Add(new PlayerNormalEvent(
                world.Tick,
                vertical.BlockingOneWayPlatformId is null
                    ? PlayerNormalEventKind.VerticalBlocked
                    : PlayerNormalEventKind.OneWayPlatformLanded,
                vertical.BlockingSurfaceId));
        }
        else if (SpeedY > 0m &&
                 world.FirstOneWayPlatformBelow(Actor, _dropThroughPlatformId) is { } landedPlatform)
        {
            SpeedY = 0m;
            VariableJumpTicksRemaining = 0;
            _events.Add(new PlayerNormalEvent(
                world.Tick,
                PlayerNormalEventKind.OneWayPlatformLanded,
                landedPlatform.Id));
        }

        return 0;
    }

    private int TryUpwardCornerCorrection(
        PlayerInput input,
        ActorMoveResult vertical,
        SimulationWorld world)
    {
        if (vertical.RequestedPixels >= 0 || SpeedY >= 0m)
        {
            return 0;
        }

        var remainingY = vertical.RequestedPixels - vertical.MovedPixels;
        if (remainingY >= 0)
        {
            return 0;
        }

        var preferredDirection = Math.Sign(SpeedX);
        if (preferredDirection == 0)
        {
            preferredDirection = input.MoveX != 0 ? input.MoveX : Facing;
        }

        for (var distance = 1; distance <= Tuning.UpwardCornerCorrectionPixels; distance++)
        {
            foreach (var direction in new[] { preferredDirection, -preferredDirection })
            {
                var offsetX = checked(direction * distance);
                if (!CanCorrectAroundCorner(world, offsetX, remainingY))
                {
                    continue;
                }

                var horizontal = Actor.MoveXExact(offsetX, world);
                var retry = Actor.MoveY(remainingY, world);
                if (horizontal.Blocked || retry.Blocked)
                {
                    throw new InvalidOperationException("Validated upward corner correction was unexpectedly blocked.");
                }
                return offsetX;
            }
        }

        return 0;
    }

    private bool CanCorrectAroundCorner(SimulationWorld world, int offsetX, int remainingY)
    {
        var horizontalDirection = Math.Sign(offsetX);
        for (var x = horizontalDirection; x != offsetX + horizontalDirection; x += horizontalDirection)
        {
            if (world.FirstSolidAt(Actor, x, 0) is not null)
            {
                return false;
            }
        }

        for (var y = -1; y >= remainingY; y--)
        {
            if (world.FirstSolidAt(Actor, offsetX, y) is not null)
            {
                return false;
            }
        }
        return true;
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
            tuning.MaximumHorizontalLiftSpeed <= 0m || tuning.MaximumUpwardLiftSpeed <= 0m ||
            tuning.WallSpeedRetentionTicks <= 0 || tuning.UpwardCornerCorrectionPixels <= 0 ||
            tuning.OneWayDropThroughSpeed <= 0m || tuning.OneWayDropThroughTicks <= 0 ||
            tuning.CoyoteTicks <= 0 || tuning.JumpBufferTicks <= 0 ||
            tuning.VariableJumpTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }
}
