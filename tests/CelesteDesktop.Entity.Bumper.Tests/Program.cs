using System.Reflection;
using CelesteDesktop.Entity.Bumper;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial state is ready", InitialState),
    ("default input keeps Bumper ready", DefaultInput),
    ("snapshot exposes immutable identity and center", SnapshotIdentity),
    ("entity ID is required", EntityIdRequired),
    ("contact target ID is required", ContactIdRequired),
    ("disabled input rejects contact", DisabledContactRejected),
    ("contact radius must be positive", InvalidRadius),
    ("launch speed must be positive", InvalidLaunchSpeed),
    ("cooldown ticks must be positive", InvalidCooldown),
    ("right contact launches right", RightLaunch),
    ("left contact launches left", LeftLaunch),
    ("up contact launches up", UpLaunch),
    ("down contact launches down", DownLaunch),
    ("three four five contact normalizes exactly", DiagonalLaunch),
    ("five twelve thirteen contact normalizes exactly", SecondDiagonalLaunch),
    ("coincident center falls back upward", CenterFallback),
    ("coincident fallback event is explicit", CenterFallbackEvent),
    ("boundary contact activates", BoundaryContact),
    ("outside contact is ignored", OutsideIgnored),
    ("ineligible contact is ignored", IneligibleIgnored),
    ("activation enters cooldown", ActivationCooldown),
    ("activation preserves target identity", TargetIdentity),
    ("activation events preserve target identity", EventTargetIdentity),
    ("cooldown duration is exact", CooldownDuration),
    ("ready event is explicit", ReadyEvent),
    ("contact during cooldown is ignored", CooldownIgnores),
    ("sticky contact cannot retrigger", StickyContact),
    ("release rearms Bumper", ReleaseRearms),
    ("disabled transition is explicit", DisableTransition),
    ("disable clears cooldown", DisableClearsCooldown),
    ("repeated disabled input is quiet", RepeatedDisabledQuiet),
    ("enabled transition is explicit", EnableTransition),
    ("enable while touching requires release", EnableRequiresRelease),
    ("update requires active step", RequiresStep),
    ("controller updates once per tick", OncePerTick),
    ("snapshot events are immutable", EventsImmutable),
    ("event IDs are stable and unique", EventIds),
    ("same inputs replay deterministically", Replay),
    ("arbitrary direction replay is deterministic", ArbitraryReplay),
    ("two Bumpers remain isolated", TwoBumperIsolation),
    ("Player receives horizontal launch", PlayerHorizontalLaunch),
    ("Player receives diagonal launch", PlayerDiagonalLaunch),
    ("Player launch application is explicit", PlayerLaunchEvent),
    ("Player remains owner of Solid collision", PlayerSolidCollision),
    ("launch effect sets both velocity axes", BothAxesSet),
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
    var bumper = Create();
    Equal(BumperState.Ready, bumper.State);
    Equal(new SimPoint(10, 10), bumper.Center);
    Equal(4, bumper.Tuning.CooldownTicks);
}

static void DefaultInput()
{
    var snapshot = Create().Step(default, new SimulationWorld());
    Equal(BumperState.Ready, snapshot.State);
    Equal(0, snapshot.Events.Count);
}

static void SnapshotIdentity()
{
    var snapshot = Create(id: "bumper-7").Step(BumperInput.None, new SimulationWorld());
    Equal("bumper-7", snapshot.EntityId);
    Equal(new SimPoint(10, 10), snapshot.Center);
    Equal(1L, snapshot.Tick);
}

static void EntityIdRequired() =>
    Throws<ArgumentException>(() => new BumperController(" ", new SimPoint(0, 0)));

static void ContactIdRequired() =>
    Throws<ArgumentException>(() => new BumperContact("", new SimPoint(0, 0)));

static void DisabledContactRejected() =>
    Throws<ArgumentException>(() => new BumperInput(Contact(), disableRequested: true));

static void InvalidRadius() =>
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { ContactRadius = 0 }));

static void InvalidLaunchSpeed() =>
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { LaunchSpeed = 0m }));

static void InvalidCooldown() =>
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { CooldownTicks = 0 }));

