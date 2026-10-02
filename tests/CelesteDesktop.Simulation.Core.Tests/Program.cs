using System.Collections;
using System.Reflection;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("fixed step is exactly 60 Hz", FixedStepIsSixtyHertz),
    ("rectangle edges are non-colliding", RectangleEdgesDoNotCollide),
    ("positive subpixels accumulate deterministically", PositiveSubpixelsAccumulate),
    ("negative subpixels accumulate deterministically", NegativeSubpixelsAccumulate),
    ("positive midpoint stays stable at zero displacement", PositiveMidpointDoesNotDrift),
    ("negative midpoint stays stable at zero displacement", NegativeMidpointDoesNotDrift),
    ("exact horizontal correction preserves subpixel remainder", ExactHorizontalCorrectionPreservesRemainder),
    ("movement outside a step is rejected", MovementOutsideStepIsRejected),
    ("tick increments exactly once", TickIncrementsExactlyOnce),
    ("horizontal collision stops at the first pixel", HorizontalCollisionStops),
    ("vertical collision stops at the first pixel", VerticalCollisionStops),
    ("one-way platform permits upward passage", OneWayPlatformPermitsUpwardPassage),
    ("one-way platform blocks downward crossing", OneWayPlatformBlocksDownwardCrossing),
    ("ignored one-way platform permits downward passage", IgnoredOneWayPlatformPermitsDownwardPassage),
    ("one-way platform never blocks horizontal motion", OneWayPlatformDoesNotBlockHorizontalMotion),
    ("one-way platform participates in grounded query", OneWayPlatformGroundingIsExplicit),
    ("ordinary solid remains authoritative above one-way platform", OrdinarySolidRemainsAuthoritative),
    ("first registered one-way platform wins", FirstRegisteredOneWayPlatformWins),
    ("one-way platform snapshots are immutable", OneWayPlatformSnapshotsAreImmutable),
    ("first registered solid wins collision ordering", FirstRegisteredSolidWins),
    ("collision clears blocked-axis remainder", CollisionClearsRemainder),
    ("actors do not collide with each other", ActorsDoNotCollide),
    ("duplicate IDs are rejected transactionally", DuplicateIdsAreRejected),
    ("initial actor-solid overlap is rejected", InitialOverlapIsRejected),
    ("membership mutation during a step is rejected", MembershipMutationIsRejected),
    ("nested steps are rejected", NestedStepsAreRejected),
    ("offset query returns first registered solid", OffsetQueryReturnsFirstSolid),
    ("offset query returns null without collision", OffsetQueryReturnsNull),
    ("offset query rejects unregistered actor", OffsetQueryRejectsUnregisteredActor),
    ("snapshots are immutable value captures", SnapshotsAreImmutableCaptures),
    ("event collections are read-only", EventCollectionsAreReadOnly),
    ("horizontal rider carry is exact", HorizontalRiderCarryIsExact),
    ("downward rider carry is exact", DownwardRiderCarryIsExact),
    ("upward solid pushes its rider", UpwardSolidPushesRider),
    ("side overlap pushes an actor", SideOverlapPushesActor),
    ("non-riders remain still", NonRidersRemainStill),
    ("solid subpixels accumulate", SolidSubpixelsAccumulate),
    ("lift speed is per-second and tick-scoped", LiftSpeedIsTickScoped),
    ("blocked push emits an explicit squish", BlockedPushEmitsSquish),
    ("solid processes riders in registration order", RidersUseRegistrationOrder),
    ("replay produces identical snapshots and events", ReplayIsDeterministic),
    ("public surface has no file GUI clock or input dependency", SurfaceIsPlatformIndependent)
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

static void FixedStepIsSixtyHertz()
{
    Equal(60, SimulationConstants.TicksPerSecond);
    Equal(1m / 60m, SimulationConstants.SecondsPerTick);
}

