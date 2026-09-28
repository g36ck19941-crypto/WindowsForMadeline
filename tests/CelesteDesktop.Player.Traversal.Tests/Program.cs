using System.Collections;
using System.Reflection;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("reference traversal constants match recorded facts", ReferenceConstantsMatch),
    ("cardinal dash directions are exact", CardinalDirectionsAreExact),
    ("diagonal dash direction is normalized", DiagonalDirectionIsNormalized),
    ("neutral dash direction uses facing", NeutralDirectionUsesFacing),
    ("dash consumes one charge", DashConsumesCharge),
    ("infinite dash assist preserves charge", InfiniteDashPreservesCharge),
    ("dash begins at reference speed", DashStartsAtReferenceSpeed),
    ("dash lasts exactly nine ticks", DashLastsNineTicks),
    ("horizontal dash ends at reference speed", HorizontalDashEndSpeed),
    ("up dash end applies vertical multiplier", UpDashEndSpeed),
    ("down dash retains dash speed", DownDashEndSpeed),
    ("dash cooldown blocks immediate restart", DashCooldownBlocksRestart),
    ("dash attack timer is explicit", DashAttackTimerIsExplicit),
    ("dash collision emits blocked event", DashCollisionIsExplicit),
    ("faster same-direction speed is preserved", FasterSpeedIsPreserved),
    ("ground refills dash after cooldown", GroundRefillsDash),
    ("dash updates facing", DashUpdatesFacing),
    ("wall jump checks three pixels", WallJumpChecksThreePixels),
    ("wall jump rejects fourth pixel", WallJumpRejectsFourthPixel),
    ("wall jump emits reference launch speed", WallJumpUsesReferenceSpeed),
    ("wall slide starts against approached wall", WallSlideStarts),
    ("wall slide reduces fall speed", WallSlideReducesFallSpeed),
    ("wall slide down input releases", WallSlideDownReleases),
    ("wall slide jump launches away", WallSlideJumpLaunchesAway),
    ("grab enters climb state", GrabEntersClimb),
    ("climb grab scales vertical speed", ClimbGrabScalesSpeed),
    ("climb no-move timer is six ticks", ClimbNoMoveTimerIsBounded),
    ("climb up reaches reference speed", ClimbUpReachesSpeed),
    ("climb up consumes stamina", ClimbUpConsumesStamina),
    ("climb still consumes stamina", ClimbStillConsumesStamina),
    ("climb down does not consume stamina", ClimbDownDoesNotConsumeStamina),
    ("tired threshold is exposed", TiredThresholdIsExposed),
    ("zero stamina releases climb", StaminaDepletionReleases),
    ("infinite stamina assist preserves stamina", InfiniteStaminaPreserves),
    ("releasing grab exits climb", GrabReleaseExitsClimb),
    ("climb jump consumes fixed stamina", ClimbJumpConsumesStamina),
    ("climb wall jump launches without climb cost", ClimbWallJumpHasNoClimbCost),
    ("climbing over a ledge starts hop", LedgeStartsHop),
    ("dash can start from climb", DashStartsFromClimb),
    ("traversal update requires active step", UpdateRequiresActiveStep),
    ("traversal updates once per tick", UpdateRunsOncePerTick),
    ("traversal snapshots expose immutable events", SnapshotEventsAreImmutable),
    ("same traversal replay is deterministic", ReplayIsDeterministic),
    ("traversal surface has no live platform dependency", SurfaceHasNoPlatformDependency)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void ReferenceConstantsMatch()
{
    var tuning = PlayerTraversalTuning.ReferencePartial;
    Equal(240m, tuning.DashSpeed);
    Equal(160m, tuning.EndDashSpeed);
    Equal(0.75m, tuning.EndDashUpMultiplier);
    Equal(9, tuning.DashTicks);
    Equal(12, tuning.DashCooldownTicks);
    Equal(6, tuning.DashRefillCooldownTicks);
    Equal(18, tuning.DashAttackTicks);
    Equal(3, tuning.WallJumpCheckDistance);
    Equal(130m, tuning.WallJumpHorizontalSpeed);
    Equal(-105m, tuning.WallJumpVerticalSpeed);
    Equal(20m, tuning.WallSlideStartMax);
    Equal(72, tuning.WallSlideTicks);
    Equal(110m, tuning.ClimbMaxStamina);
    Equal(20m, tuning.ClimbTiredThreshold);
    Equal(27.5m, tuning.ClimbJumpCost);
    Equal(2, tuning.ClimbCheckDistance);
    Equal(6, tuning.ClimbNoMoveTicks);
    Equal(-45m, tuning.ClimbUpSpeed);
    Equal(80m, tuning.ClimbDownSpeed);
    Equal(30m, tuning.ClimbSlipSpeed);
    Equal(900m, tuning.ClimbAcceleration);
    Equal(0.2m, tuning.ClimbGrabYMultiplier);
    Equal(100m, tuning.ClimbHopX);
    Equal(-120m, tuning.ClimbHopY);
}

