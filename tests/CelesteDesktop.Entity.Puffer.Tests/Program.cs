using System.Reflection;
using CelesteDesktop.Entity.Puffer;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial state is swimming", InitialState),
    ("snapshot exposes immutable identity and spawn", SnapshotIdentity),
    ("entity ID is required", EntityIdRequired),
    ("swim bounds must have width", InvalidBoundsWidth),
    ("swim bounds must contain spawn", InvalidBoundsSpawn),
    ("initial direction is bounded", InvalidDirection),
    ("swim speed must be positive", InvalidSwimSpeed),
    ("trigger radius must be positive", InvalidTriggerRadius),
    ("warning ticks must be positive", InvalidWarningTicks),
    ("launch speed must be positive", InvalidLaunchSpeed),
    ("respawn ticks must be positive", InvalidRespawnTicks),
    ("contact target ID is required", ContactIdRequired),
    ("disabled input rejects contact", DisabledContactRejected),
    ("swim advances at fixed sixty hertz", SwimStep),
    ("swim event is explicit", SwimEvent),
    ("right bound turns left", TurnRight),
    ("left bound turns right", TurnLeft),
    ("in-range contact starts warning", WarningStarts),
    ("radius boundary contact starts warning", BoundaryStarts),
    ("outside contact is ignored", OutsideIgnored),
    ("ineligible contact is ignored", IneligibleIgnored),
    ("warning locks target identity", WarningTargetIdentity),
    ("warning freezes swim position", WarningFreezesSwim),
    ("warning duration is exact", WarningDuration),
    ("explosion lifecycle events are explicit", ExplosionEvents),
    ("right target launches right", RightLaunch),
    ("left target launches left", LeftLaunch),
    ("up target launches up", UpLaunch),
    ("down target launches down", DownLaunch),
    ("diagonal target normalizes exactly", DiagonalLaunch),
    ("coincident target falls back upward", CenterFallback),
    ("coincident fallback event is explicit", CenterFallbackEvent),
    ("latest locked target position controls launch", LatestTargetPosition),
    ("different target cannot replace lock", DifferentTargetIgnored),
    ("explosion enters spent state", SpentState),
    ("spent state freezes swim position", SpentFreezesSwim),
    ("respawn duration is exact", RespawnDuration),
    ("respawn resets spawn and direction", RespawnReset),
    ("sticky contact cannot retrigger after respawn", StickyContact),
    ("release rearms respawned Puffer", ReleaseRearms),
    ("spent contact is ignored", SpentContactIgnored),
    ("disabled transition is explicit", DisableTransition),
    ("disable cancels warning", DisableCancelsWarning),
    ("disable clears spent cooldown", DisableClearsSpent),
    ("repeated disabled input is quiet", RepeatedDisabledQuiet),
    ("enabled transition resets spawn", EnableTransition),
    ("enable while touching requires release", EnableRequiresRelease),
    ("update requires active step", RequiresStep),
    ("controller updates once per tick", OncePerTick),
    ("snapshot events are immutable", EventsImmutable),
    ("event IDs are stable and unique", EventIds),
    ("same inputs replay deterministically", Replay),
    ("two Puffers remain isolated", TwoPufferIsolation),
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
    var puffer = Create();
    Equal(PufferState.Swimming, puffer.State);
    Equal(new SimVector(0m, 0m), puffer.Center);
    Equal(1, puffer.SwimDirection);
}

static void SnapshotIdentity()
{
    var snapshot = Create("puffer-7").Step(PufferInput.None, new SimulationWorld());
    Equal("puffer-7", snapshot.EntityId);
    Equal(new SimVector(0m, 0m), snapshot.SpawnCenter);
    Equal(1L, snapshot.Tick);
}

static void EntityIdRequired() => Throws<ArgumentException>(() =>
    new PufferController(" ", SimVector.Zero, -2m, 2m));

static void InvalidBoundsWidth() => Throws<ArgumentOutOfRangeException>(() =>
    new PufferController("p", SimVector.Zero, 0m, 0m));

static void InvalidBoundsSpawn() => Throws<ArgumentOutOfRangeException>(() =>
    new PufferController("p", new SimVector(3m, 0m), -2m, 2m));

static void InvalidDirection() => Throws<ArgumentOutOfRangeException>(() =>
    new PufferController("p", SimVector.Zero, -2m, 2m, 0));

