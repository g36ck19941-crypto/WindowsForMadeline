using System.Reflection;
using CelesteDesktop.Entity.Seeker;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial state is patrolling", InitialState),
    ("snapshot exposes immutable identity and spawn", SnapshotIdentity),
    ("entity ID is required", EntityIdRequired),
    ("patrol bounds must have width", InvalidBoundsWidth),
    ("patrol bounds must contain spawn", InvalidBoundsSpawn),
    ("initial direction is bounded", InvalidDirection),
    ("patrol speed must be positive", InvalidPatrolSpeed),
    ("chase speed must be positive", InvalidChaseSpeed),
    ("dash speed must be positive", InvalidDashSpeed),
    ("detection radius must be positive", InvalidDetectionRadius),
    ("dash trigger radius must be positive", InvalidDashRadius),
    ("dash trigger cannot exceed detection", InvalidRadiusOrder),
    ("alert ticks must be positive", InvalidAlertTicks),
    ("windup ticks must be positive", InvalidWindupTicks),
    ("dash ticks must be positive", InvalidDashTicks),
    ("stun ticks must be positive", InvalidStunTicks),
    ("forget ticks must be positive", InvalidForgetTicks),
    ("target ID is required", TargetIdRequired),
    ("disabled input rejects target", DisabledTargetRejected),
    ("disabled input rejects wall", DisabledWallRejected),
    ("patrol advances at fixed sixty hertz", PatrolStep),
    ("patrol event is explicit", PatrolEvent),
    ("right bound turns left", TurnRight),
    ("left bound turns right", TurnLeft),
    ("in-range target starts alert", AlertStarts),
    ("detection boundary starts alert", BoundaryAlert),
    ("outside target is ignored", OutsideIgnored),
    ("ineligible target is ignored", IneligibleIgnored),
    ("alert locks target identity", AlertIdentity),
    ("alert freezes position", AlertFreezes),
    ("alert duration is exact", AlertDuration),
    ("chase start event is explicit", ChaseStartEvent),
    ("chase follows target direction", ChaseDirection),
    ("chase event is explicit", ChaseEvent),
    ("different target cannot replace lock", DifferentTargetIgnored),
    ("target loss grace is exact", TargetLossGrace),
    ("target loss returns to patrol", TargetLossReturns),
    ("near target starts windup", WindupStarts),
    ("windup duration is exact", WindupDuration),
    ("windup tracks latest target position", WindupTracksTarget),
    ("dash start direction is normalized", DashDirection),
    ("coincident dash uses patrol fallback", CenterFallback),
    ("coincident fallback event is explicit", CenterFallbackEvent),
    ("dash advances at fixed sixty hertz", DashStep),
    ("dash event is explicit", DashEvent),
    ("wall hit enters stunned without movement", WallHit),
    ("wall hit event is explicit", WallHitEvent),
    ("target hit is target addressed", TargetHit),
    ("target hit event is explicit", TargetHitEvent),
    ("other target cannot receive hit", OtherTargetNoHit),
    ("dash timeout enters stunned", DashTimeout),
    ("stun duration is exact", StunDuration),
    ("recovery resets spawn and direction", RecoveryReset),
    ("recovery event is explicit", RecoveryEvent),
    ("disabled transition is explicit", DisableTransition),
    ("disable cancels active state", DisableCancelsState),
    ("repeated disabled input is quiet", RepeatedDisabledQuiet),
    ("enabled transition resets spawn", EnableTransition),
    ("update requires active step", RequiresStep),
    ("controller updates once per tick", OncePerTick),
    ("snapshot events are immutable", EventsImmutable),
    ("event IDs are stable and unique", EventIds),
    ("same inputs replay deterministically", Replay),
    ("two Seekers remain isolated", TwoSeekerIsolation),
    ("Puffer failure state cannot affect Seeker", CrossEntityIsolation),
    ("public surface is isolated", SurfaceIsolation)
};