static void CardinalDirectionsAreExact()
{
    Equal(new SimVector(1m, 0m), PlayerTraversalController.QuantizeDashDirection(1, 0, 1));
    Equal(new SimVector(0m, -1m), PlayerTraversalController.QuantizeDashDirection(0, -1, 1));
}

static void DiagonalDirectionIsNormalized()
{
    var direction = PlayerTraversalController.QuantizeDashDirection(-1, 1, 1);
    Equal(-0.7071067811865475m, direction.X);
    Equal(0.7071067811865475m, direction.Y);
}

static void NeutralDirectionUsesFacing() =>
    Equal(new SimVector(-1m, 0m), PlayerTraversalController.QuantizeDashDirection(0, 0, -1));

static void DashConsumesCharge()
{
    var fixture = Airborne();
    var snapshot = fixture.Controller.Step(Input(dash: true), fixture.World);
    Equal(0, snapshot.Dashes);
    HasEvent(snapshot, PlayerTraversalEventKind.DashStarted);
}

static void InfiniteDashPreservesCharge()
{
    var fixture = Airborne(assists: new PlayerAssistSettings(InfiniteDashes: true));
    Equal(1, fixture.Controller.Step(Input(dash: true), fixture.World).Dashes);
}

static void DashStartsAtReferenceSpeed()
{
    var fixture = Airborne();
    var snapshot = fixture.Controller.Step(Input(moveX: 1, dash: true), fixture.World);
    Equal(240m, snapshot.Speed.X);
    Equal(0m, snapshot.Speed.Y);
    Equal(PlayerTraversalState.Dash, snapshot.State);
}

static void DashLastsNineTicks()
{
    var fixture = Airborne();
    fixture.Controller.Step(Input(moveX: 1, dash: true), fixture.World);
    for (var index = 0; index < 7; index++)
    {
        Equal(PlayerTraversalState.Dash, fixture.Controller.Step(Input(), fixture.World).State);
    }
    var end = fixture.Controller.Step(Input(), fixture.World);
    Equal(PlayerTraversalState.Normal, end.State);
    HasEvent(end, PlayerTraversalEventKind.DashEnded);
}

static void HorizontalDashEndSpeed()
{
    var end = RunDash(Input(moveX: 1, dash: true));
    Equal(160m, end.Speed.X);
    Equal(0m, end.Speed.Y);
}

static void UpDashEndSpeed()
{
    var end = RunDash(Input(moveY: -1, dash: true));
    Equal(0m, end.Speed.X);
    Equal(-120m, end.Speed.Y);
}

static void DownDashEndSpeed()
{
    var end = RunDash(Input(moveY: 1, dash: true));
    Equal(0m, end.Speed.X);
    Equal(240m, end.Speed.Y);
}