static void RectangleEdgesDoNotCollide()
{
    var left = new SimRect(0, 0, 2, 2);
    Assert(!left.Intersects(new SimRect(2, 0, 2, 2)), "Touching horizontal edges collided.");
    Assert(!left.Intersects(new SimRect(0, 2, 2, 2)), "Touching vertical edges collided.");
    Assert(left.Intersects(new SimRect(1, 1, 2, 2)), "Overlapping rectangles did not collide.");
}

static void OffsetQueryReturnsFirstSolid()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    var first = new Solid("first", 3, 0, 2, 2);
    var second = new Solid("second", 3, 0, 2, 2);
    world.Add(actor);
    world.Add(first);
    world.Add(second);
    Equal(first, world.FirstSolidAt(actor, 2, 0));
}

static void OffsetQueryReturnsNull()
{
    var (world, actor) = ActorWorld();
    Equal<Solid?>(null, world.FirstSolidAt(actor, 1, 0));
}

static void OffsetQueryRejectsUnregisteredActor()
{
    var world = new SimulationWorld();
    Throws<ArgumentException>(() => world.FirstSolidAt(new Actor("other", 0, 0, 2, 2), 1, 0));
}

static void PositiveSubpixelsAccumulate()
{
    var (world, actor) = ActorWorld();
    for (var tick = 0; tick < 5; tick++)
    {
        world.Step(_ => actor.MoveX(0.2m, world));
    }
    Equal(1, actor.X);
    Equal(0m, actor.XSubpixel);
}

static void PositiveMidpointDoesNotDrift()
{
    var (world, actor) = ActorWorld();
    world.Step(_ => actor.MoveX(0.5m, world));
    Equal(0, actor.X);
    Equal(0.5m, actor.XSubpixel);
    for (var tick = 0; tick < 4; tick++)
    {
        world.Step(_ => actor.MoveX(0m, world));
    }
    Equal(0, actor.X);
    Equal(0.5m, actor.XSubpixel);
}

static void NegativeMidpointDoesNotDrift()
{
    var (world, actor) = ActorWorld();
    world.Step(_ => actor.MoveY(-0.5m, world));
    Equal(0, actor.Y);
    Equal(-0.5m, actor.YSubpixel);
    for (var tick = 0; tick < 4; tick++)
    {
        world.Step(_ => actor.MoveY(0m, world));
    }
    Equal(0, actor.Y);
    Equal(-0.5m, actor.YSubpixel);
}

static void NegativeSubpixelsAccumulate()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 10, 0, 2, 2);
    world.Add(actor);
    for (var tick = 0; tick < 5; tick++)
    {
        world.Step(_ => actor.MoveX(-0.2m, world));
    }
    Equal(9, actor.X);
    Equal(0m, actor.XSubpixel);
}

static void ExactHorizontalCorrectionPreservesRemainder()
{
    var (world, actor) = ActorWorld();
    world.Step(_ => actor.MoveX(-0.5m, world));
    Equal(0, actor.X);
    Equal(-0.5m, actor.XSubpixel);

    world.Step(_ => actor.MoveXExact(1, world));
    Equal(1, actor.X);
    Equal(-0.5m, actor.XSubpixel);
}

static void MovementOutsideStepIsRejected()
{
    var (world, actor) = ActorWorld();
    Throws<InvalidOperationException>(() => actor.MoveX(1m, world));
    Throws<InvalidOperationException>(() => actor.MoveXExact(1, world));
    Throws<InvalidOperationException>(() => actor.MoveYWithOneWayPlatforms(1m, world));
}

static void TickIncrementsExactlyOnce()
{
    var world = new SimulationWorld();
    Equal(0L, world.Tick);
    Equal(1L, world.Step(_ => { }).Tick);
    Equal(2L, world.Step(_ => { }).Tick);
}

static void HorizontalCollisionStops()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new Solid("wall", 3, 0, 2, 2));
    world.Step(_ => actor.MoveX(5m, world));
    Equal(1, actor.X);
    Equal("wall", world.Events.Single().BlockingSolidId);
    Equal(SimulationEventKind.ActorBlocked, world.Events.Single().Kind);
}