static void RightLaunch() => AssertLaunch(new SimPoint(13, 10), 1m, 0m, 280m, 0m);
static void LeftLaunch() => AssertLaunch(new SimPoint(7, 10), -1m, 0m, -280m, 0m);
static void UpLaunch() => AssertLaunch(new SimPoint(10, 7), 0m, -1m, 0m, -280m);
static void DownLaunch() => AssertLaunch(new SimPoint(10, 13), 0m, 1m, 0m, 280m);
static void DiagonalLaunch() => AssertLaunch(new SimPoint(13, 14), 0.6m, 0.8m, 168m, 224m);
static void SecondDiagonalLaunch()
{
    var bumper = Create(tuning: TestTuning() with { ContactRadius = 13 });
    var snapshot = bumper.Step(new BumperInput(Contact(new SimPoint(15, 22))), new SimulationWorld());
    Equal(5m / 13m, snapshot.LaunchEffect!.Direction.X);
    Equal(12m / 13m, snapshot.LaunchEffect.Direction.Y);
    Equal<decimal?>(1400m / 13m, snapshot.LaunchEffect.Velocity.SpeedX);
    Near(3360m / 13m, snapshot.LaunchEffect.Velocity.SpeedY!.Value, 0.00000000000000000000000001m);
}

static void CenterFallback()
{
    var snapshot = Create().Step(new BumperInput(Contact(new SimPoint(10, 10))), new SimulationWorld());
    Equal(new SimVector(0m, -1m), snapshot.LaunchEffect!.Direction);
    Equal<decimal?>(0m, snapshot.LaunchEffect.Velocity.SpeedX);
    Equal<decimal?>(-280m, snapshot.LaunchEffect.Velocity.SpeedY);
}

static void CenterFallbackEvent()
{
    var snapshot = Create().Step(new BumperInput(Contact(new SimPoint(10, 10))), new SimulationWorld());
    Has(snapshot, BumperEventKind.CenterFallbackUsed);
}

static void BoundaryContact()
{
    var snapshot = Create().Step(new BumperInput(Contact(new SimPoint(22, 10))), new SimulationWorld());
    Assert(snapshot.LaunchEffect is not null, "Contact on radius boundary did not activate.");
}

static void OutsideIgnored()
{
    var snapshot = Create().Step(new BumperInput(Contact(new SimPoint(23, 10))), new SimulationWorld());
    Assert(snapshot.LaunchEffect is null, "Out-of-range contact launched.");
    Has(snapshot, BumperEventKind.ContactIgnored);
}

static void IneligibleIgnored()
{
    var snapshot = Create().Step(new BumperInput(Contact(canActivate: false)), new SimulationWorld());
    Equal(BumperState.Ready, snapshot.State);
    Assert(snapshot.LaunchEffect is null, "Ineligible contact launched.");
    Has(snapshot, BumperEventKind.ContactIgnored);
}

static void ActivationCooldown()
{
    var snapshot = Create().Step(new BumperInput(Contact()), new SimulationWorld());
    Equal(BumperState.Cooldown, snapshot.State);
    Equal(4, snapshot.CooldownTicksRemaining);
    Assert(!snapshot.Armed, "Activated Bumper remained armed.");
    Has(snapshot, BumperEventKind.Activated);
    Has(snapshot, BumperEventKind.LaunchIssued);
    Has(snapshot, BumperEventKind.CooldownStarted);
}

static void TargetIdentity()
{
    var snapshot = Create().Step(new BumperInput(Contact(id: "player-7")), new SimulationWorld());
    Equal("player-7", snapshot.LaunchEffect!.TargetId);
}

static void EventTargetIdentity()
{
    var snapshot = Create().Step(new BumperInput(Contact(id: "player-7")), new SimulationWorld());
    Assert(snapshot.Events.All(item => item.TargetId == "player-7"), "Activation events lost target identity.");
}

static void CooldownDuration()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(new BumperInput(Contact()), world);
    for (var expected = 3; expected >= 1; expected--)
    {
        var snapshot = bumper.Step(BumperInput.None, world);
        Equal(BumperState.Cooldown, snapshot.State);
        Equal(expected, snapshot.CooldownTicksRemaining);
    }
    Equal(BumperState.Ready, bumper.Step(BumperInput.None, world).State);
}

static void ReadyEvent()
{
    var (world, bumper) = EnterCooldown();
    BumperSnapshot? snapshot = null;
    for (var i = 0; i < 4; i++) snapshot = bumper.Step(BumperInput.None, world);
    Has(snapshot!, BumperEventKind.Ready);
}

static void CooldownIgnores()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(new BumperInput(Contact()), world);
    var snapshot = bumper.Step(new BumperInput(Contact()), world);
    Assert(snapshot.LaunchEffect is null, "Cooldown contact launched again.");
    Has(snapshot, BumperEventKind.ContactIgnored);
}