static void DashCooldownBlocksRestart()
{
    var fixture = Airborne(assists: new PlayerAssistSettings(InfiniteDashes: true));
    fixture.Controller.Step(Input(moveX: 1, dash: true), fixture.World);
    for (var index = 0; index < 8; index++)
    {
        fixture.Controller.Step(Input(), fixture.World);
    }
    var blocked = fixture.Controller.Step(Input(moveX: -1, dash: true), fixture.World);
    Assert(!blocked.Events.Any(item => item.Kind == PlayerTraversalEventKind.DashStarted), "Dash restarted during cooldown.");
}

static void DashAttackTimerIsExplicit()
{
    var fixture = Airborne();
    Equal(18, fixture.Controller.Step(Input(dash: true), fixture.World).DashAttackTicks);
    Equal(17, fixture.Controller.Step(Input(), fixture.World).DashAttackTicks);
}

static void DashCollisionIsExplicit()
{
    var fixture = RightWall();
    var snapshot = fixture.Controller.Step(Input(moveX: 1, dash: true), fixture.World);
    Equal(0m, snapshot.Speed.X);
    HasEvent(snapshot, PlayerTraversalEventKind.DashBlocked);
}

static void FasterSpeedIsPreserved()
{
    var fixture = Airborne(initialSpeed: new SimVector(300m, 0m));
    Equal(300m, fixture.Controller.Step(Input(moveX: 1, dash: true), fixture.World).Speed.X);
}

static void GroundRefillsDash()
{
    var fixture = Grounded();
    fixture.Controller.Step(Input(moveX: 1, dash: true), fixture.World);
    PlayerTraversalSnapshot? snapshot = null;
    for (var index = 0; index < 6; index++)
    {
        snapshot = fixture.Controller.Step(Input(), fixture.World);
    }
    Equal(1, snapshot!.Dashes);
    HasEvent(snapshot, PlayerTraversalEventKind.DashRefilled);
}

static void DashUpdatesFacing()
{
    var fixture = Airborne();
    Equal(-1, fixture.Controller.Step(Input(moveX: -1, dash: true), fixture.World).Facing);
}

static void WallJumpChecksThreePixels()
{
    var fixture = RightWall(gap: 2);
    HasEvent(fixture.Controller.Step(Input(moveX: 1, jumpPressed: true, jumpHeld: true), fixture.World),
        PlayerTraversalEventKind.WallJumped);
}

static void WallJumpRejectsFourthPixel()
{
    var fixture = RightWall(gap: 3);
    var snapshot = fixture.Controller.Step(Input(moveX: 1, jumpPressed: true, jumpHeld: true), fixture.World);
    Assert(!snapshot.Events.Any(item => item.Kind == PlayerTraversalEventKind.WallJumped), "Fourth-pixel wall jump fired.");
}

static void WallJumpUsesReferenceSpeed()
{
    var fixture = RightWall();
    var snapshot = fixture.Controller.Step(Input(moveX: 1, jumpPressed: true, jumpHeld: true), fixture.World);
    Equal(-130m, snapshot.Speed.X);
    Equal(-105m, snapshot.Speed.Y);
}

static void WallSlideStarts()
{
    var fixture = RightWall(initialSpeed: new SimVector(0m, 60m));
    var snapshot = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    Equal(PlayerTraversalState.WallSlide, snapshot.State);
    HasEvent(snapshot, PlayerTraversalEventKind.WallSlideStarted);
}

static void WallSlideReducesFallSpeed()
{
    var fixture = RightWall(initialSpeed: new SimVector(0m, 60m));
    var started = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    var sliding = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    Equal(started.Speed.Y - 15m, sliding.Speed.Y);
    Equal(71, sliding.WallSlideTicks);
}

static void WallSlideDownReleases()
{
    var fixture = RightWall(initialSpeed: new SimVector(0m, 60m));
    fixture.Controller.Step(Input(moveX: 1), fixture.World);
    var snapshot = fixture.Controller.Step(Input(moveX: 1, moveY: 1), fixture.World);
    Equal(PlayerTraversalState.Normal, snapshot.State);
    HasEvent(snapshot, PlayerTraversalEventKind.WallSlideEnded);
}

