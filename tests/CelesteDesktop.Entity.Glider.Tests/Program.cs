using System.Reflection;
using CelesteDesktop.Entity.Glider;
using CelesteDesktop.Entity.Theo;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial state is free", InitialState),
    ("holder rejects invalid facing", InvalidFacing),
    ("actions require holder", ActionsRequireHolder),
    ("destroy cannot combine with action", DestroyActionRejected),
    ("pickup transitions to held and opens", PickupOpens),
    ("carry follows holder exactly", CarryFollows),
    ("carry clears subpixel remainder", CarryClearsRemainder),
    ("held state requires holder", HeldRequiresHolder),
    ("held state rejects different holder", DifferentHolderRejected),
    ("duplicate pickup is rejected", DuplicatePickupRejected),
    ("hold obstruction is explicit", HoldBlocked),
    ("holder fall cap is exposed", HolderFallCap),
    ("holder fall cap stays inactive below threshold", HolderFallCapInactive),
    ("drop inherits lift speed", DropInheritsLift),
    ("throw uses facing", ThrowUsesFacing),
    ("throw closes glider", ThrowCloses),
    ("free glider rejects drop", FreeRejectsDrop),
    ("free glider rejects throw", FreeRejectsThrow),
    ("free glider rejects stray holder", FreeRejectsHolder),
    ("gravity reaches slow terminal speed", GravityTerminal),
    ("falling glider opens", FallingOpens),
    ("grounded glider closes", GroundedCloses),
    ("horizontal friction approaches zero", Friction),
    ("horizontal collision bounces", HorizontalBounce),
    ("fast landing bounces", FastLandingBounce),
    ("slow landing stops", SlowLanding),
    ("ceiling collision stops upward speed", CeilingStops),
    ("moving solid carries glider", MovingSolidCarry),
    ("moving-solid lift is observable", MovingSolidLift),
    ("destroy transition is terminal and once", DestroyIsTerminal),
    ("squish disables only affected glider", SquishIsolation),
    ("squish transition emits once", SquishOnce),
    ("two gliders remain isolated", TwoGlidersIsolation),
    ("glider failure does not affect Theo", TheoIsolation),
    ("player pickup carry throw matrix", PlayerMatrix),
    ("update requires active step", RequiresStep),
    ("controller updates once per tick", OncePerTick),
    ("snapshot events are immutable", EventsImmutable),
    ("event IDs are stable and unique", EventIds),
    ("same inputs replay deterministically", Replay),
    ("invalid tuning is rejected", InvalidTuning),
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
    var (_, _, glider) = Create();
    Equal(GliderState.Free, glider.State);
    Equal<decimal>(0m, glider.SpeedX);
    Equal<decimal>(0m, glider.SpeedY);
}

static void InvalidFacing() => Throws<ArgumentOutOfRangeException>(() => new GliderHolderSnapshot("player", new SimPoint(0, 0), 0));

static void ActionsRequireHolder()
{
    Throws<ArgumentException>(() => new GliderInput(GliderAction.Pickup));
    Throws<ArgumentException>(() => new GliderInput(GliderAction.Drop));
    Throws<ArgumentException>(() => new GliderInput(GliderAction.Throw));
}

static void DestroyActionRejected() =>
    Throws<ArgumentException>(() => new GliderInput(GliderAction.Pickup, Holder(), destroyRequested: true));

static void PickupOpens()
{
    var (world, _, glider) = Create();
    var snapshot = glider.Step(Input(GliderAction.Pickup, 4, 5), world);
    Equal(GliderState.Held, snapshot.State);
    Equal(new SimPoint(4, 5), snapshot.Position);
    Assert(snapshot.IsOpen, "Pickup did not open Glider.");
    Has(snapshot, GliderEventKind.PickedUp);
    Has(snapshot, GliderEventKind.Opened);
}

static void CarryFollows()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    var snapshot = glider.Step(Input(GliderAction.None, 7, -3), world);
    Equal(new SimPoint(7, -3), snapshot.Position);
    Has(snapshot, GliderEventKind.Carried);
}