var failed = 0;
foreach (var test in tests)
{
    try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception exception) { failed++; Console.Error.WriteLine($"FAIL {test.Name}\n{exception}"); }
}
Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void InitialState()
{
    var seeker = Create();
    Equal(SeekerState.Patrolling, seeker.State);
    Equal(SimVector.Zero, seeker.Center);
    Equal(1, seeker.PatrolDirection);
}

static void SnapshotIdentity()
{
    var snapshot = Create("seeker-7").Step(SeekerInput.None, new SimulationWorld());
    Equal("seeker-7", snapshot.EntityId);
    Equal(SimVector.Zero, snapshot.SpawnCenter);
    Equal(1L, snapshot.Tick);
}

static void EntityIdRequired() => Throws<ArgumentException>(() => new SeekerController(" ", SimVector.Zero, -2m, 2m));
static void InvalidBoundsWidth() => Throws<ArgumentOutOfRangeException>(() => new SeekerController("s", SimVector.Zero, 0m, 0m));
static void InvalidBoundsSpawn() => Throws<ArgumentOutOfRangeException>(() => new SeekerController("s", new SimVector(3m, 0m), -2m, 2m));
static void InvalidDirection() => Throws<ArgumentOutOfRangeException>(() => new SeekerController("s", SimVector.Zero, -2m, 2m, 0));
static void InvalidPatrolSpeed() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { PatrolSpeed = 0m }));
static void InvalidChaseSpeed() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { ChaseSpeed = 0m }));
static void InvalidDashSpeed() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { DashSpeed = 0m }));
static void InvalidDetectionRadius() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { DetectionRadius = 0 }));
static void InvalidDashRadius() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { DashTriggerRadius = 0 }));
static void InvalidRadiusOrder() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { DetectionRadius = 1, DashTriggerRadius = 2 }));
static void InvalidAlertTicks() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { AlertTicks = 0 }));
static void InvalidWindupTicks() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { WindupTicks = 0 }));
static void InvalidDashTicks() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { DashTicks = 0 }));
static void InvalidStunTicks() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { StunTicks = 0 }));
static void InvalidForgetTicks() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { ForgetTicks = 0 }));
static void TargetIdRequired() => Throws<ArgumentException>(() => new SeekerTarget("", SimVector.Zero));
static void DisabledTargetRejected() => Throws<ArgumentException>(() => new SeekerInput(Target(), disableRequested: true));
static void DisabledWallRejected() => Throws<ArgumentException>(() => new SeekerInput(wallCollision: true, disableRequested: true));

static void PatrolStep() => Equal(new SimVector(1m, 0m), Create().Step(SeekerInput.None, new SimulationWorld()).Center);
static void PatrolEvent() => Has(Create().Step(SeekerInput.None, new SimulationWorld()), SeekerEventKind.Patrolled);

static void TurnRight()
{
    var snapshot = Create().Step(SeekerInput.None, new SimulationWorld());
    Equal(1m, snapshot.Center.X);
    Equal(-1, snapshot.PatrolDirection);
    Has(snapshot, SeekerEventKind.Turned);
}

static void TurnLeft()
{
    var snapshot = Create(direction: -1).Step(SeekerInput.None, new SimulationWorld());
    Equal(-1m, snapshot.Center.X);
    Equal(1, snapshot.PatrolDirection);
    Has(snapshot, SeekerEventKind.Turned);
}

static void AlertStarts()
{
    var snapshot = StartAlert();
    Equal(SeekerState.Alerted, snapshot.State);
    Equal(2, snapshot.StateTicksRemaining);
    Has(snapshot, SeekerEventKind.Alerted);
}

static void BoundaryAlert()
{
    var seeker = Create(bounds: (-20m, 20m));
    Equal(SeekerState.Alerted, seeker.Step(new SeekerInput(Target(new SimVector(11m, 0m))), new SimulationWorld()).State);
}