static void InvalidSwimSpeed() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { SwimSpeed = 0m }));
static void InvalidTriggerRadius() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { TriggerRadius = 0 }));
static void InvalidWarningTicks() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { WarningTicks = 0 }));
static void InvalidLaunchSpeed() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { LaunchSpeed = 0m }));
static void InvalidRespawnTicks() => Throws<ArgumentOutOfRangeException>(() => Create(tuning: TestTuning() with { RespawnTicks = 0 }));

static void ContactIdRequired() => Throws<ArgumentException>(() => new PufferContact("", SimVector.Zero));
static void DisabledContactRejected() => Throws<ArgumentException>(() => new PufferInput(Contact(), true));

static void SwimStep()
{
    var snapshot = Create().Step(PufferInput.None, new SimulationWorld());
    Equal(new SimVector(1m, 0m), snapshot.Center);
}

static void SwimEvent() => Has(Create().Step(PufferInput.None, new SimulationWorld()), PufferEventKind.Swam);

static void TurnRight()
{
    var snapshot = Create(tuning: TestTuning() with { SwimSpeed = 120m }).Step(PufferInput.None, new SimulationWorld());
    Equal(2m, snapshot.Center.X);
    Equal(-1, snapshot.SwimDirection);
    Has(snapshot, PufferEventKind.Turned);
}

static void TurnLeft()
{
    var snapshot = Create(direction: -1, tuning: TestTuning() with { SwimSpeed = 120m }).Step(PufferInput.None, new SimulationWorld());
    Equal(-2m, snapshot.Center.X);
    Equal(1, snapshot.SwimDirection);
    Has(snapshot, PufferEventKind.Turned);
}

static void WarningStarts()
{
    var snapshot = StartWarning();
    Equal(PufferState.Warning, snapshot.State);
    Equal(3, snapshot.WarningTicksRemaining);
    Equal("player", snapshot.LockedTargetId);
    Has(snapshot, PufferEventKind.WarningStarted);
}

static void BoundaryStarts()
{
    var puffer = Create(bounds: (-100m, 100m));
    var snapshot = puffer.Step(new PufferInput(Contact(new SimVector(11m, 0m))), new SimulationWorld());
    Equal(PufferState.Warning, snapshot.State);
}

static void OutsideIgnored()
{
    var snapshot = Create().Step(new PufferInput(Contact(new SimVector(20m, 0m))), new SimulationWorld());
    Equal(PufferState.Swimming, snapshot.State);
    Has(snapshot, PufferEventKind.ContactIgnored);
}

static void IneligibleIgnored()
{
    var snapshot = Create().Step(new PufferInput(Contact(canTrigger: false)), new SimulationWorld());
    Equal(PufferState.Swimming, snapshot.State);
    Has(snapshot, PufferEventKind.ContactIgnored);
}

static void WarningTargetIdentity() => Equal("player-7", StartWarning(id: "player-7").LockedTargetId);

static void WarningFreezesSwim()
{
    var (world, puffer) = WarningController();
    var before = puffer.Center;
    var snapshot = puffer.Step(PufferInput.None, world);
    Equal(before, snapshot.Center);
}

static void WarningDuration()
{
    var (world, puffer) = WarningController();
    Equal(2, puffer.Step(PufferInput.None, world).WarningTicksRemaining);
    Equal(1, puffer.Step(PufferInput.None, world).WarningTicksRemaining);
    Equal(PufferState.Spent, puffer.Step(PufferInput.None, world).State);
}

static void ExplosionEvents()
{
    var snapshot = Explode(Contact(new SimVector(6m, 0m)));
    Has(snapshot, PufferEventKind.Exploded);
    Has(snapshot, PufferEventKind.LaunchIssued);
    Has(snapshot, PufferEventKind.SpentStarted);
}

static void RightLaunch() => AssertLaunch(new SimVector(6m, 0m), 1m, 0m, 240m, 0m);
static void LeftLaunch() => AssertLaunch(new SimVector(-4m, 0m), -1m, 0m, -240m, 0m);
static void UpLaunch() => AssertLaunch(new SimVector(1m, -5m), 0m, -1m, 0m, -240m);
static void DownLaunch() => AssertLaunch(new SimVector(1m, 5m), 0m, 1m, 0m, 240m);
static void DiagonalLaunch() => AssertLaunch(new SimVector(4m, 4m), 0.6m, 0.8m, 144m, 192m);