static void CarryClearsRemainder()
{
    var (world, actor, glider) = Create(initialSpeed: new SimVector(40m, 0m));
    _ = glider.Step(GliderInput.None, world);
    Assert(actor.XSubpixel != 0m, "Precondition did not create remainder.");
    var snapshot = glider.Step(Input(GliderAction.Pickup, 8, 0), world);
    Equal(new SimPoint(8, 0), snapshot.Position);
    Equal<decimal>(0m, actor.XSubpixel);
}

static void HeldRequiresHolder()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    Throws<InvalidOperationException>(() => glider.Step(GliderInput.None, world));
}

static void DifferentHolderRejected()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    Throws<InvalidOperationException>(() => glider.Step(new GliderInput(GliderAction.None, new GliderHolderSnapshot("other", new SimPoint(0, 0), 1)), world));
}

static void DuplicatePickupRejected()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    Throws<InvalidOperationException>(() => glider.Step(Input(GliderAction.Pickup), world));
}

static void HoldBlocked()
{
    var (world, _, glider) = Create(width: 1, height: 1);
    world.Add(new Solid("wall", 2, -2, 1, 6));
    var snapshot = glider.Step(Input(GliderAction.Pickup, 4, 0), world);
    Equal(1, snapshot.Position.X);
    Has(snapshot, GliderEventKind.HoldBlocked);
}

static void HolderFallCap()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    var snapshot = glider.Step(Input(GliderAction.None, holderVy: 100m), world);
    Assert(snapshot.HolderEffect is { LimitRequired: true }, "Fall limit was not requested.");
    Equal(glider.Tuning.MaximumHolderFallSpeed, snapshot.HolderEffect!.MaximumFallSpeed);
    Has(snapshot, GliderEventKind.HolderFallLimited);
}

static void HolderFallCapInactive()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    var snapshot = glider.Step(Input(GliderAction.None, holderVy: 10m), world);
    Assert(snapshot.HolderEffect is { LimitRequired: false }, "Fall limit was incorrectly requested.");
    Assert(snapshot.Events.All(x => x.Kind != GliderEventKind.HolderFallLimited), "Unexpected fall-limit event.");
}

static void DropInheritsLift()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    var lift = new SimVector(20m, -10m);
    var snapshot = glider.Step(Input(GliderAction.Drop, lift: lift), world);
    Equal(GliderState.Free, snapshot.State);
    Equal(lift, snapshot.Speed);
    Has(snapshot, GliderEventKind.Dropped);
    Has(snapshot, GliderEventKind.LiftInherited);
}

static void ThrowUsesFacing()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    var snapshot = glider.Step(Input(GliderAction.Throw, facing: -1), world);
    Equal(-glider.Tuning.ThrowSpeedX, snapshot.Speed.X);
    Equal(glider.Tuning.ThrowSpeedY, snapshot.Speed.Y);
    Has(snapshot, GliderEventKind.Thrown);
}

static void ThrowCloses()
{
    var (world, _, glider) = Create();
    _ = glider.Step(Input(GliderAction.Pickup), world);
    var snapshot = glider.Step(Input(GliderAction.Throw), world);
    Assert(!snapshot.IsOpen, "Thrown Glider did not close.");
    Has(snapshot, GliderEventKind.Closed);
}

static void FreeRejectsDrop()
{
    var (world, _, glider) = Create();
    Throws<InvalidOperationException>(() => glider.Step(Input(GliderAction.Drop), world));
}

static void FreeRejectsThrow()
{
    var (world, _, glider) = Create();
    Throws<InvalidOperationException>(() => glider.Step(Input(GliderAction.Throw), world));
}

static void FreeRejectsHolder()
{
    var (world, _, glider) = Create();
    Throws<InvalidOperationException>(() => glider.Step(Input(GliderAction.None), world));
}

static void GravityTerminal()
{
    var (world, _, glider) = Create(y: -1000);
    GliderSnapshot? snapshot = null;
    for (var i = 0; i < 20; i++) snapshot = glider.Step(GliderInput.None, world);
    Equal(glider.Tuning.MaximumFallSpeed, snapshot!.Speed.Y);
}

static void FallingOpens()
{
    var (world, _, glider) = Create(y: -20);
    var snapshot = glider.Step(GliderInput.None, world);
    Assert(snapshot.IsOpen, "Falling Glider stayed closed.");
    Has(snapshot, GliderEventKind.Opened);
}