static void OutsideIgnored()
{
    var snapshot = Create(bounds: (-20m, 20m)).Step(new SeekerInput(Target(new SimVector(12m, 0m))), new SimulationWorld());
    Equal(SeekerState.Patrolling, snapshot.State);
    Has(snapshot, SeekerEventKind.TargetIgnored);
}

static void IneligibleIgnored() => Has(Create().Step(new SeekerInput(Target(canDetect: false)), new SimulationWorld()), SeekerEventKind.TargetIgnored);
static void AlertIdentity() => Equal("player-7", StartAlert("player-7").LockedTargetId);

static void AlertFreezes()
{
    var (world, seeker) = AlertController();
    var before = seeker.Center;
    Equal(before, seeker.Step(SeekerInput.None, world).Center);
}

static void AlertDuration()
{
    var (world, seeker) = AlertController();
    Equal(1, seeker.Step(SeekerInput.None, world).StateTicksRemaining);
    Equal(SeekerState.Chasing, seeker.Step(SeekerInput.None, world).State);
}

static void ChaseStartEvent()
{
    var (world, seeker) = AlertController();
    _ = seeker.Step(SeekerInput.None, world);
    Has(seeker.Step(SeekerInput.None, world), SeekerEventKind.ChaseStarted);
}

static void ChaseDirection()
{
    var (world, seeker) = ChaseController(Target(new SimVector(8m, 0m)));
    var before = seeker.Center;
    var snapshot = seeker.Step(new SeekerInput(Target(new SimVector(8m, 0m))), world);
    Equal(before.X + 1m, snapshot.Center.X);
}

static void ChaseEvent()
{
    var (world, seeker) = ChaseController(Target(new SimVector(8m, 0m)));
    Has(seeker.Step(new SeekerInput(Target(new SimVector(8m, 0m))), world), SeekerEventKind.Chased);
}

static void DifferentTargetIgnored()
{
    var (world, seeker) = ChaseController(Target(new SimVector(8m, 0m), "player-a"));
    var snapshot = seeker.Step(new SeekerInput(Target(new SimVector(-8m, 0m), "player-b")), world);
    Equal("player-a", snapshot.LockedTargetId);
    Equal(1, snapshot.LostSightTicks);
}

static void TargetLossGrace()
{
    var (world, seeker) = ChaseController(Target(new SimVector(8m, 0m)));
    var snapshot = seeker.Step(SeekerInput.None, world);
    Equal(SeekerState.Chasing, snapshot.State);
    Equal(1, snapshot.LostSightTicks);
}

static void TargetLossReturns()
{
    var (world, seeker) = ChaseController(Target(new SimVector(8m, 0m)));
    _ = seeker.Step(SeekerInput.None, world);
    var snapshot = seeker.Step(SeekerInput.None, world);
    Equal(SeekerState.Patrolling, snapshot.State);
    Has(snapshot, SeekerEventKind.TargetLost);
}

static void WindupStarts()
{
    var (world, seeker) = ChaseController(Target(new SimVector(2m, 0m)));
    var snapshot = seeker.Step(new SeekerInput(Target(new SimVector(2m, 0m))), world);
    Equal(SeekerState.Windup, snapshot.State);
    Has(snapshot, SeekerEventKind.WindupStarted);
}

static void WindupDuration()
{
    var (world, seeker) = WindupController();
    Equal(1, seeker.Step(new SeekerInput(Target(new SimVector(4m, 0m))), world).StateTicksRemaining);
    Equal(SeekerState.Dashing, seeker.Step(new SeekerInput(Target(new SimVector(4m, 0m))), world).State);
}

static void WindupTracksTarget()
{
    var (world, seeker) = WindupController();
    _ = seeker.Step(new SeekerInput(Target(new SimVector(1m, -4m))), world);
    var snapshot = seeker.Step(new SeekerInput(Target(new SimVector(1m, -4m))), world);
    Equal(new SimVector(0m, -1m), snapshot.DashDirection);
}