static void VerticalCollisionStops()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new Solid("floor", 0, 4, 2, 2));
    world.Step(_ => actor.MoveY(8m, world));
    Equal(2, actor.Y);
    Equal(MovementAxis.Vertical, world.Events.Single().Axis);
}

static void OneWayPlatformPermitsUpwardPassage()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 8, 2, 2);
    world.Add(actor);
    world.Add(new OneWayPlatform("platform", -4, 5, 10, 2));
    ActorMoveResult? result = null;
    world.Step(_ => result = actor.MoveYWithOneWayPlatforms(-6m, world));
    Equal(2, actor.Y);
    Assert(result is { Blocked: false }, "Upward movement was blocked by a one-way platform.");
}

static void OneWayPlatformBlocksDownwardCrossing()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new OneWayPlatform("platform", -4, 4, 10, 2));
    ActorMoveResult? result = null;
    world.Step(_ => result = actor.MoveYWithOneWayPlatforms(8m, world));
    Equal(2, actor.Y);
    Equal("platform", result!.BlockingOneWayPlatformId);
    Equal<string?>(null, result.BlockingSolidId);
    Equal("platform", result.BlockingSurfaceId);
}

static void IgnoredOneWayPlatformPermitsDownwardPassage()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new OneWayPlatform("platform", -4, 4, 10, 2));
    ActorMoveResult? result = null;
    world.Step(_ => result = actor.MoveYWithOneWayPlatforms(8m, world, "platform"));
    Equal(8, actor.Y);
    Assert(result is { Blocked: false }, "The explicitly ignored one-way platform still blocked movement.");
}

static void OneWayPlatformDoesNotBlockHorizontalMotion()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 4, 2, 2);
    world.Add(actor);
    world.Add(new OneWayPlatform("platform", 3, 4, 2, 2));
    world.Step(_ => actor.MoveX(5m, world));
    Equal(5, actor.X);
}

static void OneWayPlatformGroundingIsExplicit()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 2, 2, 2);
    world.Add(actor);
    world.Add(new OneWayPlatform("platform", -4, 4, 10, 2));
    Assert(world.IsGrounded(actor), "Actor standing on one-way platform was not grounded.");
    Assert(!world.IsGrounded(actor, "platform"), "Ignored one-way platform still grounded the actor.");
    Equal("platform", world.FirstOneWayPlatformBelow(actor)?.Id);
}

static void OrdinarySolidRemainsAuthoritative()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new Solid("solid", -4, 3, 10, 1));
    world.Add(new OneWayPlatform("platform", -4, 4, 10, 2));
    ActorMoveResult? result = null;
    world.Step(_ => result = actor.MoveYWithOneWayPlatforms(8m, world, "platform"));
    Equal(1, actor.Y);
    Equal("solid", result!.BlockingSolidId);
    Equal<string?>(null, result.BlockingOneWayPlatformId);
}

static void FirstRegisteredOneWayPlatformWins()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new OneWayPlatform("first", -4, 4, 10, 2));
    world.Add(new OneWayPlatform("second", -4, 4, 10, 2));
    ActorMoveResult? result = null;
    world.Step(_ => result = actor.MoveYWithOneWayPlatforms(8m, world));
    Equal("first", result!.BlockingOneWayPlatformId);
}

static void OneWayPlatformSnapshotsAreImmutable()
{
    var world = new SimulationWorld();
    world.Add(new OneWayPlatform("platform", 1, 2, 3, 4));
    var snapshot = world.CaptureSnapshot();
    Equal(new OneWayPlatformSnapshot("platform", new SimPoint(1, 2), 3, 4), snapshot.OneWayPlatforms.Single());
    Assert(snapshot.OneWayPlatforms is IList list && list.IsReadOnly, "One-way platform snapshots were mutable.");
}

static void FirstRegisteredSolidWins()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 3);
    world.Add(actor);
    world.Add(new Solid("first", 3, 0, 2, 2));
    world.Add(new Solid("second", 3, 1, 2, 2));
    world.Step(_ => actor.MoveX(4m, world));
    Equal("first", world.Events.Single().BlockingSolidId);
}