static void WallSlideJumpLaunchesAway()
{
    var fixture = RightWall(initialSpeed: new SimVector(0m, 60m));
    fixture.Controller.Step(Input(moveX: 1), fixture.World);
    var snapshot = fixture.Controller.Step(Input(moveX: 1, jumpPressed: true, jumpHeld: true), fixture.World);
    Equal(-130m, snapshot.Speed.X);
    Equal(-105m, snapshot.Speed.Y);
    HasEvent(snapshot, PlayerTraversalEventKind.WallJumped);
}

static void GrabEntersClimb()
{
    var fixture = RightWall();
    var snapshot = fixture.Controller.Step(Input(moveX: 1, grab: true), fixture.World);
    Equal(PlayerTraversalState.Climb, snapshot.State);
    HasEvent(snapshot, PlayerTraversalEventKind.ClimbStarted);
}

static void ClimbGrabScalesSpeed()
{
    var fixture = RightWall(initialSpeed: new SimVector(0m, 100m));
    Equal(5m, fixture.Controller.Step(Input(moveX: 1, grab: true), fixture.World).Speed.Y);
}

static void ClimbNoMoveTimerIsBounded()
{
    var fixture = EnterClimb();
    Equal(5, fixture.Last.ClimbNoMoveTicks);
    for (var index = 0; index < 5; index++)
    {
        fixture.Last = fixture.Fixture.Controller.Step(Input(moveX: 1, moveY: -1, grab: true), fixture.Fixture.World);
    }
    Equal(0, fixture.Last.ClimbNoMoveTicks);
}

static void ClimbUpReachesSpeed()
{
    var fixture = EnterClimb();
    for (var index = 0; index < 7; index++)
    {
        fixture.Last = fixture.Fixture.Controller.Step(Input(moveX: 1, moveY: -1, grab: true), fixture.Fixture.World);
    }
    Equal(-45m, fixture.Last.Speed.Y);
}

static void ClimbUpConsumesStamina()
{
    var fixture = EnterClimb();
    for (var index = 0; index < 5; index++)
    {
        fixture.Last = fixture.Fixture.Controller.Step(Input(moveX: 1, moveY: -1, grab: true), fixture.Fixture.World);
    }
    Assert(fixture.Last.Stamina < 110m, "Upward climb did not consume stamina.");
}

static void ClimbStillConsumesStamina()
{
    var fixture = EnterClimb();
    for (var index = 0; index < 5; index++)
    {
        fixture.Last = fixture.Fixture.Controller.Step(Input(moveX: 1, grab: true), fixture.Fixture.World);
    }
    Assert(fixture.Last.Stamina < 110m, "Still climb did not consume stamina.");
}

static void ClimbDownDoesNotConsumeStamina()
{
    var fixture = EnterClimb();
    for (var index = 0; index < 8; index++)
    {
        fixture.Last = fixture.Fixture.Controller.Step(Input(moveX: 1, moveY: 1, grab: true), fixture.Fixture.World);
    }
    Equal(110m, fixture.Last.Stamina);
}

static void TiredThresholdIsExposed()
{
    var tuning = PlayerTraversalTuning.ReferencePartial with
    {
        ClimbMaxStamina = 6m,
        ClimbTiredThreshold = 5.5m,
        ClimbStillCostPerSecond = 60m,
        ClimbNoMoveTicks = 1
    };
    var fixture = RightWall(tuning: tuning);
    var snapshot = fixture.Controller.Step(Input(moveX: 1, grab: true), fixture.World);
    Assert(snapshot.IsTired, "Tired threshold was not reflected in the snapshot.");
}

static void StaminaDepletionReleases()
{
    var tuning = PlayerTraversalTuning.ReferencePartial with
    {
        ClimbMaxStamina = 1m,
        ClimbTiredThreshold = 0.5m,
        ClimbUpCostPerSecond = 60m,
        ClimbNoMoveTicks = 1
    };
    var fixture = RightWall(tuning: tuning);
    var snapshot = fixture.Controller.Step(Input(moveX: 1, moveY: -1, grab: true), fixture.World);
    Equal(PlayerTraversalState.Normal, snapshot.State);
    Equal(0m, snapshot.Stamina);
    HasEvent(snapshot, PlayerTraversalEventKind.StaminaDepleted);
}