static void DashDirection()
{
    var snapshot = StartDash(new SimVector(4m, 4m));
    Equal(0.6m, snapshot.DashDirection.X);
    Equal(0.8m, snapshot.DashDirection.Y);
}

static void CenterFallback()
{
    var snapshot = StartDash(new SimVector(1m, 0m));
    Equal(new SimVector(1m, 0m), snapshot.DashDirection);
}

static void CenterFallbackEvent() => Has(StartDash(new SimVector(1m, 0m)), SeekerEventKind.CenterFallbackUsed);

static void DashStep()
{
    var (world, seeker) = DashController(new SimVector(5m, 0m));
    var before = seeker.Center;
    var snapshot = seeker.Step(SeekerInput.None, world);
    Equal(before.X + 2m, snapshot.Center.X);
}

static void DashEvent()
{
    var (world, seeker) = DashController(new SimVector(5m, 0m));
    Has(seeker.Step(SeekerInput.None, world), SeekerEventKind.Dashed);
}

static void WallHit()
{
    var (world, seeker) = DashController(new SimVector(5m, 0m));
    var before = seeker.Center;
    var snapshot = seeker.Step(new SeekerInput(wallCollision: true), world);
    Equal(before, snapshot.Center);
    Equal(SeekerState.Stunned, snapshot.State);
}

static void WallHitEvent()
{
    var (world, seeker) = DashController(new SimVector(5m, 0m));
    var snapshot = seeker.Step(new SeekerInput(wallCollision: true), world);
    Has(snapshot, SeekerEventKind.WallHit);
    Has(snapshot, SeekerEventKind.Stunned);
}

static void TargetHit()
{
    var (world, seeker) = DashController(new SimVector(5m, 0m));
    var snapshot = seeker.Step(new SeekerInput(Target(new SimVector(5m, 0m), touching: true)), world);
    Equal("player", snapshot.HitEffect!.TargetId);
    Equal(new SimVector(1m, 0m), snapshot.HitEffect.ImpactDirection);
    Equal(SeekerState.Stunned, snapshot.State);
}

static void TargetHitEvent()
{
    var (world, seeker) = DashController(new SimVector(5m, 0m));
    Has(seeker.Step(new SeekerInput(Target(new SimVector(5m, 0m), touching: true)), world), SeekerEventKind.TargetHit);
}

static void OtherTargetNoHit()
{
    var (world, seeker) = DashController(new SimVector(5m, 0m));
    var snapshot = seeker.Step(new SeekerInput(Target(new SimVector(5m, 0m), "other", touching: true)), world);
    Assert(snapshot.HitEffect is null, "Different target received Seeker hit.");
    Equal(SeekerState.Dashing, snapshot.State);
}

static void DashTimeout()
{
    var (world, seeker) = DashController(new SimVector(5m, 0m));
    _ = seeker.Step(SeekerInput.None, world);
    _ = seeker.Step(SeekerInput.None, world);
    Equal(SeekerState.Stunned, seeker.Step(SeekerInput.None, world).State);
}

static void StunDuration()
{
    var (world, seeker) = StunnedController();
    Equal(SeekerState.Stunned, seeker.Step(SeekerInput.None, world).State);
    Equal(SeekerState.Patrolling, seeker.Step(SeekerInput.None, world).State);
}

static void RecoveryReset()
{
    var (world, seeker) = StunnedController(direction: -1);
    _ = seeker.Step(SeekerInput.None, world);
    var snapshot = seeker.Step(SeekerInput.None, world);
    Equal(SimVector.Zero, snapshot.Center);
    Equal(-1, snapshot.PatrolDirection);
}

static void RecoveryEvent()
{
    var (world, seeker) = StunnedController();
    _ = seeker.Step(SeekerInput.None, world);
    Has(seeker.Step(SeekerInput.None, world), SeekerEventKind.Recovered);
}