static void CollisionClearsRemainder()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new Solid("wall", 2, 0, 2, 2));
    world.Step(_ => actor.MoveX(0.6m, world));
    Equal(0m, actor.XSubpixel);
}

static void ActorsDoNotCollide()
{
    var world = new SimulationWorld();
    var first = new Actor("first", 0, 0, 2, 2);
    var second = new Actor("second", 2, 0, 2, 2);
    world.Add(first);
    world.Add(second);
    world.Step(_ => first.MoveX(3m, world));
    Equal(3, first.X);
    Equal(2, second.X);
}

static void DuplicateIdsAreRejected()
{
    var world = new SimulationWorld();
    world.Add(new Actor("same", 0, 0, 1, 1));
    Throws<ArgumentException>(() => world.Add(new Solid("same", 10, 10, 1, 1)));
    world.Add(new Solid("usable", 10, 10, 1, 1));
}

static void InitialOverlapIsRejected()
{
    var world = new SimulationWorld();
    world.Add(new Actor("actor", 0, 0, 2, 2));
    Throws<ArgumentException>(() => world.Add(new Solid("solid", 1, 1, 2, 2)));
}

static void MembershipMutationIsRejected()
{
    var world = new SimulationWorld();
    Throws<InvalidOperationException>(() => world.Step(_ => world.Add(new Actor("actor", 0, 0, 1, 1))));
}

static void NestedStepsAreRejected()
{
    var world = new SimulationWorld();
    Throws<InvalidOperationException>(() => world.Step(_ => world.Step(__ => { })));
}

static void SnapshotsAreImmutableCaptures()
{
    var (world, actor) = ActorWorld();
    var before = world.CaptureSnapshot();
    world.Step(_ => actor.MoveX(3m, world));
    Equal(0, before.Actors[0].Position.X);
    Assert(before.Actors is IList list && list.IsReadOnly, "Snapshot actors are mutable.");
}

static void EventCollectionsAreReadOnly()
{
    var world = new SimulationWorld();
    Assert(world.Events is IList list && list.IsReadOnly, "Events are mutable.");
}

static void HorizontalRiderCarryIsExact()
{
    var (world, actor, platform) = RiderWorld();
    world.Step(_ => platform.Move(3m, 0m, world));
    Equal(3, actor.X);
    Equal(3, platform.X);
    Equal(SimulationEventKind.ActorCarried, world.Events.Single().Kind);
}

static void DownwardRiderCarryIsExact()
{
    var (world, actor, platform) = RiderWorld();
    world.Step(_ => platform.Move(0m, 2m, world));
    Equal(10, actor.Y);
    Equal(12, platform.Y);
    Equal(SimulationEventKind.ActorCarried, world.Events.Single().Kind);
}

static void UpwardSolidPushesRider()
{
    var (world, actor, platform) = RiderWorld();
    world.Step(_ => platform.Move(0m, -2m, world));
    Equal(6, actor.Y);
    Equal(8, platform.Y);
    Equal(SimulationEventKind.ActorPushed, world.Events.Single().Kind);
}

static void SideOverlapPushesActor()
{
    var world = new SimulationWorld();
    var platform = new Solid("platform", 0, 0, 2, 2);
    var actor = new Actor("actor", 3, 0, 2, 2);
    world.Add(platform);
    world.Add(actor);
    world.Step(_ => platform.Move(2m, 0m, world));
    Equal(4, actor.X);
    Equal(SimulationEventKind.ActorPushed, world.Events.Single().Kind);
}

static void NonRidersRemainStill()
{
    var world = new SimulationWorld();
    var platform = new Solid("platform", 0, 10, 4, 2);
    var actor = new Actor("actor", 10, 8, 2, 2);
    world.Add(platform);
    world.Add(actor);
    world.Step(_ => platform.Move(2m, 0m, world));
    Equal(10, actor.X);
    Equal(0, world.Events.Count);
}

static void SolidSubpixelsAccumulate()
{
    var world = new SimulationWorld();
    var platform = new Solid("platform", 0, 10, 4, 2);
    world.Add(platform);
    for (var tick = 0; tick < 4; tick++)
    {
        world.Step(_ => platform.Move(0.25m, 0m, world));
    }
    Equal(1, platform.X);
    Equal(0m, platform.XSubpixel);
}