static void GroundedCloses()
{
    var (world, _, glider) = Create(width: 2, height: 2);
    world.Add(new Solid("floor", -5, 2, 10, 2));
    var snapshot = glider.Step(GliderInput.None, world);
    Assert(snapshot.Grounded, "Glider was not grounded.");
    Assert(!snapshot.IsOpen, "Grounded Glider was open.");
}

static void Friction()
{
    var (world, _, glider) = Create(initialSpeed: new SimVector(100m, 0m));
    var snapshot = glider.Step(GliderInput.None, world);
    Assert(snapshot.Speed.X is > 0m and < 100m, "Friction did not approach zero.");
}

static void HorizontalBounce()
{
    var (world, _, glider) = Create(width: 2, height: 2, initialSpeed: new SimVector(120m, 0m));
    world.Add(new Solid("wall", 3, -10, 2, 20));
    var snapshot = glider.Step(GliderInput.None, world);
    Assert(snapshot.Speed.X < 0m, "Glider did not bounce.");
    Has(snapshot, GliderEventKind.HorizontalBounced);
}

static void FastLandingBounce()
{
    var tuning = GliderTuning.PartialBaseline with { Gravity = 1m, MaximumFallSpeed = 200m };
    var (world, _, glider) = Create(width: 1, height: 1, initialSpeed: new SimVector(0m, 120m), tuning: tuning);
    world.Add(new Solid("floor", -5, 2, 10, 2));
    var snapshot = glider.Step(GliderInput.None, world);
    Assert(snapshot.Speed.Y < 0m, "Fast landing did not bounce.");
    Has(snapshot, GliderEventKind.Bounced);
}

static void SlowLanding()
{
    var tuning = GliderTuning.PartialBaseline with { Gravity = 1m };
    var (world, _, glider) = Create(width: 1, height: 1, initialSpeed: new SimVector(0m, 10m), tuning: tuning);
    world.Add(new Solid("floor", -5, 2, 10, 2));
    GliderSnapshot? snapshot = null;
    for (var i = 0; i < 6; i++)
    {
        snapshot = glider.Step(GliderInput.None, world);
        if (snapshot.Events.Any(x => x.Kind == GliderEventKind.Landed)) break;
    }
    Equal<decimal>(0m, snapshot!.Speed.Y);
    Has(snapshot, GliderEventKind.Landed);
}

static void CeilingStops()
{
    var (world, _, glider) = Create(y: 2, width: 1, height: 1, initialSpeed: new SimVector(0m, -120m));
    world.Add(new Solid("ceiling", -5, 0, 10, 1));
    GliderSnapshot? snapshot = null;
    for (var i = 0; i < 3; i++)
    {
        snapshot = glider.Step(GliderInput.None, world);
        if (snapshot.Events.Any(x => x.Kind == GliderEventKind.VerticalBlocked)) break;
    }
    Equal<decimal>(0m, snapshot!.Speed.Y);
    Has(snapshot, GliderEventKind.VerticalBlocked);
}

static void MovingSolidCarry()
{
    var (world, actor, glider) = Create(width: 2, height: 2);
    var platform = new Solid("platform", -5, 2, 20, 2);
    world.Add(platform);
    GliderSnapshot? snapshot = null;
    world.Step(current => { platform.Move(1m, 0m, current); snapshot = glider.Update(GliderInput.None, current); });
    Equal(1, actor.X);
    Has(snapshot!, GliderEventKind.LiftCarried);
}

static void MovingSolidLift()
{
    var (world, _, glider) = Create(width: 2, height: 2);
    var platform = new Solid("platform", -5, 2, 20, 2);
    world.Add(platform);
    GliderSnapshot? snapshot = null;
    world.Step(current => { platform.Move(1m, -1m, current); snapshot = glider.Update(GliderInput.None, current); });
    Equal(new SimVector(60m, -60m), snapshot!.LiftSpeed);
}

static void DestroyIsTerminal()
{
    var (world, _, glider) = Create(initialSpeed: new SimVector(20m, 20m));
    var first = glider.Step(GliderInput.Destroy, world);
    Equal(GliderState.Destroyed, first.State);
    Equal(SimVector.Zero, first.Speed);
    Has(first, GliderEventKind.Destroyed);
    var second = glider.Step(GliderInput.None, world);
    Equal(GliderState.Destroyed, second.State);
    Equal(0, second.Events.Count);
}

