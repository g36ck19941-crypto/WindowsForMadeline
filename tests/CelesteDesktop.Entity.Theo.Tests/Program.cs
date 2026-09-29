using System.Reflection;
using CelesteDesktop.Entity.Theo;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial state is free", InitialStateIsFree),
    ("holder rejects invalid facing", HolderRejectsInvalidFacing),
    ("actions require holder snapshots", ActionsRequireHolder),
    ("pickup transitions to held", PickupTransitionsToHeld),
    ("carry follows holder exactly", CarryFollowsHolder),
    ("carry clears prior subpixel remainder", CarryClearsSubpixelRemainder),
    ("held state requires holder every tick", HeldRequiresHolder),
    ("held state rejects different holder", HeldRejectsDifferentHolder),
    ("duplicate pickup is rejected", DuplicatePickupRejected),
    ("hold obstruction is explicit", HoldObstructionIsExplicit),
    ("drop inherits holder lift speed", DropInheritsLiftSpeed),
    ("throw uses holder facing", ThrowUsesFacing),
    ("throw adds holder lift speed", ThrowAddsLiftSpeed),
    ("free crystal rejects drop", FreeRejectsDrop),
    ("free crystal rejects throw", FreeRejectsThrow),
    ("free crystal rejects stray holder", FreeRejectsStrayHolder),
    ("gravity reaches terminal speed", GravityReachesTerminal),
    ("ground prevents gravity", GroundPreventsGravity),
    ("horizontal friction approaches zero", HorizontalFrictionApproachesZero),
    ("horizontal collision bounces", HorizontalCollisionBounces),
    ("fast landing bounces", FastLandingBounces),
    ("slow landing stops", SlowLandingStops),
    ("ceiling collision stops upward speed", CeilingCollisionStops),
    ("update requires active step", UpdateRequiresActiveStep),
    ("controller updates once per tick", UpdatesOncePerTick),
    ("step wrapper produces snapshot", StepWrapperProducesSnapshot),
    ("snapshots expose immutable events", SnapshotEventsAreImmutable),
    ("event IDs are stable and unique", EventIdsAreUnique),
    ("moving solid carries Theo", MovingSolidCarriesTheo),
    ("moving-solid lift is observable", MovingSolidLiftIsObservable),
    ("squish disables only affected Theo", SquishIsIsolated),
    ("squish transition emits once", SquishEmitsOnce),
    ("two Theo controllers remain isolated", TwoTheoRemainIsolated),
    ("player pickup carry throw matrix", PlayerInteractionMatrix),
    ("same inputs replay deterministically", ReplayIsDeterministic),
    ("invalid tuning is rejected", InvalidTuningRejected),
    ("public surface has no file GUI rendering desktop or player dependency", PublicSurfaceIsIsolated)
};

var failed = 0;
foreach (var test in tests)
{
    try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception exception) { failed++; Console.Error.WriteLine($"FAIL {test.Name}\n{exception}"); }
}
Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void InitialStateIsFree()
{
    var (_, _, theo) = CreateTheo();
    Equal(TheoCrystalState.Free, theo.State);
    Equal<decimal>(0m, theo.SpeedX);
    Equal<decimal>(0m, theo.SpeedY);
}

static void HolderRejectsInvalidFacing() =>
    Throws<ArgumentOutOfRangeException>(() => new TheoHolderSnapshot("player", new SimPoint(0, 0), 0));

static void ActionsRequireHolder()
{
    Throws<ArgumentException>(() => new TheoCrystalInput(TheoCrystalAction.Pickup));
    Throws<ArgumentException>(() => new TheoCrystalInput(TheoCrystalAction.Drop));
    Throws<ArgumentException>(() => new TheoCrystalInput(TheoCrystalAction.Throw));
}

static void PickupTransitionsToHeld()
{
    var (world, actor, theo) = CreateTheo();
    var snapshot = theo.Step(Input(TheoCrystalAction.Pickup, x: 4, y: 5), world);
    Equal(TheoCrystalState.Held, snapshot.State);
    Equal("player", snapshot.HolderId);
    Equal(new SimPoint(4, 5), snapshot.Position);
    Equal(new SimPoint(4, 5), new SimPoint(actor.X, actor.Y));
    Has(snapshot, TheoCrystalEventKind.PickedUp);
}