static void LiftSpeedIsTickScoped()
{
    var (world, actor, platform) = RiderWorld();
    world.Step(_ => platform.Move(2m, -1m, world));
    Equal(new SimVector(120m, -60m), actor.LiftSpeed);
    world.Step(_ => { });
    Equal(SimVector.Zero, actor.LiftSpeed);
}

static void BlockedPushEmitsSquish()
{
    var world = new SimulationWorld();
    var mover = new Solid("mover", 0, 0, 2, 2);
    var actor = new Actor("actor", 3, 0, 2, 2);
    var wall = new Solid("wall", 5, 0, 2, 2);
    world.Add(mover);
    world.Add(actor);
    world.Add(wall);
    world.Step(_ => mover.Move(2m, 0m, world));
    Assert(actor.IsSquished, "Actor was not marked squished.");
    Equal(2, world.Events.Count);
    Equal(SimulationEventKind.ActorPushed, world.Events[0].Kind);
    Equal(SimulationEventKind.ActorSquished, world.Events[1].Kind);
    Equal("wall", world.Events[1].BlockingSolidId);
}

static void RidersUseRegistrationOrder()
{
    var world = new SimulationWorld();
    var platform = new Solid("platform", 0, 10, 8, 2);
    var first = new Actor("first", 0, 8, 2, 2);
    var second = new Actor("second", 4, 8, 2, 2);
    world.Add(platform);
    world.Add(first);
    world.Add(second);
    world.Step(_ => platform.Move(1m, 0m, world));
    Assert(world.Events.Select(item => item.ActorId).SequenceEqual(["first", "second"]), "Rider order changed.");
}

static void ReplayIsDeterministic()
{
    var first = RunReplay();
    var second = RunReplay();
    Equal(first, second);
}

static void SurfaceIsPlatformIndependent()
{
    var assembly = typeof(SimulationWorld).Assembly;
    var forbidden = new[] { "System.IO.", "System.Windows", "Microsoft.Win32", "System.Diagnostics", "System.Threading" };
    var exposedTypes = assembly.GetExportedTypes()
        .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .SelectMany(MemberTypes))
        .Where(type => type is not null)
        .Cast<Type>();
    Assert(!exposedTypes.Any(type => forbidden.Any(prefix => (type.FullName ?? string.Empty).StartsWith(prefix, StringComparison.Ordinal))),
        "Platform, file, clock, or input type leaked into the public surface.");
}

static IEnumerable<Type?> MemberTypes(MemberInfo member) => member switch
{
    MethodInfo method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType),
    PropertyInfo property => [property.PropertyType],
    FieldInfo field => [field.FieldType],
    _ => []
};

static string RunReplay()
{
    var (world, actor, platform) = RiderWorld();
    var lines = new List<string>();
    for (var tick = 0; tick < 6; tick++)
    {
        var snapshot = world.Step(_ =>
        {
            platform.Move(0.4m, tick % 2 == 0 ? -0.25m : 0.25m, world);
            actor.MoveX(0.35m, world);
        });
        lines.Add($"{snapshot.Tick}:{actor.X},{actor.Y}:{actor.XSubpixel},{actor.YSubpixel}:{platform.X},{platform.Y}:{string.Join(',', world.Events.Select(item => item.Kind))}");
    }
    return string.Join('|', lines);
}

static (SimulationWorld World, Actor Actor) ActorWorld()
{
    var world = new SimulationWorld();
    var actor = new Actor("actor", 0, 0, 2, 2);
    world.Add(actor);
    return (world, actor);
}

static (SimulationWorld World, Actor Actor, Solid Platform) RiderWorld()
{
    var world = new SimulationWorld();
    var platform = new Solid("platform", 0, 10, 4, 2);
    var actor = new Actor("actor", 0, 8, 2, 2);
    world.Add(platform);
    world.Add(actor);
    return (world, actor, platform);
}

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