static void SquishIsolation()
{
    var world = new SimulationWorld();
    var crushedActor = new Actor("glider-a", 4, 0, 2, 2);
    var safeActor = new Actor("glider-b", 20, 0, 2, 2);
    var mover = new Solid("mover", 2, 0, 2, 2);
    var wall = new Solid("wall", 6, 0, 1, 2);
    world.Add(crushedActor); world.Add(safeActor); world.Add(mover); world.Add(wall);
    var crushed = new GliderController(crushedActor);
    var safe = new GliderController(safeActor);
    GliderSnapshot? a = null; GliderSnapshot? b = null;
    world.Step(current => { mover.Move(1m, 0m, current); a = crushed.Update(GliderInput.None, current); b = safe.Update(GliderInput.None, current); });
    Equal(GliderState.Squished, a!.State);
    Equal(GliderState.Free, b!.State);
}

static void SquishOnce()
{
    var world = new SimulationWorld();
    var actor = new Actor("glider", 4, 0, 2, 2);
    var mover = new Solid("mover", 2, 0, 2, 2);
    var wall = new Solid("wall", 6, 0, 1, 2);
    world.Add(actor); world.Add(mover); world.Add(wall);
    var glider = new GliderController(actor);
    GliderSnapshot? first = null;
    world.Step(current => { mover.Move(1m, 0m, current); first = glider.Update(GliderInput.None, current); });
    Has(first!, GliderEventKind.Squished);
    Equal(0, glider.Step(GliderInput.None, world).Events.Count);
}

static void TwoGlidersIsolation()
{
    var world = new SimulationWorld();
    var aActor = new Actor("a", 0, 0, 1, 1); var bActor = new Actor("b", 10, 0, 1, 1);
    world.Add(aActor); world.Add(bActor);
    var a = new GliderController(aActor); var b = new GliderController(bActor);
    GliderSnapshot? sa = null; GliderSnapshot? sb = null;
    world.Step(current => { sa = a.Update(Input(GliderAction.Pickup, 3, 0), current); sb = b.Update(GliderInput.None, current); });
    Equal(GliderState.Held, sa!.State);
    Equal(GliderState.Free, sb!.State);
    Equal(new SimPoint(10, 0), sb.Position);
}

static void TheoIsolation()
{
    var world = new SimulationWorld();
    var gliderActor = new Actor("glider", 0, 0, 1, 1); var theoActor = new Actor("theo", 10, 0, 1, 1);
    world.Add(gliderActor); world.Add(theoActor);
    var glider = new GliderController(gliderActor); var theo = new TheoCrystalController(theoActor);
    TheoCrystalSnapshot? theoSnapshot = null;
    world.Step(current => { _ = glider.Update(GliderInput.Destroy, current); theoSnapshot = theo.Update(TheoCrystalInput.None, current); });
    Equal(GliderState.Destroyed, glider.State);
    Equal(TheoCrystalState.Free, theoSnapshot!.State);
}

static void PlayerMatrix()
{
    var world = new SimulationWorld();
    var playerActor = new Actor("player", 0, 0, 1, 2); var gliderActor = new Actor("glider", 10, 0, 1, 2);
    world.Add(playerActor); world.Add(gliderActor);
    var player = new PlayerNormalController(playerActor, initialSpeed: new SimVector(0m, 100m)); var glider = new GliderController(gliderActor);
    GliderSnapshot? snapshot = null;
    world.Step(current =>
    {
        var p = player.Update(new PlayerInput(1, 0, false, false), current);
        snapshot = glider.Update(new GliderInput(GliderAction.Pickup, new GliderHolderSnapshot("player", new SimPoint(p.Position.X + 2, p.Position.Y), p.Facing)), current);
    });
    Equal(GliderState.Held, snapshot!.State);
    world.Step(current =>
    {
        var p = player.Update(new PlayerInput(1, 0, false, false), current);
        snapshot = glider.Update(new GliderInput(GliderAction.None, new GliderHolderSnapshot("player", new SimPoint(p.Position.X + 2, p.Position.Y), p.Facing, holderVerticalSpeed: p.Speed.Y)), current);
    });
    Assert(snapshot!.HolderEffect is { LimitRequired: true }, "Player matrix missed holder effect.");
    PlayerNormalSnapshot? limitedPlayer = null;
    world.Step(current =>
    {
        var effects = new PlayerExternalEffects(snapshot!.HolderEffect!.MaximumFallSpeed);
        var p = player.Update(new PlayerInput(0, 0, false, false), effects, current);
        limitedPlayer = p;
        snapshot = glider.Update(new GliderInput(GliderAction.Throw, new GliderHolderSnapshot("player", new SimPoint(p.Position.X + 2, p.Position.Y), p.Facing)), current);
    });
    Equal(glider.Tuning.MaximumHolderFallSpeed, limitedPlayer!.Speed.Y);
    Assert(limitedPlayer.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalFallSpeedLimited), "Player did not apply the Glider fall limit.");
    Equal(GliderState.Free, snapshot!.State);
}