static void DisableTransition()
{
    var snapshot = Create().Step(SeekerInput.Disabled, new SimulationWorld());
    Equal(SeekerState.Disabled, snapshot.State);
    Has(snapshot, SeekerEventKind.Disabled);
}

static void DisableCancelsState()
{
    var (world, seeker) = ChaseController(Target(new SimVector(8m, 0m)));
    var snapshot = seeker.Step(SeekerInput.Disabled, world);
    Equal(SeekerState.Disabled, snapshot.State);
    Equal<string?>(null, snapshot.LockedTargetId);
}

static void RepeatedDisabledQuiet()
{
    var world = new SimulationWorld();
    var seeker = Create();
    _ = seeker.Step(SeekerInput.Disabled, world);
    Equal(0, seeker.Step(SeekerInput.Disabled, world).Events.Count);
}

static void EnableTransition()
{
    var world = new SimulationWorld();
    var seeker = Create(direction: -1);
    _ = seeker.Step(SeekerInput.None, world);
    _ = seeker.Step(SeekerInput.Disabled, world);
    var snapshot = seeker.Step(SeekerInput.None, world);
    Equal(SeekerState.Patrolling, snapshot.State);
    Equal(SimVector.Zero, snapshot.Center);
    Equal(-1, snapshot.PatrolDirection);
    Has(snapshot, SeekerEventKind.Enabled);
}

static void RequiresStep() => Throws<InvalidOperationException>(() => Create().Update(SeekerInput.None, new SimulationWorld()));

static void OncePerTick()
{
    var world = new SimulationWorld();
    var seeker = Create();
    Throws<InvalidOperationException>(() => world.Step(current =>
    {
        _ = seeker.Update(SeekerInput.None, current);
        _ = seeker.Update(SeekerInput.None, current);
    }));
}

static void EventsImmutable()
{
    var snapshot = Create().Step(SeekerInput.None, new SimulationWorld());
    Throws<NotSupportedException>(() => ((ICollection<SeekerEvent>)snapshot.Events).Add(snapshot.Events[0]));
}

static void EventIds()
{
    var ids = Enum.GetValues<SeekerEventKind>().Select(SeekerEventIds.For).ToArray();
    Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    Assert(ids.All(id => id.StartsWith("SEEKER_", StringComparison.Ordinal)), "Seeker event prefix changed.");
}

static void Replay() => Equal(RunReplay(), RunReplay());

static void TwoSeekerIsolation()
{
    var world = new SimulationWorld();
    var first = Create("seeker-a", bounds: (-20m, 20m));
    var second = Create("seeker-b", direction: -1, bounds: (-20m, 20m));
    SeekerSnapshot? a = null;
    SeekerSnapshot? b = null;
    world.Step(current =>
    {
        a = first.Update(new SeekerInput(Target()), current);
        b = second.Update(SeekerInput.None, current);
    });
    Equal(SeekerState.Alerted, a!.State);
    Equal(SeekerState.Patrolling, b!.State);
}

static void CrossEntityIsolation()
{
    var world = new SimulationWorld();
    var seeker = Create();
    var snapshot = seeker.Step(SeekerInput.None, world);
    Equal(SeekerState.Patrolling, snapshot.State);
    Assert(snapshot.Events.All(item => !item.EventId.StartsWith("PUFFER_", StringComparison.Ordinal)), "Puffer event leaked into Seeker.");
}

static void SurfaceIsolation()
{
    var text = string.Join(' ', typeof(SeekerController).Assembly.GetExportedTypes().SelectMany(type =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Window", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Rendering", "CelesteDesktop.Desktop", "CelesteDesktop.Player", "CelesteDesktop.Entity.Puffer" })
    {
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
    }
}