static void CarryFollowsHolder()
{
    var (world, _, theo) = CreateTheo();
    _ = theo.Step(Input(TheoCrystalAction.Pickup, x: 2, y: 3), world);
    var snapshot = theo.Step(Input(TheoCrystalAction.None, x: 7, y: -2), world);
    Equal(new SimPoint(7, -2), snapshot.Position);
    Has(snapshot, TheoCrystalEventKind.Carried);
}

static void CarryClearsSubpixelRemainder()
{
    var (world, actor, theo) = CreateTheo(initialSpeed: new SimVector(40m, 0m));
    _ = theo.Step(TheoCrystalInput.None, world);
    Assert(actor.XSubpixel != 0m, "Precondition did not create a subpixel remainder.");
    var snapshot = theo.Step(Input(TheoCrystalAction.Pickup, x: 8, y: 0), world);
    Equal(new SimPoint(8, 0), snapshot.Position);
    Equal<decimal>(0m, actor.XSubpixel);
}

static void HeldRequiresHolder()
{
    var (world, _, theo) = CreateTheo();
    _ = theo.Step(Input(TheoCrystalAction.Pickup), world);
    Throws<InvalidOperationException>(() => theo.Step(TheoCrystalInput.None, world));
}

static void HeldRejectsDifferentHolder()
{
    var (world, _, theo) = CreateTheo();
    _ = theo.Step(Input(TheoCrystalAction.Pickup), world);
    var other = new TheoCrystalInput(
        TheoCrystalAction.None,
        new TheoHolderSnapshot("other", new SimPoint(0, 0), 1));
    Throws<InvalidOperationException>(() => theo.Step(other, world));
}

static void DuplicatePickupRejected()
{
    var (world, _, theo) = CreateTheo();
    _ = theo.Step(Input(TheoCrystalAction.Pickup), world);
    Throws<InvalidOperationException>(() => theo.Step(Input(TheoCrystalAction.Pickup), world));
}

static void HoldObstructionIsExplicit()
{
    var (world, _, theo) = CreateTheo(width: 1, height: 1);
    world.Add(new Solid("wall", 2, -2, 1, 6));
    var snapshot = theo.Step(Input(TheoCrystalAction.Pickup, x: 4, y: 0), world);
    Equal(1, snapshot.Position.X);
    Has(snapshot, TheoCrystalEventKind.HoldBlocked);
    Equal("wall", snapshot.Events.Single(x => x.Kind == TheoCrystalEventKind.HoldBlocked).SolidId);
}

static void DropInheritsLiftSpeed()
{
    var (world, _, theo) = CreateTheo();
    _ = theo.Step(Input(TheoCrystalAction.Pickup), world);
    var snapshot = theo.Step(Input(TheoCrystalAction.Drop, lift: new SimVector(30m, -20m)), world);
    Equal(TheoCrystalState.Free, snapshot.State);
    Equal(new SimVector(30m, -20m), snapshot.Speed);
    Has(snapshot, TheoCrystalEventKind.LiftInherited);
    Has(snapshot, TheoCrystalEventKind.Dropped);
}

static void ThrowUsesFacing()
{
    var (world, _, theo) = CreateTheo();
    _ = theo.Step(Input(TheoCrystalAction.Pickup), world);
    var snapshot = theo.Step(Input(TheoCrystalAction.Throw, facing: -1), world);
    Equal(-theo.Tuning.ThrowSpeedX, snapshot.Speed.X);
    Equal(theo.Tuning.ThrowSpeedY, snapshot.Speed.Y);
    Has(snapshot, TheoCrystalEventKind.Thrown);
}

static void ThrowAddsLiftSpeed()
{
    var (world, _, theo) = CreateTheo();
    _ = theo.Step(Input(TheoCrystalAction.Pickup), world);
    var lift = new SimVector(20m, -10m);
    var snapshot = theo.Step(Input(TheoCrystalAction.Throw, lift: lift), world);
    Equal(lift.X + theo.Tuning.ThrowSpeedX, snapshot.Speed.X);
    Equal(lift.Y + theo.Tuning.ThrowSpeedY, snapshot.Speed.Y);
}

static void FreeRejectsDrop()
{
    var (world, _, theo) = CreateTheo();
    Throws<InvalidOperationException>(() => theo.Step(Input(TheoCrystalAction.Drop), world));
}

static void FreeRejectsThrow()
{
    var (world, _, theo) = CreateTheo();
    Throws<InvalidOperationException>(() => theo.Step(Input(TheoCrystalAction.Throw), world));
}

static void FreeRejectsStrayHolder()
{
    var (world, _, theo) = CreateTheo();
    Throws<InvalidOperationException>(() => theo.Step(Input(TheoCrystalAction.None), world));
}