static void StickyContact()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(new BumperInput(Contact()), world);
    BumperSnapshot? snapshot = null;
    for (var i = 0; i < 5; i++) snapshot = bumper.Step(new BumperInput(Contact()), world);
    Equal(BumperState.Ready, snapshot!.State);
    Assert(snapshot.LaunchEffect is null, "Sticky contact retriggered.");
}

static void ReleaseRearms()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(new BumperInput(Contact()), world);
    for (var i = 0; i < 4; i++) _ = bumper.Step(new BumperInput(Contact()), world);
    _ = bumper.Step(BumperInput.None, world);
    Assert(bumper.Step(new BumperInput(Contact()), world).LaunchEffect is not null, "Release did not rearm Bumper.");
}

static void DisableTransition()
{
    var snapshot = Create().Step(BumperInput.Disabled, new SimulationWorld());
    Equal(BumperState.Disabled, snapshot.State);
    Has(snapshot, BumperEventKind.Disabled);
}

static void DisableClearsCooldown()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(new BumperInput(Contact()), world);
    var snapshot = bumper.Step(BumperInput.Disabled, world);
    Equal(0, snapshot.CooldownTicksRemaining);
    Assert(!snapshot.Armed, "Disabled Bumper remained armed.");
}

static void RepeatedDisabledQuiet()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(BumperInput.Disabled, world);
    Equal(0, bumper.Step(BumperInput.Disabled, world).Events.Count);
}

static void EnableTransition()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(BumperInput.Disabled, world);
    var snapshot = bumper.Step(BumperInput.None, world);
    Equal(BumperState.Ready, snapshot.State);
    Has(snapshot, BumperEventKind.Enabled);
}

static void EnableRequiresRelease()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(BumperInput.Disabled, world);
    var enabled = bumper.Step(new BumperInput(Contact()), world);
    Assert(enabled.LaunchEffect is null, "Enable tick launched touching target.");
    _ = bumper.Step(BumperInput.None, world);
    Assert(bumper.Step(new BumperInput(Contact()), world).LaunchEffect is not null, "Release did not rearm enabled Bumper.");
}

static void RequiresStep() =>
    Throws<InvalidOperationException>(() => Create().Update(BumperInput.None, new SimulationWorld()));

static void OncePerTick()
{
    var world = new SimulationWorld();
    var bumper = Create();
    Throws<InvalidOperationException>(() => world.Step(current =>
    {
        _ = bumper.Update(BumperInput.None, current);
        _ = bumper.Update(BumperInput.None, current);
    }));
}

static void EventsImmutable()
{
    var snapshot = Create().Step(new BumperInput(Contact()), new SimulationWorld());
    Throws<NotSupportedException>(() => ((ICollection<BumperEvent>)snapshot.Events).Add(snapshot.Events[0]));
}

static void EventIds()
{
    var ids = Enum.GetValues<BumperEventKind>().Select(BumperEventIds.For).ToArray();
    Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    Assert(ids.All(id => id.StartsWith("BUMPER_", StringComparison.Ordinal)), "Bumper event prefix changed.");
}

static void Replay() => Equal(RunReplay(false), RunReplay(false));
static void ArbitraryReplay() => Equal(RunReplay(true), RunReplay(true));

static void TwoBumperIsolation()
{
    var world = new SimulationWorld();
    var first = Create(id: "bumper-a");
    var second = new BumperController("bumper-b", new SimPoint(100, 100), TestTuning());
    BumperSnapshot? a = null;
    BumperSnapshot? b = null;
    world.Step(current =>
    {
        a = first.Update(new BumperInput(Contact()), current);
        b = second.Update(BumperInput.None, current);
    });
    Assert(a!.LaunchEffect is not null, "First Bumper did not launch.");
    Equal(BumperState.Ready, b!.State);
    Equal(0, b.Events.Count);
}

static void PlayerHorizontalLaunch()
{
    var snapshot = ApplyToPlayer(new SimPoint(13, 10));
    Equal<decimal>(280m, snapshot.Speed.X);
    Equal<decimal>(0m, snapshot.Speed.Y);
}

static void PlayerDiagonalLaunch()
{
    var snapshot = ApplyToPlayer(new SimPoint(13, 14));
    Equal<decimal>(168m, snapshot.Speed.X);
    Equal<decimal>(224m, snapshot.Speed.Y);
}

