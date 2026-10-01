using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.Entity.Seeker;

public sealed class SeekerController
{
    private readonly List<SeekerEvent> _events = [];
    private readonly decimal _leftBound;
    private readonly decimal _rightBound;
    private readonly int _initialDirection;
    private long _lastUpdatedTick = -1;
    private string? _lockedTargetId;
    private SimVector _lockedTargetCenter;
    private SimVector _dashDirection;

    public SeekerController(
        string entityId,
        SimVector spawnCenter,
        decimal leftBound,
        decimal rightBound,
        int initialDirection = 1,
        SeekerTuning? tuning = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }
        if (leftBound > spawnCenter.X || rightBound < spawnCenter.X || leftBound >= rightBound)
        {
            throw new ArgumentOutOfRangeException(nameof(leftBound), "Patrol bounds must contain spawn and have positive width.");
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
        PatrolDirection = initialDirection;
        Tuning = tuning ?? SeekerTuning.PartialBaseline;
        ValidateTuning(Tuning);
    }

    public string EntityId { get; }
    public SimVector SpawnCenter { get; }
    public SimVector Center { get; private set; }
    public SeekerState State { get; private set; }
    public int PatrolDirection { get; private set; }
    public int StateTicksRemaining { get; private set; }
    public int LostSightTicks { get; private set; }
    public SeekerTuning Tuning { get; }

    public SeekerSnapshot Step(SeekerInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        SeekerSnapshot? result = null;
        world.Step(current => result = Update(input, current));
        return result ?? throw new InvalidOperationException("Seeker step did not produce a snapshot.");
    }

    public SeekerSnapshot Update(SeekerInput input, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.IsAdvancing)
        {
            throw new InvalidOperationException("Seeker update is only valid inside a fixed simulation step.");
        }
        if (_lastUpdatedTick == world.Tick)
        {
            throw new InvalidOperationException("Seeker can only update once per simulation tick.");
        }

        _lastUpdatedTick = world.Tick;
        _events.Clear();

        if (input.DisableRequested)
        {
            Disable(world.Tick);
            return Capture(world.Tick, null);
        }
        if (State == SeekerState.Disabled)
        {
            ResetAtSpawn();
            AddEvent(world.Tick, SeekerEventKind.Enabled, input.Target?.TargetId);
            return Capture(world.Tick, null);
        }