static void GravityReachesTerminal()
{
    var (world, _, theo) = CreateTheo(y: -10000);
    TheoCrystalSnapshot? snapshot = null;
    for (var index = 0; index < 60; index++)
    {
        snapshot = theo.Step(TheoCrystalInput.None, world);
    }
    Equal(theo.Tuning.MaximumFallSpeed, snapshot!.Speed.Y);
}

static void GroundPreventsGravity()
{
    var (world, _, theo) = CreateTheo(width: 2, height: 2);
    world.Add(new Solid("floor", -10, 2, 20, 2));
    var snapshot = theo.Step(TheoCrystalInput.None, world);
    Equal<decimal>(0m, snapshot.Speed.Y);
    Assert(snapshot.Grounded, "Theo was not grounded.");
}

static void HorizontalFrictionApproachesZero()
{
    var (world, _, theo) = CreateTheo(initialSpeed: new SimVector(100m, 0m));
    world.Add(new Solid("floor", -100, 2, 200, 2));
    var snapshot = theo.Step(TheoCrystalInput.None, world);
    Assert(snapshot.Speed.X is > 0m and < 100m, "Horizontal friction did not approach zero.");
}

static void HorizontalCollisionBounces()
{
    var (world, _, theo) = CreateTheo(width: 2, height: 2, initialSpeed: new SimVector(120m, 0m));
    world.Add(new Solid("wall", 3, -10, 2, 20));
    var snapshot = theo.Step(TheoCrystalInput.None, world);
    Assert(snapshot.Speed.X < 0m, "Theo did not bounce away from the wall.");
    Has(snapshot, TheoCrystalEventKind.HorizontalBounced);
}

static void FastLandingBounces()
{
    var (world, _, theo) = CreateTheo(width: 1, height: 1, initialSpeed: new SimVector(0m, 120m));
    world.Add(new Solid("floor", -5, 2, 10, 2));
    var snapshot = theo.Step(TheoCrystalInput.None, world);
    Assert(snapshot.Speed.Y < 0m, "Fast landing did not bounce upward.");
    Has(snapshot, TheoCrystalEventKind.Bounced);
}

static void SlowLandingStops()
{
    var tuning = TheoCrystalTuning.PartialBaseline with { Gravity = 1m };
    var (world, _, theo) = CreateTheo(width: 1, height: 1, initialSpeed: new SimVector(0m, 39m), tuning: tuning);
    world.Add(new Solid("floor", -5, 2, 10, 2));
    var snapshot = theo.Step(TheoCrystalInput.None, world);
    Equal<decimal>(0m, snapshot.Speed.Y);
    Has(snapshot, TheoCrystalEventKind.Landed);
}

static void CeilingCollisionStops()
{
    var (world, _, theo) = CreateTheo(y: 2, width: 1, height: 1, initialSpeed: new SimVector(0m, -120m));
    world.Add(new Solid("ceiling", -5, 0, 10, 1));
    TheoCrystalSnapshot? snapshot = null;
    for (var index = 0; index < 3; index++)
    {
        snapshot = theo.Step(TheoCrystalInput.None, world);
        if (snapshot.Events.Any(x => x.Kind == TheoCrystalEventKind.VerticalBlocked))
        {
            break;
        }
    }
    Equal<decimal>(0m, snapshot!.Speed.Y);
    Has(snapshot, TheoCrystalEventKind.VerticalBlocked);
}

static void UpdateRequiresActiveStep()
{
    var (world, _, theo) = CreateTheo();
    Throws<InvalidOperationException>(() => theo.Update(TheoCrystalInput.None, world));
}

static void UpdatesOncePerTick()
{
    var (world, _, theo) = CreateTheo();
    Throws<InvalidOperationException>(() => world.Step(current =>
    {
        _ = theo.Update(TheoCrystalInput.None, current);
        _ = theo.Update(TheoCrystalInput.None, current);
    }));
}

static void StepWrapperProducesSnapshot()
{
    var (world, _, theo) = CreateTheo();
    Equal<long>(1, theo.Step(TheoCrystalInput.None, world).Tick);
}

static void SnapshotEventsAreImmutable()
{
    var (world, _, theo) = CreateTheo();
    var snapshot = theo.Step(Input(TheoCrystalAction.Pickup), world);
    Throws<NotSupportedException>(() => ((ICollection<TheoCrystalEvent>)snapshot.Events).Add(snapshot.Events[0]));
}