static void PlayerLaunchEvent()
{
    var snapshot = ApplyToPlayer(new SimPoint(13, 10));
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied), "Player application event missing.");
}

static void PlayerSolidCollision()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new Solid("wall", 3, 0, 2, 10));
    var player = new PlayerNormalController(actor);
    var bumper = Create();
    PlayerNormalSnapshot? launched = null;
    world.Step(current =>
    {
        var effect = bumper.Update(new BumperInput(Contact()), current).LaunchEffect!.Velocity;
        launched = player.Update(new PlayerInput(0, 0, false, false), new PlayerExternalEffects(null, effect), current);
    });
    Equal<decimal>(0m, launched!.Speed.X);
    Equal(1, actor.X);
    Assert(launched.Events.Any(item => item.Kind == PlayerNormalEventKind.HorizontalBlocked), "Player-owned Solid collision event missing.");
}

static void BothAxesSet()
{
    var effect = Create().Step(new BumperInput(Contact(new SimPoint(13, 14))), new SimulationWorld()).LaunchEffect!.Velocity;
    Assert(effect.SpeedX is not null && effect.SpeedY is not null, "Radial launch did not set both axes.");
}

static void SurfaceIsolation()
{
    var text = string.Join(' ', typeof(BumperController).Assembly.GetExportedTypes().SelectMany(type =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Window", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Rendering", "CelesteDesktop.Desktop", "CelesteDesktop.Player", "CelesteDesktop.Entity.Spring", "CelesteDesktop.Entity.Water" })
    {
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
    }
}

static PlayerNormalSnapshot ApplyToPlayer(SimPoint contactCenter)
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 1, 1);
    world.Add(actor);
    var player = new PlayerNormalController(actor);
    var bumper = Create();
    PlayerNormalSnapshot? snapshot = null;
    world.Step(current =>
    {
        var effect = bumper.Update(new BumperInput(Contact(contactCenter)), current).LaunchEffect!.Velocity;
        snapshot = player.Update(new PlayerInput(0, 0, false, false), new PlayerExternalEffects(null, effect), current);
    });
    return snapshot!;
}

static (SimulationWorld World, BumperController Bumper) EnterCooldown()
{
    var world = new SimulationWorld();
    var bumper = Create();
    _ = bumper.Step(new BumperInput(Contact()), world);
    return (world, bumper);
}

static string RunReplay(bool arbitrary)
{
    var world = new SimulationWorld();
    var bumper = Create();
    var rows = new List<string>();
    for (var tick = 0; tick < 14; tick++)
    {
        BumperInput input;
        if (tick is 0 or 7)
        {
            input = new BumperInput(Contact(arbitrary ? new SimPoint(12, 17) : new SimPoint(13, 10)));
        }
        else
        {
            input = BumperInput.None;
        }
        rows.Add(Format(bumper.Step(input, world)));
    }
    return string.Join('|', rows);
}

static void AssertLaunch(SimPoint target, decimal directionX, decimal directionY, decimal speedX, decimal speedY)
{
    var snapshot = Create().Step(new BumperInput(Contact(target)), new SimulationWorld());
    Equal(directionX, snapshot.LaunchEffect!.Direction.X);
    Equal(directionY, snapshot.LaunchEffect.Direction.Y);
    Equal<decimal?>(speedX, snapshot.LaunchEffect.Velocity.SpeedX);
    Equal<decimal?>(speedY, snapshot.LaunchEffect.Velocity.SpeedY);
}

static BumperController Create(string id = "bumper", BumperTuning? tuning = null) =>
    new(id, new SimPoint(10, 10), tuning ?? TestTuning());

static BumperTuning TestTuning() => new(12, 280m, 4);

static BumperContact Contact(SimPoint? center = null, string id = "player", bool canActivate = true) =>
    new(id, center ?? new SimPoint(13, 10), canActivate);

static string Format(BumperSnapshot snapshot) =>
    $"{snapshot.Tick}:{snapshot.State}:{snapshot.CooldownTicksRemaining}:{snapshot.Armed}:{snapshot.LaunchEffect?.TargetId}:{snapshot.LaunchEffect?.Direction}:{string.Join(',', snapshot.Events.Select(item => item.EventId))}";

static void Has(BumperSnapshot snapshot, BumperEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing Bumper event {kind}.");

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

static void Near(decimal expected, decimal actual, decimal tolerance)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"Expected {expected} +/- {tolerance}, got {actual}.");
    }
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