        return State switch
        {
            SeekerState.Patrolling => UpdatePatrol(input.Target, world.Tick),
            SeekerState.Alerted => UpdateAlert(input.Target, world.Tick),
            SeekerState.Chasing => UpdateChase(input.Target, world.Tick),
            SeekerState.Windup => UpdateWindup(input.Target, world.Tick),
            SeekerState.Dashing => UpdateDash(input, world.Tick),
            SeekerState.Stunned => UpdateStunned(world.Tick),
            _ => throw new InvalidOperationException($"Unsupported Seeker state {State}.")
        };
    }

    private SeekerSnapshot UpdatePatrol(SeekerTarget? target, long tick)
    {
        AdvancePatrol(tick);
        if (target is null)
        {
            return Capture(tick, null);
        }
        if (!target.CanBeDetected || !IsWithin(target.Center, Tuning.DetectionRadius))
        {
            AddEvent(tick, SeekerEventKind.TargetIgnored, target.TargetId);
            return Capture(tick, null);
        }

        _lockedTargetId = target.TargetId;
        _lockedTargetCenter = target.Center;
        State = SeekerState.Alerted;
        StateTicksRemaining = Tuning.AlertTicks;
        LostSightTicks = 0;
        AddEvent(tick, SeekerEventKind.Alerted, target.TargetId);
        return Capture(tick, null);
    }

    private SeekerSnapshot UpdateAlert(SeekerTarget? target, long tick)
    {
        UpdateLockedTarget(target);
        StateTicksRemaining--;
        if (StateTicksRemaining > 0)
        {
            return Capture(tick, null);
        }

        StateTicksRemaining = 0;
        State = SeekerState.Chasing;
        AddEvent(tick, SeekerEventKind.ChaseStarted, _lockedTargetId);
        return Capture(tick, null);
    }

    private SeekerSnapshot UpdateChase(SeekerTarget? target, long tick)
    {
        if (!UpdateLockedTarget(target))
        {
            LostSightTicks++;
            if (LostSightTicks >= Tuning.ForgetTicks)
            {
                AddEvent(tick, SeekerEventKind.TargetLost, _lockedTargetId);
                ReturnToPatrol();
            }
            return Capture(tick, null);
        }

        LostSightTicks = 0;
        if (IsWithin(_lockedTargetCenter, Tuning.DashTriggerRadius))
        {
            State = SeekerState.Windup;
            StateTicksRemaining = Tuning.WindupTicks;
            AddEvent(tick, SeekerEventKind.WindupStarted, _lockedTargetId);
            return Capture(tick, null);
        }

        var direction = DirectionTo(_lockedTargetCenter, tick);
        Center = AddScaled(Center, direction, Tuning.ChaseSpeed / 60m);
        AddEvent(tick, SeekerEventKind.Chased, _lockedTargetId);
        return Capture(tick, null);
    }

    private SeekerSnapshot UpdateWindup(SeekerTarget? target, long tick)
    {
        UpdateLockedTarget(target);
        StateTicksRemaining--;
        if (StateTicksRemaining > 0)
        {
            return Capture(tick, null);
        }

        StateTicksRemaining = Tuning.DashTicks;
        _dashDirection = DirectionTo(_lockedTargetCenter, tick);
        State = SeekerState.Dashing;
        AddEvent(tick, SeekerEventKind.DashStarted, _lockedTargetId);
        return Capture(tick, null);
    }

    private SeekerSnapshot UpdateDash(SeekerInput input, long tick)
    {
        if (input.WallCollision)
        {
            AddEvent(tick, SeekerEventKind.WallHit, _lockedTargetId);
            EnterStunned(tick);
            return Capture(tick, null);
        }

        Center = AddScaled(Center, _dashDirection, Tuning.DashSpeed / 60m);
        AddEvent(tick, SeekerEventKind.Dashed, _lockedTargetId);

        if (input.Target is not null &&
            string.Equals(input.Target.TargetId, _lockedTargetId, StringComparison.Ordinal) &&
            input.Target.IsTouching)
        {
            var effect = new SeekerHitEffect(input.Target.TargetId, _dashDirection);
            AddEvent(tick, SeekerEventKind.TargetHit, input.Target.TargetId);
            EnterStunned(tick);
            return Capture(tick, effect);
        }

        StateTicksRemaining--;
        if (StateTicksRemaining <= 0)
        {
            EnterStunned(tick);
        }
        return Capture(tick, null);
    }

    private SeekerSnapshot UpdateStunned(long tick)
    {
        StateTicksRemaining--;
        if (StateTicksRemaining <= 0)
        {
            ResetAtSpawn();
            AddEvent(tick, SeekerEventKind.Recovered, null);
        }
        return Capture(tick, null);
    }

    private void AdvancePatrol(long tick)
    {
        var nextX = Center.X + (PatrolDirection * Tuning.PatrolSpeed / 60m);
        var turned = false;
        if (nextX >= _rightBound)
        {
            nextX = _rightBound;
            PatrolDirection = -1;
            turned = true;
        }
        else if (nextX <= _leftBound)
        {
            nextX = _leftBound;
            PatrolDirection = 1;
            turned = true;
        }
        Center = new SimVector(nextX, SpawnCenter.Y);
        AddEvent(tick, SeekerEventKind.Patrolled, null);
        if (turned)
        {
            AddEvent(tick, SeekerEventKind.Turned, null);
        }
    }

    private bool UpdateLockedTarget(SeekerTarget? target)
    {
        if (target is null || !target.CanBeDetected ||
            !string.Equals(target.TargetId, _lockedTargetId, StringComparison.Ordinal))
        {
            return false;
        }
        _lockedTargetCenter = target.Center;
        return true;
    }

    private void EnterStunned(long tick)
    {
        State = SeekerState.Stunned;
        StateTicksRemaining = Tuning.StunTicks;
        AddEvent(tick, SeekerEventKind.Stunned, _lockedTargetId);
    }

    private void ReturnToPatrol()
    {
        State = SeekerState.Patrolling;
        StateTicksRemaining = 0;
        LostSightTicks = 0;
        _lockedTargetId = null;
        _lockedTargetCenter = default;
        _dashDirection = default;
    }

    private void Disable(long tick)
    {
        if (State == SeekerState.Disabled)
        {
            return;
        }
        State = SeekerState.Disabled;
        StateTicksRemaining = 0;
        LostSightTicks = 0;
        _lockedTargetId = null;
        _lockedTargetCenter = default;
        _dashDirection = default;
        AddEvent(tick, SeekerEventKind.Disabled, null);
    }

    private void ResetAtSpawn()
    {
        Center = SpawnCenter;
        PatrolDirection = _initialDirection;
        State = SeekerState.Patrolling;
        StateTicksRemaining = 0;
        LostSightTicks = 0;
        _lockedTargetId = null;
        _lockedTargetCenter = default;
        _dashDirection = default;
    }

    private bool IsWithin(SimVector target, int radius)
    {
        var dx = target.X - Center.X;
        var dy = target.Y - Center.Y;
        var r = (decimal)radius;
        return (dx * dx) + (dy * dy) <= r * r;
    }

    private SimVector DirectionTo(SimVector target, long tick)
    {
        var dx = target.X - Center.X;
        var dy = target.Y - Center.Y;
        if (dx == 0m && dy == 0m)
        {
            AddEvent(tick, SeekerEventKind.CenterFallbackUsed, _lockedTargetId);
            return new SimVector(PatrolDirection, 0m);
        }
        var magnitude = SquareRoot((dx * dx) + (dy * dy));
        return new SimVector(dx / magnitude, dy / magnitude);
    }

    private SeekerSnapshot Capture(long tick, SeekerHitEffect? hitEffect) => new(
        tick,
        EntityId,
        SpawnCenter,
        Center,
        State,
        PatrolDirection,
        StateTicksRemaining,
        LostSightTicks,
        _lockedTargetId,
        _dashDirection,
        hitEffect,
        _events);

    private void AddEvent(long tick, SeekerEventKind kind, string? targetId) =>
        _events.Add(new SeekerEvent(tick, SeekerEventIds.For(kind), kind, EntityId, Center, targetId));

    private static SimVector AddScaled(SimVector value, SimVector direction, decimal distance) =>
        new(value.X + (direction.X * distance), value.Y + (direction.Y * distance));

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

    private static void ValidateTuning(SeekerTuning tuning)
    {
        if (tuning.PatrolSpeed <= 0m || tuning.ChaseSpeed <= 0m || tuning.DashSpeed <= 0m ||
            tuning.DetectionRadius <= 0 || tuning.DashTriggerRadius <= 0 ||
            tuning.AlertTicks <= 0 || tuning.WindupTicks <= 0 || tuning.DashTicks <= 0 ||
            tuning.StunTicks <= 0 || tuning.ForgetTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning));
        }
        if (tuning.DashTriggerRadius > tuning.DetectionRadius)
        {
            throw new ArgumentOutOfRangeException(nameof(tuning), "Dash trigger radius cannot exceed detection radius.");
        }
    }
}