static void EventIdsAreUnique()
{
    var ids = Enum.GetValues<TheoCrystalEventKind>().Select(TheoCrystalEventIds.For).ToArray();
    Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    Assert(ids.All(id => id.StartsWith("THEO_", StringComparison.Ordinal)), "Theo event ID prefix is unstable.");
}

static void MovingSolidCarriesTheo()
{
    var (world, actor, theo) = CreateTheo(width: 2, height: 2);
    var platform = new Solid("platform", -5, 2, 20, 2);
    world.Add(platform);
    TheoCrystalSnapshot? snapshot = null;
    world.Step(current =>
    {
        platform.Move(1m, 0m, current);
        snapshot = theo.Update(TheoCrystalInput.None, current);
    });
    Equal(1, actor.X);
    Has(snapshot!, TheoCrystalEventKind.LiftCarried);
}

static void MovingSolidLiftIsObservable()
{
    var (world, _, theo) = CreateTheo(width: 2, height: 2);
    var platform = new Solid("platform", -5, 2, 20, 2);
    world.Add(platform);
    TheoCrystalSnapshot? snapshot = null;
    world.Step(current =>
    {
        platform.Move(1m, -1m, current);
        snapshot = theo.Update(TheoCrystalInput.None, current);
    });
    Equal(new SimVector(60m, -60m), snapshot!.LiftSpeed);
}

static void SquishIsIsolated()
{
    var world = new SimulationWorld();
    var crushedActor = new Actor("theo-a", 4, 0, 2, 2);
    var safeActor = new Actor("theo-b", 20, 0, 2, 2);
    var mover = new Solid("mover", 2, 0, 2, 2);
    var wall = new Solid("wall", 6, 0, 1, 2);
    world.Add(crushedActor); world.Add(safeActor); world.Add(mover); world.Add(wall);
    var crushed = new TheoCrystalController(crushedActor);
    var safe = new TheoCrystalController(safeActor);
    TheoCrystalSnapshot? crushedSnapshot = null;
    TheoCrystalSnapshot? safeSnapshot = null;
    world.Step(current =>
    {
        mover.Move(1m, 0m, current);
        crushedSnapshot = crushed.Update(TheoCrystalInput.None, current);
        safeSnapshot = safe.Update(TheoCrystalInput.None, current);
    });
    Equal(TheoCrystalState.Squished, crushedSnapshot!.State);
    Equal(TheoCrystalState.Free, safeSnapshot!.State);
    Assert(!safeActor.IsSquished, "Unrelated Theo was disabled.");
}

static void SquishEmitsOnce()
{
    var world = new SimulationWorld();
    var actor = new Actor("theo", 4, 0, 2, 2);
    var mover = new Solid("mover", 2, 0, 2, 2);
    var wall = new Solid("wall", 6, 0, 1, 2);
    world.Add(actor); world.Add(mover); world.Add(wall);
    var theo = new TheoCrystalController(actor);
    TheoCrystalSnapshot? first = null;
    world.Step(current => { mover.Move(1m, 0m, current); first = theo.Update(TheoCrystalInput.None, current); });
    Has(first!, TheoCrystalEventKind.Squished);
    var second = theo.Step(TheoCrystalInput.None, world);
    Assert(second.Events.All(x => x.Kind != TheoCrystalEventKind.Squished), "Squish transition repeated.");
}

static void TwoTheoRemainIsolated()
{
    var world = new SimulationWorld();
    var firstActor = new Actor("theo-a", 0, 0, 1, 1);
    var secondActor = new Actor("theo-b", 10, 0, 1, 1);
    world.Add(firstActor); world.Add(secondActor);
    var first = new TheoCrystalController(firstActor);
    var second = new TheoCrystalController(secondActor);
    TheoCrystalSnapshot? firstSnapshot = null;
    TheoCrystalSnapshot? secondSnapshot = null;
    world.Step(current =>
    {
        firstSnapshot = first.Update(Input(TheoCrystalAction.Pickup, x: 3, y: 0), current);
        secondSnapshot = second.Update(TheoCrystalInput.None, current);
    });
    Equal(TheoCrystalState.Held, firstSnapshot!.State);
    Equal(TheoCrystalState.Free, secondSnapshot!.State);
    Equal(new SimPoint(10, 0), secondSnapshot.Position);
}

