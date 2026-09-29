using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Player;

public sealed class PlayerTraversalController
{
    private const decimal DiagonalComponent = 0.7071067811865475m;

    private readonly PlayerNormalController _normal;
    private readonly List<PlayerTraversalEvent> _events = [];
    private SimVector _lastAim = new(1m, 0m);
    private int _wallDirection;
    private long _lastUpdatedTick = -1;

    public PlayerTraversalController(
        Actor actor,
        NormalJumpTuning? normalTuning = null,
        PlayerTraversalTuning? traversalTuning = null,
        PlayerAssistSettings? assists = null,
        SimVector initialSpeed = default,
        int maxDashes = 1)
    {
        if (maxDashes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDashes));
        }

        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Tuning = traversalTuning ?? PlayerTraversalTuning.ReferencePartial;
        ValidateTuning(Tuning);
        Assists = assists ?? new PlayerAssistSettings();
        MaxDashes = maxDashes;
        Dashes = maxDashes;
        Stamina = Tuning.ClimbMaxStamina;
        WallSlideTicksRemaining = Tuning.WallSlideTicks;
        _normal = new PlayerNormalController(actor, normalTuning, initialSpeed);
    }

    public Actor Actor { get; }
    public PlayerTraversalTuning Tuning { get; }
    public PlayerAssistSettings Assists { get; }
    public PlayerTraversalState State { get; private set; }
    public int MaxDashes { get; }
    public int Dashes { get; private set; }
    public decimal Stamina { get; private set; }
    public decimal SpeedX => _normal.SpeedX;
    public decimal SpeedY => _normal.SpeedY;
    public int Facing => _normal.Facing;
    public SimVector DashDirection { get; private set; }
    public int DashTicksRemaining { get; private set; }
    public int DashCooldownTicksRemaining { get; private set; }
    public int DashRefillCooldownTicksRemaining { get; private set; }
    public int DashAttackTicksRemaining { get; private set; }
    public int WallSlideTicksRemaining { get; private set; }
    public int ClimbNoMoveTicksRemaining { get; private set; }

    public PlayerTraversalSnapshot Step(PlayerInput input, SimulationWorld world) =>
        Step(input, PlayerExternalEffects.None, world);

    public PlayerTraversalSnapshot Step(PlayerInput input, PlayerExternalEffects effects, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        PlayerTraversalSnapshot? result = null;
        world.Step(currentWorld => result = Update(input, effects, currentWorld));
        return result ?? throw new InvalidOperationException("Player traversal step did not produce a snapshot.");
    }

    public PlayerTraversalSnapshot Update(PlayerInput input, SimulationWorld world) =>
        Update(input, PlayerExternalEffects.None, world);

    public PlayerTraversalSnapshot Update(PlayerInput input, PlayerExternalEffects effects, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Player traversal update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Player traversal can only update once per simulation tick.");
        }

        _lastUpdatedTick = world.Tick;
        _events.Clear();
        TickGlobalTimers();
        UpdateAim(input);
        ApplyAssists();

        if (State != PlayerTraversalState.Climb &&
            SpeedY >= 0m &&
            world.IsGrounded(Actor))
        {
            RefillOnGround(world.Tick);
        }

        switch (State)
        {
            case PlayerTraversalState.Normal:
                UpdateNormal(input, effects, world);
                break;
            case PlayerTraversalState.Dash:
                UpdateDash(input, world);
                break;
            case PlayerTraversalState.WallSlide:
                UpdateWallSlide(input, world);
                break;
            case PlayerTraversalState.Climb:
                UpdateClimb(input, world);
                break;
            default:
                throw new InvalidOperationException($"Unsupported traversal state: {State}.");
        }

        ApplyAssists();
        return Capture(world);
    }

    public static SimVector QuantizeDashDirection(int moveX, int moveY, int fallbackFacing)
    {
        if (moveX is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moveX));
        }
        if (moveY is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moveY));
        }
        if (fallbackFacing is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(fallbackFacing));
        }

        if (moveX == 0 && moveY == 0)
        {
            return new SimVector(fallbackFacing, 0m);
        }
        if (moveX != 0 && moveY != 0)
        {
            return new SimVector(moveX * DiagonalComponent, moveY * DiagonalComponent);
        }
        return new SimVector(moveX, moveY);
    }

    private void UpdateNormal(PlayerInput input, PlayerExternalEffects effects, SimulationWorld world)
    {
        if (CanStartDash(input))
        {
            StartDash(world.Tick);
            UpdateDash(input, world);
            return;
        }

        var wallDirection = FindWallDirection(world, Tuning.WallJumpCheckDistance, input.MoveX);
        if (input.JumpPressed && wallDirection != 0 && !world.IsGrounded(Actor))
        {
            StartWallJump(-wallDirection, world.Tick);
            MoveExternalKinematics(world);
            return;
        }

        var climbDirection = FindWallDirection(world, Tuning.ClimbCheckDistance, input.MoveX);
        if (input.GrabHeld && climbDirection != 0 && Stamina > 0m && !world.IsGrounded(Actor))
        {
            StartClimb(climbDirection, world.Tick);
            UpdateClimb(input, world);
            return;
        }

        var normal = _normal.Update(input, effects, world);
        MapNormalEvents(normal);

        var slideDirection = FindWallDirection(world, 1, input.MoveX);
        var wantsSlide = input.MoveY != 1 &&
            normal.Speed.Y >= 0m &&
            !normal.Grounded &&
            slideDirection != 0 &&
            (input.MoveX == slideDirection || (input.MoveX == 0 && input.GrabHeld));
        if (wantsSlide)
        {
            State = PlayerTraversalState.WallSlide;
            _wallDirection = slideDirection;
            WallSlideTicksRemaining = Tuning.WallSlideTicks;
            _events.Add(new PlayerTraversalEvent(world.Tick, PlayerTraversalEventKind.WallSlideStarted,
                world.FirstSolidAt(Actor, slideDirection, 0)?.Id));
        }
    }

    private void UpdateDash(PlayerInput input, SimulationWorld world)
    {
        var horizontal = Actor.MoveX(SpeedX / SimulationConstants.TicksPerSecond, world);
        var vertical = Actor.MoveY(SpeedY / SimulationConstants.TicksPerSecond, world);
        var nextX = horizontal.Blocked ? 0m : SpeedX;
        var nextY = vertical.Blocked ? 0m : SpeedY;
        if (horizontal.Blocked)
        {
            _events.Add(new PlayerTraversalEvent(world.Tick, PlayerTraversalEventKind.DashBlocked, horizontal.BlockingSolidId));
        }
        if (vertical.Blocked)
        {
            _events.Add(new PlayerTraversalEvent(world.Tick, PlayerTraversalEventKind.DashBlocked, vertical.BlockingSolidId));
        }
        _normal.SetExternalKinematics(new SimVector(nextX, nextY));

        DashTicksRemaining--;
        if (DashTicksRemaining > 0)
        {
            return;
        }

        var endSpeed = new SimVector(nextX, nextY);
        if (DashDirection.Y <= 0m)
        {
            endSpeed = new SimVector(
                DashDirection.X * Tuning.EndDashSpeed,
                DashDirection.Y * Tuning.EndDashSpeed);
        }
        if (endSpeed.Y < 0m)
        {
            endSpeed = endSpeed with { Y = endSpeed.Y * Tuning.EndDashUpMultiplier };
        }
        _normal.SetExternalKinematics(endSpeed);
        State = PlayerTraversalState.Normal;
        _events.Add(new PlayerTraversalEvent(world.Tick, PlayerTraversalEventKind.DashEnded));
    }

    private void UpdateWallSlide(PlayerInput input, SimulationWorld world)
    {
        if (CanStartDash(input))
        {
            EndWallSlide(world.Tick);
            StartDash(world.Tick);
            UpdateDash(input, world);
            return;
        }
        if (input.GrabHeld && Stamina > 0m && HasWall(world, _wallDirection, Tuning.ClimbCheckDistance))
        {
            EndWallSlide(world.Tick);
            StartClimb(_wallDirection, world.Tick);
            UpdateClimb(input, world);
            return;
        }
        if (input.JumpPressed && HasWall(world, _wallDirection, Tuning.WallJumpCheckDistance))
        {
            var away = -_wallDirection;
            EndWallSlide(world.Tick);
            StartWallJump(away, world.Tick);
            MoveExternalKinematics(world);
            return;
        }
        if (world.IsGrounded(Actor) || !HasWall(world, _wallDirection, 1) || input.MoveY == 1)
        {
            EndWallSlide(world.Tick);
            MapNormalEvents(_normal.Update(input, world));
            return;
        }

        var elapsed = Tuning.WallSlideTicks - WallSlideTicksRemaining;
        var ratio = Math.Clamp((decimal)elapsed / Tuning.WallSlideTicks, 0m, 1m);
        var maxFall = Tuning.WallSlideStartMax +
            ((NormalJumpTuning.ReferencePartial.NormalMaxFall - Tuning.WallSlideStartMax) * ratio);
        var nextY = Approach(SpeedY, maxFall,
            NormalJumpTuning.ReferencePartial.Gravity / SimulationConstants.TicksPerSecond);
        var vertical = Actor.MoveY(nextY / SimulationConstants.TicksPerSecond, world);
        _normal.SetExternalKinematics(new SimVector(0m, vertical.Blocked ? 0m : nextY), _wallDirection);
        WallSlideTicksRemaining--;
        if (vertical.Blocked || WallSlideTicksRemaining <= 0)
        {
            EndWallSlide(world.Tick);
        }
    }

    private void UpdateClimb(PlayerInput input, SimulationWorld world)
    {
        if (CanStartDash(input))
        {
            State = PlayerTraversalState.Normal;
            StartDash(world.Tick);
            UpdateDash(input, world);
            return;
        }
        if (!input.GrabHeld)
        {
            ReleaseClimb(world.Tick);
            MapNormalEvents(_normal.Update(input, world));
            return;
        }
        if (!HasWall(world, _wallDirection, Tuning.ClimbCheckDistance))
        {
            if (SpeedY < 0m)
            {
                StartClimbHop(world.Tick);
            }
            else
            {
                ReleaseClimb(world.Tick);
            }
            return;
        }
        if (input.JumpPressed)
        {
            if (input.MoveX == -_wallDirection)
            {
                StartWallJump(-_wallDirection, world.Tick);
            }
            else
            {
                if (!world.IsGrounded(Actor))
                {
                    Stamina = Math.Max(0m, Stamina - Tuning.ClimbJumpCost);
                }
                var speedX = SpeedX + (NormalJumpTuning.ReferencePartial.JumpHorizontalBoost * input.MoveX);
                _normal.BeginExternalJump(speedX, NormalJumpTuning.ReferencePartial.JumpSpeed, _wallDirection);
                State = PlayerTraversalState.Normal;
                _events.Add(new PlayerTraversalEvent(world.Tick, PlayerTraversalEventKind.ClimbJumped));
            }
            MoveExternalKinematics(world);
            return;
        }

        if (ClimbNoMoveTicksRemaining > 0)
        {
            ClimbNoMoveTicksRemaining--;
        }
        var target = ClimbNoMoveTicksRemaining > 0
            ? 0m
            : input.MoveY switch
            {
                -1 => Tuning.ClimbUpSpeed,
                1 => Tuning.ClimbDownSpeed,
                _ => 0m
            };
        var nextY = Approach(SpeedY, target, Tuning.ClimbAcceleration / SimulationConstants.TicksPerSecond);
        var vertical = Actor.MoveY(nextY / SimulationConstants.TicksPerSecond, world);
        if (vertical.Blocked)
        {
            nextY = 0m;
        }
        _normal.SetExternalKinematics(new SimVector(0m, nextY), _wallDirection);

        if (ClimbNoMoveTicksRemaining <= 0 && !Assists.InfiniteStamina)
        {
            if (input.MoveY == -1)
            {
                Stamina = Math.Max(0m, Stamina - (Tuning.ClimbUpCostPerSecond / SimulationConstants.TicksPerSecond));
            }
            else if (input.MoveY == 0 && !world.IsGrounded(Actor))
            {
                Stamina = Math.Max(0m, Stamina - (Tuning.ClimbStillCostPerSecond / SimulationConstants.TicksPerSecond));
            }
        }

        if (Stamina <= 0m)
        {
            State = PlayerTraversalState.Normal;
            _events.Add(new PlayerTraversalEvent(world.Tick, PlayerTraversalEventKind.StaminaDepleted));
            return;
        }

        if (nextY < 0m && !HasWall(world, _wallDirection, Tuning.ClimbCheckDistance))
        {
            StartClimbHop(world.Tick);
        }
    }

    private void StartDash(long tick)
    {
        var direction = _lastAim;
        if (!Assists.InfiniteDashes)
        {
            Dashes--;
        }
        DashDirection = direction;
        DashTicksRemaining = Tuning.DashTicks;
        DashCooldownTicksRemaining = Tuning.DashCooldownTicks;
        DashRefillCooldownTicksRemaining = Tuning.DashRefillCooldownTicks;
        DashAttackTicksRemaining = Tuning.DashAttackTicks;
        var dashX = direction.X * Tuning.DashSpeed;
        if (Math.Sign(SpeedX) == Math.Sign(dashX) && Math.Abs(SpeedX) > Math.Abs(dashX))
        {
            dashX = SpeedX;
        }
        _normal.SetExternalKinematics(new SimVector(dashX, direction.Y * Tuning.DashSpeed),
            direction.X == 0m ? Facing : Math.Sign(direction.X));
        State = PlayerTraversalState.Dash;
        _events.Add(new PlayerTraversalEvent(tick, PlayerTraversalEventKind.DashStarted));
    }

    private void StartWallJump(int awayDirection, long tick)
    {
        _normal.BeginExternalJump(
            Tuning.WallJumpHorizontalSpeed * awayDirection,
            Tuning.WallJumpVerticalSpeed,
            awayDirection);
        State = PlayerTraversalState.Normal;
        WallSlideTicksRemaining = Tuning.WallSlideTicks;
        _events.Add(new PlayerTraversalEvent(tick, PlayerTraversalEventKind.WallJumped));
    }

    private void StartClimb(int direction, long tick)
    {
        _wallDirection = direction;
        _normal.SetExternalKinematics(new SimVector(0m, SpeedY * Tuning.ClimbGrabYMultiplier), direction);
        ClimbNoMoveTicksRemaining = Tuning.ClimbNoMoveTicks;
        WallSlideTicksRemaining = Tuning.WallSlideTicks;
        State = PlayerTraversalState.Climb;
        _events.Add(new PlayerTraversalEvent(tick, PlayerTraversalEventKind.ClimbStarted));
    }

    private void StartClimbHop(long tick)
    {
        _normal.SetExternalKinematics(new SimVector(
            _wallDirection * Tuning.ClimbHopX,
            Math.Min(SpeedY, Tuning.ClimbHopY)), _wallDirection);
        State = PlayerTraversalState.Normal;
        _events.Add(new PlayerTraversalEvent(tick, PlayerTraversalEventKind.ClimbHop));
    }

    private void ReleaseClimb(long tick)
    {
        State = PlayerTraversalState.Normal;
        _events.Add(new PlayerTraversalEvent(tick, PlayerTraversalEventKind.ClimbReleased));
    }

    private void EndWallSlide(long tick)
    {
        if (State == PlayerTraversalState.WallSlide)
        {
            State = PlayerTraversalState.Normal;
            _events.Add(new PlayerTraversalEvent(tick, PlayerTraversalEventKind.WallSlideEnded));
        }
    }

    private void RefillOnGround(long tick)
    {
        Stamina = Tuning.ClimbMaxStamina;
        if (DashRefillCooldownTicksRemaining <= 0 && Dashes < MaxDashes)
        {
            Dashes = MaxDashes;
            _events.Add(new PlayerTraversalEvent(tick, PlayerTraversalEventKind.DashRefilled));
        }
    }

    private void ApplyAssists()
    {
        if (Assists.InfiniteDashes)
        {
            Dashes = MaxDashes;
        }
        if (Assists.InfiniteStamina)
        {
            Stamina = Tuning.ClimbMaxStamina;
        }
    }

    private bool CanStartDash(PlayerInput input) =>
        input.DashPressed && DashCooldownTicksRemaining <= 0 && Dashes > 0;

    private void UpdateAim(PlayerInput input)
    {
        if (input.MoveX != 0 || input.MoveY != 0)
        {
            _lastAim = QuantizeDashDirection(input.MoveX, input.MoveY, Facing);
        }
    }

    private int FindWallDirection(SimulationWorld world, int distance, int preferredDirection)
    {
        var first = preferredDirection != 0 ? preferredDirection : Facing;
        if (HasWall(world, first, distance))
        {
            return first;
        }
        return HasWall(world, -first, distance) ? -first : 0;
    }

    private bool HasWall(SimulationWorld world, int direction, int distance)
    {
        for (var offset = 1; offset <= distance; offset++)
        {
            if (world.FirstSolidAt(Actor, direction * offset, 0) is not null)
            {
                return true;
            }
        }
        return false;
    }

    private void MapNormalEvents(PlayerNormalSnapshot snapshot)
    {
        foreach (var normalEvent in snapshot.Events)
        {
            if (normalEvent.Kind == PlayerNormalEventKind.Jumped)
            {
                _events.Add(new PlayerTraversalEvent(snapshot.Tick, PlayerTraversalEventKind.Jumped));
            }
            else if (normalEvent.Kind == PlayerNormalEventKind.Landed)
            {
                _events.Add(new PlayerTraversalEvent(snapshot.Tick, PlayerTraversalEventKind.Landed));
            }
        }
    }

    private void TickGlobalTimers()
    {
        DashCooldownTicksRemaining = Math.Max(0, DashCooldownTicksRemaining - 1);
        DashRefillCooldownTicksRemaining = Math.Max(0, DashRefillCooldownTicksRemaining - 1);
        DashAttackTicksRemaining = Math.Max(0, DashAttackTicksRemaining - 1);
    }

    private void MoveExternalKinematics(SimulationWorld world)
    {
        var horizontal = Actor.MoveX(SpeedX / SimulationConstants.TicksPerSecond, world);
        var vertical = Actor.MoveY(SpeedY / SimulationConstants.TicksPerSecond, world);
        _normal.SetExternalKinematics(new SimVector(
            horizontal.Blocked ? 0m : SpeedX,
            vertical.Blocked ? 0m : SpeedY), Facing, preserveVariableJump: true);
    }

    private PlayerTraversalSnapshot Capture(SimulationWorld world) => new(
        world.Tick,
        new SimPoint(Actor.X, Actor.Y),
        new SimVector(SpeedX, SpeedY),
        Facing,
        SpeedY >= 0m && world.IsGrounded(Actor),
        State,
        Dashes,
        Stamina,
        DashDirection,
        DashTicksRemaining,
        DashCooldownTicksRemaining,
        DashAttackTicksRemaining,
        WallSlideTicksRemaining,
        ClimbNoMoveTicksRemaining,
        Tuning.ClimbTiredThreshold,
        _events);

    private static decimal Approach(decimal value, decimal target, decimal maximumDelta)
    {
        if (value < target)
        {
            return Math.Min(value + maximumDelta, target);
        }
        return Math.Max(value - maximumDelta, target);
    }

    private static void ValidateTuning(PlayerTraversalTuning tuning)
    {
        if (tuning.DashSpeed <= 0m || tuning.EndDashSpeed <= 0m ||
            tuning.EndDashUpMultiplier <= 0m || tuning.DashTicks <= 0 ||
            tuning.DashCooldownTicks <= 0 || tuning.DashRefillCooldownTicks <= 0 ||
            tuning.DashAttackTicks <= 0 || tuning.WallJumpCheckDistance <= 0 ||
            tuning.WallJumpHorizontalSpeed <= 0m || tuning.WallJumpVerticalSpeed >= 0m ||
            tuning.WallSlideStartMax <= 0m || tuning.WallSlideTicks <= 0 ||
            tuning.ClimbMaxStamina <= 0m || tuning.ClimbTiredThreshold <= 0m ||
            tuning.ClimbTiredThreshold >= tuning.ClimbMaxStamina ||
            tuning.ClimbUpCostPerSecond <= 0m || tuning.ClimbStillCostPerSecond <= 0m ||
            tuning.ClimbJumpCost <= 0m || tuning.ClimbCheckDistance <= 0 ||
            tuning.ClimbNoMoveTicks <= 0 || tuning.ClimbUpSpeed >= 0m ||
            tuning.ClimbDownSpeed <= 0m || tuning.ClimbSlipSpeed <= 0m ||
            tuning.ClimbAcceleration <= 0m || tuning.ClimbGrabYMultiplier is < 0m or > 1m ||
            tuning.ClimbHopX <= 0m || tuning.ClimbHopY >= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
    }
}