static void CenterFallback()
{
    var snapshot = Explode(Contact(new SimVector(1m, 0m)));
    Equal(new SimVector(0m, -1m), snapshot.LaunchEffect!.Direction);
    Equal<decimal?>(0m, snapshot.LaunchEffect.Velocity.SpeedX);
    Equal<decimal?>(-240m, snapshot.LaunchEffect.Velocity.SpeedY);
}

static void CenterFallbackEvent() => Has(Explode(Contact(new SimVector(1m, 0m))), PufferEventKind.CenterFallbackUsed);

static void LatestTargetPosition()
{
    var (world, puffer) = WarningController(Contact(new SimVector(6m, 0m)));
    _ = puffer.Step(new PufferInput(Contact(new SimVector(1m, -5m))), world);
    _ = puffer.Step(PufferInput.None, world);
    var snapshot = puffer.Step(PufferInput.None, world);
    Equal(new SimVector(0m, -1m), snapshot.LaunchEffect!.Direction);
}

static void DifferentTargetIgnored()
{
    var (world, puffer) = WarningController(Contact(new SimVector(6m, 0m), "player-a"));
    _ = puffer.Step(new PufferInput(Contact(new SimVector(1m, -5m), "player-b")), world);
    _ = puffer.Step(PufferInput.None, world);
    var snapshot = puffer.Step(PufferInput.None, world);
    Equal("player-a", snapshot.LaunchEffect!.TargetId);
    Equal(new SimVector(1m, 0m), snapshot.LaunchEffect.Direction);
}

static void SpentState()
{
    var snapshot = Explode(Contact());
    Equal(PufferState.Spent, snapshot.State);
    Equal(4, snapshot.RespawnTicksRemaining);
}

static void SpentFreezesSwim()
{
    var (world, puffer) = SpentController();
    var before = puffer.Center;
    Equal(before, puffer.Step(PufferInput.None, world).Center);
}

static void RespawnDuration()
{
    var (world, puffer) = SpentController();
    for (var remaining = 3; remaining >= 1; remaining--)
    {
        var snapshot = puffer.Step(PufferInput.None, world);
        Equal(PufferState.Spent, snapshot.State);
        Equal(remaining, snapshot.RespawnTicksRemaining);
    }
    Equal(PufferState.Swimming, puffer.Step(PufferInput.None, world).State);
}

static void RespawnReset()
{
    var (world, puffer) = SpentController(direction: -1);
    PufferSnapshot? snapshot = null;
    for (var i = 0; i < 4; i++) snapshot = puffer.Step(PufferInput.None, world);
    Equal(SimVector.Zero, snapshot!.Center);
    Equal(-1, snapshot.SwimDirection);
    Has(snapshot, PufferEventKind.Respawned);
}

static void StickyContact()
{
    var (world, puffer) = SpentController();
    PufferSnapshot? snapshot = null;
    for (var i = 0; i < 5; i++) snapshot = puffer.Step(new PufferInput(Contact()), world);
    Equal(PufferState.Swimming, snapshot!.State);
    Assert(snapshot.LaunchEffect is null, "Sticky contact retriggered Puffer.");
}

static void ReleaseRearms()
{
    var (world, puffer) = SpentController();
    for (var i = 0; i < 4; i++) _ = puffer.Step(new PufferInput(Contact()), world);
    _ = puffer.Step(PufferInput.None, world);
    Equal(PufferState.Warning, puffer.Step(new PufferInput(Contact()), world).State);
}

static void SpentContactIgnored()
{
    var (world, puffer) = SpentController();
    Has(puffer.Step(new PufferInput(Contact()), world), PufferEventKind.ContactIgnored);
}

static void DisableTransition()
{
    var snapshot = Create().Step(PufferInput.Disabled, new SimulationWorld());
    Equal(PufferState.Disabled, snapshot.State);
    Has(snapshot, PufferEventKind.Disabled);
}

static void DisableCancelsWarning()
{
    var (world, puffer) = WarningController();
    var snapshot = puffer.Step(PufferInput.Disabled, world);
    Equal(0, snapshot.WarningTicksRemaining);
    Equal<string?>(null, snapshot.LockedTargetId);
}

static void DisableClearsSpent()
{
    var (world, puffer) = SpentController();
    Equal(0, puffer.Step(PufferInput.Disabled, world).RespawnTicksRemaining);
}

static void RepeatedDisabledQuiet()
{
    var world = new SimulationWorld();
    var puffer = Create();
    _ = puffer.Step(PufferInput.Disabled, world);
    Equal(0, puffer.Step(PufferInput.Disabled, world).Events.Count);
}