static void RequiresStep()
{
    var (world, _, glider) = Create();
    Throws<InvalidOperationException>(() => glider.Update(GliderInput.None, world));
}

static void OncePerTick()
{
    var (world, _, glider) = Create();
    Throws<InvalidOperationException>(() => world.Step(current => { _ = glider.Update(GliderInput.None, current); _ = glider.Update(GliderInput.None, current); }));
}

static void EventsImmutable()
{
    var (world, _, glider) = Create();
    var snapshot = glider.Step(Input(GliderAction.Pickup), world);
    Throws<NotSupportedException>(() => ((ICollection<GliderEvent>)snapshot.Events).Add(snapshot.Events[0]));
}

static void EventIds()
{
    var ids = Enum.GetValues<GliderEventKind>().Select(GliderEventIds.For).ToArray();
    Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    Assert(ids.All(x => x.StartsWith("GLIDER_", StringComparison.Ordinal)), "Event prefix changed.");
}

static void Replay()
{
    static string Run()
    {
        var (world, _, glider) = Create(y: -10);
        var rows = new List<string>
        {
            Format(glider.Step(Input(GliderAction.Pickup, 2, -4), world)),
            Format(glider.Step(Input(GliderAction.None, 4, -3, holderVy: 100m), world)),
            Format(glider.Step(Input(GliderAction.Throw, 4, -3), world))
        };
        for (var i = 0; i < 16; i++) rows.Add(Format(glider.Step(GliderInput.None, world)));
        return string.Join('|', rows);
    }
    Equal(Run(), Run());
}

static void InvalidTuning()
{
    var bad = GliderTuning.PartialBaseline with { MaximumHolderFallSpeed = 0m };
    Throws<ArgumentOutOfRangeException>(() => new GliderController(new Actor("glider", 0, 0, 1, 1), bad));
}

static void SurfaceIsolation()
{
    var text = string.Join(' ', typeof(GliderController).Assembly.GetExportedTypes().SelectMany(type =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Window", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Rendering", "CelesteDesktop.Desktop", "CelesteDesktop.Player", "CelesteDesktop.Entity.Theo" })
    {
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
    }
}

static (SimulationWorld World, Actor Actor, GliderController Glider) Create(
    int x = 0, int y = 0, int width = 2, int height = 2,
    SimVector initialSpeed = default, GliderTuning? tuning = null)
{
    var world = new SimulationWorld();
    var actor = new Actor("glider", x, y, width, height);
    world.Add(actor);
    return (world, actor, new GliderController(actor, tuning, initialSpeed));
}

static GliderHolderSnapshot Holder(int x = 0, int y = 0, int facing = 1, SimVector lift = default, decimal holderVy = 0m) =>
    new("player", new SimPoint(x, y), facing, lift, holderVy);

static GliderInput Input(GliderAction action, int x = 0, int y = 0, int facing = 1, SimVector lift = default, decimal holderVy = 0m) =>
    new(action, Holder(x, y, facing, lift, holderVy));

static string Format(GliderSnapshot snapshot) =>
    $"{snapshot.Tick}:{snapshot.State}:{snapshot.Position.X},{snapshot.Position.Y}:{snapshot.Speed.X},{snapshot.Speed.Y}:{snapshot.IsOpen}:{string.Join(',', snapshot.Events.Select(x => x.EventId))}";

static void Has(GliderSnapshot snapshot, GliderEventKind kind) =>
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