static void PlayerInteractionMatrix()
{
    var world = new SimulationWorld();
    var playerActor = new Actor("player", 0, 0, 1, 2);
    var theoActor = new Actor("theo", 10, 0, 1, 2);
    world.Add(playerActor); world.Add(theoActor);
    world.Add(new Solid("floor", -20, 2, 80, 2));
    var player = new PlayerNormalController(playerActor);
    var theo = new TheoCrystalController(theoActor);

    TheoCrystalSnapshot? picked = null;
    world.Step(current =>
    {
        var playerSnapshot = player.Update(new PlayerInput(1, 0, false, false), current);
        var holder = new TheoHolderSnapshot(
            "player",
            new SimPoint(playerSnapshot.Position.X + 2, playerSnapshot.Position.Y),
            playerSnapshot.Facing,
            playerActor.LiftSpeed);
        picked = theo.Update(new TheoCrystalInput(TheoCrystalAction.Pickup, holder), current);
    });
    Equal(TheoCrystalState.Held, picked!.State);

    TheoCrystalSnapshot? carried = null;
    world.Step(current =>
    {
        var playerSnapshot = player.Update(new PlayerInput(1, 0, false, false), current);
        var holder = new TheoHolderSnapshot("player", new SimPoint(playerSnapshot.Position.X + 2, playerSnapshot.Position.Y), playerSnapshot.Facing);
        carried = theo.Update(new TheoCrystalInput(TheoCrystalAction.None, holder), current);
    });
    Equal(playerActor.X + 2, carried!.Position.X);

    TheoCrystalSnapshot? thrown = null;
    world.Step(current =>
    {
        var playerSnapshot = player.Update(new PlayerInput(0, 0, false, false), current);
        var holder = new TheoHolderSnapshot("player", new SimPoint(playerSnapshot.Position.X + 2, playerSnapshot.Position.Y), playerSnapshot.Facing);
        thrown = theo.Update(new TheoCrystalInput(TheoCrystalAction.Throw, holder), current);
    });
    Equal(TheoCrystalState.Free, thrown!.State);
    Assert(thrown.Speed.X > 0m, "Player-facing throw did not launch Theo right.");
}

static void ReplayIsDeterministic()
{
    static string Run()
    {
        var (world, _, theo) = CreateTheo(y: -10);
        var rows = new List<string>();
        rows.Add(Format(theo.Step(Input(TheoCrystalAction.Pickup, x: 2, y: -4), world)));
        rows.Add(Format(theo.Step(Input(TheoCrystalAction.None, x: 4, y: -3), world)));
        rows.Add(Format(theo.Step(Input(TheoCrystalAction.Throw, x: 4, y: -3), world)));
        for (var index = 0; index < 12; index++) rows.Add(Format(theo.Step(TheoCrystalInput.None, world)));
        return string.Join('|', rows);
    }

    Equal(Run(), Run());
}

static void InvalidTuningRejected()
{
    var bad = TheoCrystalTuning.PartialBaseline with { Gravity = 0m };
    Throws<ArgumentOutOfRangeException>(() => new TheoCrystalController(new Actor("theo", 0, 0, 1, 1), bad));
}

static void PublicSurfaceIsIsolated()
{
    var text = string.Join(' ', typeof(TheoCrystalController).Assembly.GetExportedTypes().SelectMany(type =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Window", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Rendering", "CelesteDesktop.Desktop", "CelesteDesktop.Player" })
    {
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
    }
}

static (SimulationWorld World, Actor Actor, TheoCrystalController Theo) CreateTheo(
    int x = 0,
    int y = 0,
    int width = 2,
    int height = 2,
    SimVector initialSpeed = default,
    TheoCrystalTuning? tuning = null)
{
    var world = new SimulationWorld();
    var actor = new Actor("theo", x, y, width, height);
    world.Add(actor);
    return (world, actor, new TheoCrystalController(actor, tuning, initialSpeed));
}

static TheoCrystalInput Input(
    TheoCrystalAction action,
    int x = 0,
    int y = 0,
    int facing = 1,
    SimVector lift = default) =>
    new(action, new TheoHolderSnapshot("player", new SimPoint(x, y), facing, lift));

static string Format(TheoCrystalSnapshot snapshot) =>
    $"{snapshot.Tick}:{snapshot.State}:{snapshot.Position.X},{snapshot.Position.Y}:{snapshot.Speed.X},{snapshot.Speed.Y}:{string.Join(',', snapshot.Events.Select(x => x.EventId))}";

static void Has(TheoCrystalSnapshot snapshot, TheoCrystalEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing event {kind}.");

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