static void EnableTransition()
{
    var world = new SimulationWorld();
    var puffer = Create(direction: -1);
    _ = puffer.Step(PufferInput.None, world);
    _ = puffer.Step(PufferInput.Disabled, world);
    var snapshot = puffer.Step(PufferInput.None, world);
    Equal(PufferState.Swimming, snapshot.State);
    Equal(SimVector.Zero, snapshot.Center);
    Equal(-1, snapshot.SwimDirection);
    Has(snapshot, PufferEventKind.Enabled);
}

static void EnableRequiresRelease()
{
    var world = new SimulationWorld();
    var puffer = Create();
    _ = puffer.Step(PufferInput.Disabled, world);
    var enabled = puffer.Step(new PufferInput(Contact()), world);
    Equal(PufferState.Swimming, enabled.State);
    _ = puffer.Step(PufferInput.None, world);
    Equal(PufferState.Warning, puffer.Step(new PufferInput(Contact()), world).State);
}

static void RequiresStep() => Throws<InvalidOperationException>(() => Create().Update(PufferInput.None, new SimulationWorld()));

static void OncePerTick()
{
    var world = new SimulationWorld();
    var puffer = Create();
    Throws<InvalidOperationException>(() => world.Step(current =>
    {
        _ = puffer.Update(PufferInput.None, current);
        _ = puffer.Update(PufferInput.None, current);
    }));
}

static void EventsImmutable()
{
    var snapshot = Create().Step(PufferInput.None, new SimulationWorld());
    Throws<NotSupportedException>(() => ((ICollection<PufferEvent>)snapshot.Events).Add(snapshot.Events[0]));
}

static void EventIds()
{
    var ids = Enum.GetValues<PufferEventKind>().Select(PufferEventIds.For).ToArray();
    Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    Assert(ids.All(id => id.StartsWith("PUFFER_", StringComparison.Ordinal)), "Puffer event prefix changed.");
}

static void Replay() => Equal(RunReplay(), RunReplay());

static void TwoPufferIsolation()
{
    var world = new SimulationWorld();
    var first = Create("puffer-a");
    var second = Create("puffer-b", direction: -1);
    PufferSnapshot? a = null;
    PufferSnapshot? b = null;
    world.Step(current =>
    {
        a = first.Update(new PufferInput(Contact()), current);
        b = second.Update(PufferInput.None, current);
    });
    Equal(PufferState.Warning, a!.State);
    Equal(PufferState.Swimming, b!.State);
    Assert(!b.Events.Any(item => item.Kind == PufferEventKind.WarningStarted), "Second Puffer was coupled to first.");
}

static void PlayerHorizontalLaunch()
{
    var snapshot = ApplyToPlayer(new SimVector(6m, 0m));
    Equal<decimal>(240m, snapshot.Speed.X);
    Equal<decimal>(0m, snapshot.Speed.Y);
}

static void PlayerDiagonalLaunch()
{
    var snapshot = ApplyToPlayer(new SimVector(4m, 4m));
    Equal<decimal>(144m, snapshot.Speed.X);
    Equal<decimal>(192m, snapshot.Speed.Y);
}

static void PlayerLaunchEvent()
{
    var snapshot = ApplyToPlayer(new SimVector(6m, 0m));
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied), "Player application event missing.");
}

static void PlayerSolidCollision()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new Solid("wall", 3, 0, 2, 10));
    var player = new PlayerNormalController(actor);
    var puffer = Create();
    var pufferWorld = new SimulationWorld();
    _ = puffer.Step(new PufferInput(Contact(new SimVector(6m, 0m))), pufferWorld);
    _ = puffer.Step(PufferInput.None, pufferWorld);
    _ = puffer.Step(PufferInput.None, pufferWorld);
    var launch = puffer.Step(PufferInput.None, pufferWorld).LaunchEffect!.Velocity;
    PlayerNormalSnapshot? applied = null;
    world.Step(current => applied = player.Update(new PlayerInput(0, 0, false, false), new PlayerExternalEffects(null, launch), current));
    Equal<decimal>(0m, applied!.Speed.X);
    Equal(1, actor.X);
    Assert(applied.Events.Any(item => item.Kind == PlayerNormalEventKind.HorizontalBlocked), "Player-owned Solid collision event missing.");
}

static void BothAxesSet()
{
    var effect = Explode(Contact(new SimVector(4m, 4m))).LaunchEffect!.Velocity;
    Assert(effect.SpeedX is not null && effect.SpeedY is not null, "Radial launch did not set both axes.");
}