static void InfiniteStaminaPreserves()
{
    var fixture = RightWall(assists: new PlayerAssistSettings(InfiniteStamina: true));
    PlayerTraversalSnapshot snapshot = fixture.Controller.Step(Input(moveX: 1, moveY: -1, grab: true), fixture.World);
    for (var index = 0; index < 20; index++)
    {
        snapshot = fixture.Controller.Step(Input(moveX: 1, moveY: -1, grab: true), fixture.World);
    }
    Equal(110m, snapshot.Stamina);
}

static void GrabReleaseExitsClimb()
{
    var entered = EnterClimb();
    var snapshot = entered.Fixture.Controller.Step(Input(), entered.Fixture.World);
    Equal(PlayerTraversalState.Normal, snapshot.State);
    HasEvent(snapshot, PlayerTraversalEventKind.ClimbReleased);
}

static void ClimbJumpConsumesStamina()
{
    var entered = EnterClimb();
    var snapshot = entered.Fixture.Controller.Step(Input(moveX: 1, jumpPressed: true, jumpHeld: true, grab: true), entered.Fixture.World);
    Equal(82.5m, snapshot.Stamina);
    Equal(-105m, snapshot.Speed.Y);
    HasEvent(snapshot, PlayerTraversalEventKind.ClimbJumped);
}

static void ClimbWallJumpHasNoClimbCost()
{
    var entered = EnterClimb();
    var snapshot = entered.Fixture.Controller.Step(Input(moveX: -1, jumpPressed: true, jumpHeld: true, grab: true), entered.Fixture.World);
    Equal(110m, snapshot.Stamina);
    Equal(-130m, snapshot.Speed.X);
    HasEvent(snapshot, PlayerTraversalEventKind.WallJumped);
}

static void LedgeStartsHop()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    world.Add(actor);
    world.Add(new Solid("short-wall", 8, 0, 4, 12));
    var controller = new PlayerTraversalController(actor);
    var snapshot = controller.Step(Input(moveX: 1, moveY: -1, grab: true), world);
    for (var index = 0; index < 40 && snapshot.State == PlayerTraversalState.Climb; index++)
    {
        snapshot = controller.Step(Input(moveX: 1, moveY: -1, grab: true), world);
    }
    Equal(PlayerTraversalState.Normal, snapshot.State);
    HasEvent(snapshot, PlayerTraversalEventKind.ClimbHop);
    Equal(100m, snapshot.Speed.X);
    Equal(-120m, snapshot.Speed.Y);
}

static void DashStartsFromClimb()
{
    var entered = EnterClimb();
    var snapshot = entered.Fixture.Controller.Step(Input(moveX: -1, dash: true, grab: true), entered.Fixture.World);
    Equal(PlayerTraversalState.Dash, snapshot.State);
    HasEvent(snapshot, PlayerTraversalEventKind.DashStarted);
}

static void UpdateRequiresActiveStep()
{
    var fixture = Airborne();
    Throws<InvalidOperationException>(() => fixture.Controller.Update(Input(), fixture.World));
}

static void UpdateRunsOncePerTick()
{
    var fixture = Airborne();
    Throws<InvalidOperationException>(() => fixture.World.Step(world =>
    {
        fixture.Controller.Update(Input(), world);
        fixture.Controller.Update(Input(), world);
    }));
}

static void SnapshotEventsAreImmutable()
{
    var fixture = Airborne();
    var snapshot = fixture.Controller.Step(Input(dash: true), fixture.World);
    Assert(snapshot.Events is IList list && list.IsReadOnly, "Traversal events are mutable.");
}

static void ReplayIsDeterministic() => Equal(RunReplay(), RunReplay());