static string RunReplay()
{
    var world = new SimulationWorld();
    var seeker = Create(bounds: (-20m, 20m));
    var rows = new List<string>();
    for (var tick = 0; tick < 16; tick++)
    {
        var target = tick is >= 1 and <= 9 ? Target(new SimVector(6m, 0m), touching: tick == 9) : null;
        rows.Add(Format(seeker.Step(new SeekerInput(target, wallCollision: tick == 13), world)));
    }
    return string.Join('|', rows);
}

static SeekerSnapshot StartAlert(string id = "player") =>
    Create(bounds: (-20m, 20m)).Step(new SeekerInput(Target(id: id)), new SimulationWorld());

static (SimulationWorld World, SeekerController Seeker) AlertController(SeekerTarget? target = null)
{
    var world = new SimulationWorld();
    var seeker = Create(bounds: (-20m, 20m));
    _ = seeker.Step(new SeekerInput(target ?? Target()), world);
    return (world, seeker);
}

static (SimulationWorld World, SeekerController Seeker) ChaseController(SeekerTarget target)
{
    var (world, seeker) = AlertController(target);
    _ = seeker.Step(new SeekerInput(target), world);
    _ = seeker.Step(new SeekerInput(target), world);
    return (world, seeker);
}

static (SimulationWorld World, SeekerController Seeker) WindupController()
{
    var (world, seeker) = ChaseController(Target(new SimVector(2m, 0m)));
    _ = seeker.Step(new SeekerInput(Target(new SimVector(2m, 0m))), world);
    return (world, seeker);
}

static SeekerSnapshot StartDash(SimVector target)
{
    var (_, _, snapshot) = AdvanceToDash(target);
    return snapshot;
}

static (SimulationWorld World, SeekerController Seeker) DashController(SimVector target, int direction = 1)
{
    var (world, seeker, _) = AdvanceToDash(target, direction);
    return (world, seeker);
}

static (SimulationWorld World, SeekerController Seeker, SeekerSnapshot Snapshot) AdvanceToDash(
    SimVector target,
    int direction = 1)
{
    var world = new SimulationWorld();
    var seeker = Create(direction: direction, bounds: (-20m, 20m));
    var observed = Target(target);
    SeekerSnapshot? snapshot = null;
    for (var step = 0; step < 20; step++)
    {
        snapshot = seeker.Step(new SeekerInput(observed), world);
        if (snapshot.State == SeekerState.Dashing)
        {
            return (world, seeker, snapshot);
        }
    }
    throw new InvalidOperationException("Seeker did not enter Dashing within the bounded test sequence.");
}

static (SimulationWorld World, SeekerController Seeker) StunnedController(int direction = 1)
{
    var (world, seeker) = DashController(new SimVector(5m, 0m), direction);
    _ = seeker.Step(new SeekerInput(wallCollision: true), world);
    return (world, seeker);
}

static SeekerController Create(
    string id = "seeker",
    int direction = 1,
    SeekerTuning? tuning = null,
    (decimal Left, decimal Right)? bounds = null)
{
    var range = bounds ?? (-1m, 1m);
    return new SeekerController(id, SimVector.Zero, range.Left, range.Right, direction, tuning ?? TestTuning());
}

static SeekerTuning TestTuning() => new(60m, 60m, 120m, 10, 2, 2, 2, 3, 2, 2);

static SeekerTarget Target(
    SimVector? center = null,
    string id = "player",
    bool canDetect = true,
    bool touching = false) => new(id, center ?? new SimVector(5m, 0m), canDetect, touching);

static string Format(SeekerSnapshot snapshot) =>
    $"{snapshot.Tick}:{snapshot.Center}:{snapshot.State}:{snapshot.PatrolDirection}:{snapshot.StateTicksRemaining}:{snapshot.LostSightTicks}:{snapshot.LockedTargetId}:{snapshot.DashDirection}:{snapshot.HitEffect?.TargetId}:{string.Join(',', snapshot.Events.Select(item => item.EventId))}";

static void Has(SeekerSnapshot snapshot, SeekerEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing Seeker event {kind}.");

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