static void SurfaceIsolation()
{
    var text = string.Join(' ', typeof(PufferController).Assembly.GetExportedTypes().SelectMany(type =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Window", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Rendering", "CelesteDesktop.Desktop", "CelesteDesktop.Player", "CelesteDesktop.Entity.Bumper", "CelesteDesktop.Entity.Water" })
    {
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
    }
}

static PlayerNormalSnapshot ApplyToPlayer(SimVector target)
{
    var puffer = Create();
    var pufferWorld = new SimulationWorld();
    _ = puffer.Step(new PufferInput(Contact(target)), pufferWorld);
    _ = puffer.Step(PufferInput.None, pufferWorld);
    _ = puffer.Step(PufferInput.None, pufferWorld);
    var effect = puffer.Step(PufferInput.None, pufferWorld).LaunchEffect!.Velocity;
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 1, 1);
    world.Add(actor);
    var player = new PlayerNormalController(actor);
    PlayerNormalSnapshot? snapshot = null;
    world.Step(current => snapshot = player.Update(new PlayerInput(0, 0, false, false), new PlayerExternalEffects(null, effect), current));
    return snapshot!;
}

static string RunReplay()
{
    var world = new SimulationWorld();
    var puffer = Create();
    var rows = new List<string>();
    for (var tick = 0; tick < 12; tick++)
    {
        var input = tick is 2 or 10 ? new PufferInput(Contact(new SimVector(4m, 4m))) : PufferInput.None;
        rows.Add(Format(puffer.Step(input, world)));
    }
    return string.Join('|', rows);
}

static void AssertLaunch(SimVector target, decimal directionX, decimal directionY, decimal speedX, decimal speedY)
{
    var snapshot = Explode(Contact(target));
    Equal(directionX, snapshot.LaunchEffect!.Direction.X);
    Equal(directionY, snapshot.LaunchEffect.Direction.Y);
    Equal<decimal?>(speedX, snapshot.LaunchEffect.Velocity.SpeedX);
    Equal<decimal?>(speedY, snapshot.LaunchEffect.Velocity.SpeedY);
}

static PufferSnapshot StartWarning(string id = "player") =>
    Create().Step(new PufferInput(Contact(id: id)), new SimulationWorld());

static (SimulationWorld World, PufferController Puffer) WarningController(PufferContact? contact = null)
{
    var world = new SimulationWorld();
    var puffer = Create();
    _ = puffer.Step(new PufferInput(contact ?? Contact()), world);
    return (world, puffer);
}

static PufferSnapshot Explode(PufferContact contact)
{
    var (world, puffer) = WarningController(contact);
    _ = puffer.Step(PufferInput.None, world);
    _ = puffer.Step(PufferInput.None, world);
    return puffer.Step(PufferInput.None, world);
}

static (SimulationWorld World, PufferController Puffer) SpentController(int direction = 1)
{
    var world = new SimulationWorld();
    var puffer = Create(direction: direction);
    _ = puffer.Step(new PufferInput(Contact()), world);
    _ = puffer.Step(PufferInput.None, world);
    _ = puffer.Step(PufferInput.None, world);
    _ = puffer.Step(PufferInput.None, world);
    return (world, puffer);
}

static PufferController Create(
    string id = "puffer",
    int direction = 1,
    PufferTuning? tuning = null,
    (decimal Left, decimal Right)? bounds = null)
{
    var range = bounds ?? (-2m, 2m);
    return new PufferController(id, SimVector.Zero, range.Left, range.Right, direction, tuning ?? TestTuning());
}

static PufferTuning TestTuning() => new(60m, 10, 3, 240m, 4);

static PufferContact Contact(SimVector? center = null, string id = "player", bool canTrigger = true) =>
    new(id, center ?? new SimVector(4m, 0m), canTrigger);

static string Format(PufferSnapshot snapshot) =>
    $"{snapshot.Tick}:{snapshot.Center}:{snapshot.SwimDirection}:{snapshot.State}:{snapshot.WarningTicksRemaining}:{snapshot.RespawnTicksRemaining}:{snapshot.Armed}:{snapshot.LockedTargetId}:{snapshot.LaunchEffect?.TargetId}:{snapshot.LaunchEffect?.Direction}:{string.Join(',', snapshot.Events.Select(item => item.EventId))}";

static void Has(PufferSnapshot snapshot, PufferEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing Puffer event {kind}.");

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