static void SurfaceHasNoPlatformDependency()
{
    var assembly = typeof(PlayerTraversalController).Assembly;
    var exposed = assembly.GetExportedTypes()
        .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        .SelectMany(MemberTypes)
        .Where(type => type is not null)
        .Cast<Type>();
    var forbidden = new[] { "System.Windows", "Microsoft.Win32", "System.IO.", "System.Diagnostics", "System.Threading" };
    Assert(!exposed.Any(type => forbidden.Any(prefix => (type.FullName ?? string.Empty).StartsWith(prefix, StringComparison.Ordinal))),
        "A GUI, file, clock, thread, or live-input type leaked into the traversal surface.");
}

static PlayerTraversalSnapshot RunDash(PlayerInput first)
{
    var fixture = Airborne();
    var snapshot = fixture.Controller.Step(first, fixture.World);
    for (var index = 0; index < 8; index++)
    {
        snapshot = fixture.Controller.Step(Input(), fixture.World);
    }
    return snapshot;
}

static string RunReplay()
{
    var fixture = RightWall(initialSpeed: new SimVector(0m, 60m));
    var inputs = new[]
    {
        Input(moveX: 1),
        Input(moveX: 1, jumpPressed: true, jumpHeld: true),
        Input(moveX: -1, dash: true),
        Input(), Input(), Input(), Input(), Input(), Input(), Input(), Input()
    };
    return string.Join('|', inputs.Select(input =>
    {
        var snapshot = fixture.Controller.Step(input, fixture.World);
        return $"{snapshot.Tick}:{snapshot.State}:{snapshot.Position.X},{snapshot.Position.Y}:{snapshot.Speed.X},{snapshot.Speed.Y}:{snapshot.Dashes}:{snapshot.Stamina}:{string.Join(',', snapshot.Events.Select(item => item.Kind))}";
    }));
}

static PlayerInput Input(
    int moveX = 0,
    int moveY = 0,
    bool jumpPressed = false,
    bool jumpHeld = false,
    bool dash = false,
    bool grab = false) => new(moveX, moveY, jumpPressed, jumpHeld, dash, grab);

static TraversalFixture Airborne(
    SimVector initialSpeed = default,
    PlayerAssistSettings? assists = null,
    PlayerTraversalTuning? tuning = null)
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    world.Add(actor);
    return new TraversalFixture(world, actor,
        new PlayerTraversalController(actor, traversalTuning: tuning, assists: assists, initialSpeed: initialSpeed));
}

static TraversalFixture Grounded()
{
    var fixture = Airborne();
    fixture.World.Add(new Solid("floor", -100, 11, 400, 4));
    return fixture;
}

static TraversalFixture RightWall(
    int gap = 0,
    SimVector initialSpeed = default,
    PlayerAssistSettings? assists = null,
    PlayerTraversalTuning? tuning = null)
{
    var fixture = Airborne(initialSpeed, assists, tuning);
    fixture.World.Add(new Solid("wall", 8 + gap, -200, 4, 400));
    return fixture;
}

static ClimbFixture EnterClimb()
{
    var fixture = RightWall();
    var entered = fixture.Controller.Step(Input(moveX: 1, grab: true), fixture.World);
    return new ClimbFixture(fixture, entered);
}

static IEnumerable<Type?> MemberTypes(MemberInfo member) => member switch
{
    MethodInfo method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType),
    PropertyInfo property => [property.PropertyType],
    FieldInfo field => [field.FieldType],
    _ => []
};

static void HasEvent(PlayerTraversalSnapshot snapshot, PlayerTraversalEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing event {kind}.");

static void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }
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
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed record TraversalFixture(
    SimulationWorld World,
    Actor Actor,
    PlayerTraversalController Controller);

internal sealed class ClimbFixture(TraversalFixture fixture, PlayerTraversalSnapshot last)
{
    public TraversalFixture Fixture { get; } = fixture;
    public PlayerTraversalSnapshot Last { get; set; } = last;
}
