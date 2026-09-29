using System.Reflection;
using CelesteDesktop.Entity.Glider;
using CelesteDesktop.Entity.Spring;
using CelesteDesktop.Entity.Theo;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial state is ready", InitialState),
    ("default input keeps Spring enabled", DefaultInputIsEnabled),
    ("entity ID is required", EntityIdRequired),
    ("contact target ID is required", ContactIdRequired),
    ("disabled input rejects contact", DisabledContactRejected),
    ("external velocity requires an axis", VelocityAxisRequired),
    ("up orientation launches upward", UpLaunch),
    ("right orientation launches right", RightLaunch),
    ("down orientation launches downward", DownLaunch),
    ("left orientation launches left", LeftLaunch),
    ("activation enters retracted state", ActivationRetracts),
    ("activation preserves target identity", TargetIdentity),
    ("retracted duration is exact", RetractedDuration),
    ("cooldown duration is exact", CooldownDuration),
    ("ready event is explicit", ReadyEvent),
    ("contact during cooldown is ignored", CooldownIgnores),
    ("sticky contact cannot retrigger", StickyContact),
    ("released contact rearms spring", ReleaseRearms),
    ("ineligible contact is ignored", IneligibleIgnored),
    ("disabled transition is explicit", DisableTransition),
    ("repeated disabled input is quiet", RepeatedDisabledQuiet),
    ("enabled transition is explicit", EnableTransition),
    ("enable while touching requires release", EnableRequiresRelease),
    ("update requires active step", RequiresStep),
    ("controller updates once per tick", OncePerTick),
    ("snapshot events are immutable", EventsImmutable),
    ("event IDs are stable and unique", EventIds),
    ("same inputs replay deterministically", Replay),
    ("two springs remain isolated", TwoSpringIsolation),
    ("player receives vertical launch", PlayerVerticalLaunch),
    ("player horizontal launch preserves vertical speed", PlayerHorizontalLaunch),
    ("player launch event is explicit", PlayerLaunchEvent),
    ("Theo receives vertical launch", TheoVerticalLaunch),
    ("Theo horizontal launch preserves vertical speed", TheoHorizontalLaunch),
    ("held Theo rejects external launch", HeldTheoRejects),
    ("Glider receives vertical launch", GliderVerticalLaunch),
    ("Glider horizontal launch preserves vertical speed", GliderHorizontalLaunch),
    ("held Glider rejects external launch", HeldGliderRejects),
    ("invalid launch speed is rejected", InvalidLaunchSpeed),
    ("invalid retracted ticks are rejected", InvalidRetractedTicks),
    ("invalid cooldown ticks are rejected", InvalidCooldownTicks),
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
    var spring = Create();
    Equal(SpringState.Ready, spring.State);
    Equal(new SimPoint(4, 5), spring.Position);
    Equal(SpringOrientation.Up, spring.Orientation);
}

static void EntityIdRequired() =>
    Throws<ArgumentException>(() => new SpringController(" ", new SimPoint(0, 0), SpringOrientation.Up));

static void ContactIdRequired() =>
    Throws<ArgumentException>(() => new SpringContact("", SpringTargetKind.Player, new SimPoint(0, 0), SimVector.Zero));

static void DisabledContactRejected() =>
    Throws<ArgumentException>(() => new SpringInput(Contact(), disableRequested: true));

static void VelocityAxisRequired() =>
    Throws<ArgumentException>(() => new ExternalVelocityEffect(null, null));

static void DefaultInputIsEnabled()
{
    var world = new SimulationWorld();
    var snapshot = Create().Step(default, world);
    Equal(SpringState.Ready, snapshot.State);
}

static void UpLaunch() => AssertLaunch(SpringOrientation.Up, null, -240m);
static void RightLaunch() => AssertLaunch(SpringOrientation.Right, 240m, null);
static void DownLaunch() => AssertLaunch(SpringOrientation.Down, null, 240m);
static void LeftLaunch() => AssertLaunch(SpringOrientation.Left, -240m, null);

static void ActivationRetracts()
{
    var world = new SimulationWorld();
    var snapshot = Create().Step(new SpringInput(Contact()), world);
    Equal(SpringState.Retracted, snapshot.State);
    Equal(3, snapshot.RetractedTicksRemaining);
    Has(snapshot, SpringEventKind.Activated);
    Has(snapshot, SpringEventKind.Retracted);
    Has(snapshot, SpringEventKind.LaunchIssued);
}

static void TargetIdentity()
{
    var world = new SimulationWorld();
    var snapshot = Create().Step(new SpringInput(Contact("theo-7", SpringTargetKind.Theo)), world);
    Equal("theo-7", snapshot.LaunchEffect!.TargetId);
    Equal(SpringTargetKind.Theo, snapshot.LaunchEffect.TargetKind);
    Assert(snapshot.Events.All(item => item.TargetId == "theo-7"), "Activation events lost target identity.");
}

static void RetractedDuration()
{
    var world = new SimulationWorld();
    var spring = Create();
    _ = spring.Step(new SpringInput(Contact()), world);
    Equal(2, spring.Step(SpringInput.None, world).RetractedTicksRemaining);
    Equal(1, spring.Step(SpringInput.None, world).RetractedTicksRemaining);
    var snapshot = spring.Step(SpringInput.None, world);
    Equal(SpringState.Cooldown, snapshot.State);
    Equal(5, snapshot.CooldownTicksRemaining);
    Has(snapshot, SpringEventKind.CooldownStarted);
}

static void CooldownDuration()
{
    var (world, spring) = EnterCooldown();
    for (var expected = 4; expected >= 1; expected--)
    {
        var snapshot = spring.Step(SpringInput.None, world);
        Equal(SpringState.Cooldown, snapshot.State);
        Equal(expected, snapshot.CooldownTicksRemaining);
    }
    Equal(SpringState.Ready, spring.Step(SpringInput.None, world).State);
}

static void ReadyEvent()
{
    var (world, spring) = EnterCooldown();
    SpringSnapshot? snapshot = null;
    for (var i = 0; i < 5; i++) snapshot = spring.Step(SpringInput.None, world);
    Has(snapshot!, SpringEventKind.Ready);
}

static void CooldownIgnores()
{
    var world = new SimulationWorld();
    var spring = Create();
    _ = spring.Step(new SpringInput(Contact()), world);
    var snapshot = spring.Step(new SpringInput(Contact()), world);
    Assert(snapshot.LaunchEffect is null, "Cooldown produced another launch.");
    Has(snapshot, SpringEventKind.ContactIgnored);
}

static void StickyContact()
{
    var world = new SimulationWorld();
    var spring = Create();
    _ = spring.Step(new SpringInput(Contact()), world);
    SpringSnapshot? snapshot = null;
    for (var i = 0; i < 8; i++) snapshot = spring.Step(new SpringInput(Contact()), world);
    Equal(SpringState.Ready, snapshot!.State);
    Assert(snapshot.LaunchEffect is null, "Sticky contact retriggered after reset.");
}

static void ReleaseRearms()
{
    var world = new SimulationWorld();
    var spring = Create();
    _ = spring.Step(new SpringInput(Contact()), world);
    for (var i = 0; i < 8; i++) _ = spring.Step(new SpringInput(Contact()), world);
    _ = spring.Step(SpringInput.None, world);
    Assert(spring.Step(new SpringInput(Contact()), world).LaunchEffect is not null, "Released contact did not rearm.");
}

static void IneligibleIgnored()
{
    var world = new SimulationWorld();
    var spring = Create();
    var snapshot = spring.Step(new SpringInput(Contact(canActivate: false)), world);
    Equal(SpringState.Ready, snapshot.State);
    Assert(snapshot.LaunchEffect is null, "Ineligible contact launched.");
    Has(snapshot, SpringEventKind.ContactIgnored);
}

static void DisableTransition()
{
    var world = new SimulationWorld();
    var snapshot = Create().Step(SpringInput.Disabled, world);
    Equal(SpringState.Disabled, snapshot.State);
    Has(snapshot, SpringEventKind.Disabled);
}

static void RepeatedDisabledQuiet()
{
    var world = new SimulationWorld();
    var spring = Create();
    _ = spring.Step(SpringInput.Disabled, world);
    Equal(0, spring.Step(SpringInput.Disabled, world).Events.Count);
}

static void EnableTransition()
{
    var world = new SimulationWorld();
    var spring = Create();
    _ = spring.Step(SpringInput.Disabled, world);
    var snapshot = spring.Step(SpringInput.None, world);
    Equal(SpringState.Ready, snapshot.State);
    Has(snapshot, SpringEventKind.Enabled);
}

static void EnableRequiresRelease()
{
    var world = new SimulationWorld();
    var spring = Create();
    _ = spring.Step(SpringInput.Disabled, world);
    var enabled = spring.Step(new SpringInput(Contact()), world);
    Assert(enabled.LaunchEffect is null, "Enable tick launched a touching target.");
    _ = spring.Step(SpringInput.None, world);
    Assert(spring.Step(new SpringInput(Contact()), world).LaunchEffect is not null, "Release did not rearm enabled Spring.");
}

static void RequiresStep()
{
    var world = new SimulationWorld();
    Throws<InvalidOperationException>(() => Create().Update(SpringInput.None, world));
}

static void OncePerTick()
{
    var world = new SimulationWorld();
    var spring = Create();
    Throws<InvalidOperationException>(() => world.Step(current =>
    {
        _ = spring.Update(SpringInput.None, current);
        _ = spring.Update(SpringInput.None, current);
    }));
}

static void EventsImmutable()
{
    var world = new SimulationWorld();
    var snapshot = Create().Step(new SpringInput(Contact()), world);
    Throws<NotSupportedException>(() => ((ICollection<SpringEvent>)snapshot.Events).Add(snapshot.Events[0]));
}

static void EventIds()
{
    var ids = Enum.GetValues<SpringEventKind>().Select(SpringEventIds.For).ToArray();
    Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    Assert(ids.All(id => id.StartsWith("SPRING_", StringComparison.Ordinal)), "Spring event prefix changed.");
}

static void Replay()
{
    static string Run()
    {
        var world = new SimulationWorld();
        var spring = Create(SpringOrientation.Right);
        var rows = new List<string>();
        for (var tick = 0; tick < 14; tick++)
        {
            var input = tick is 0 or 10 ? new SpringInput(Contact()) : SpringInput.None;
            rows.Add(Format(spring.Step(input, world)));
        }
        return string.Join('|', rows);
    }
    Equal(Run(), Run());
}

static void TwoSpringIsolation()
{
    var world = new SimulationWorld();
    var first = Create(SpringOrientation.Up, "spring-a");
    var second = Create(SpringOrientation.Left, "spring-b");
    SpringSnapshot? a = null;
    SpringSnapshot? b = null;
    world.Step(current =>
    {
        a = first.Update(new SpringInput(Contact("player-a")), current);
        b = second.Update(SpringInput.None, current);
    });
    Assert(a!.LaunchEffect is not null, "First Spring did not launch.");
    Equal(SpringState.Ready, b!.State);
    Equal(0, b.Events.Count);
}

static void PlayerVerticalLaunch()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 1, 1);
    world.Add(actor);
    var player = new PlayerNormalController(actor, initialSpeed: new SimVector(30m, 80m));
    var spring = Create(SpringOrientation.Up);
    PlayerNormalSnapshot? snapshot = null;
    world.Step(current =>
    {
        var launch = spring.Update(new SpringInput(Contact("player", SpringTargetKind.Player)), current).LaunchEffect!;
        snapshot = player.Update(new PlayerInput(0, 0, false, false), new PlayerExternalEffects(null, launch.Velocity), current);
    });
    Equal(30m - ((player.Tuning.RunAcceleration * player.Tuning.AirControlMultiplier) / 60m), snapshot!.Speed.X);
    Equal<decimal>(-240m, snapshot.Speed.Y);
}

static void PlayerHorizontalLaunch()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 1, 1);
    world.Add(actor);
    var player = new PlayerNormalController(actor, initialSpeed: new SimVector(0m, 40m));
    var spring = Create(SpringOrientation.Right);
    PlayerNormalSnapshot? snapshot = null;
    world.Step(current =>
    {
        var launch = spring.Update(new SpringInput(Contact("player", SpringTargetKind.Player)), current).LaunchEffect!;
        snapshot = player.Update(new PlayerInput(0, 0, false, false), new PlayerExternalEffects(null, launch.Velocity), current);
    });
    Equal<decimal>(240m, snapshot!.Speed.X);
    Equal<decimal>(55m, snapshot.Speed.Y);
}

static void PlayerLaunchEvent()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 1, 1);
    world.Add(actor);
    var player = new PlayerNormalController(actor);
    var spring = Create();
    PlayerNormalSnapshot? snapshot = null;
    world.Step(current =>
    {
        var launch = spring.Update(new SpringInput(Contact("player", SpringTargetKind.Player)), current).LaunchEffect!;
        snapshot = player.Update(new PlayerInput(0, 0, false, false), new PlayerExternalEffects(null, launch.Velocity), current);
    });
    Assert(snapshot!.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied), "Player launch event missing.");
}

static void TheoVerticalLaunch()
{
    var world = new SimulationWorld();
    var actor = new Actor("theo", 0, 0, 1, 1);
    world.Add(actor);
    var theo = new TheoCrystalController(actor, initialSpeed: new SimVector(25m, 80m));
    var spring = Create();
    TheoCrystalSnapshot? snapshot = null;
    world.Step(current =>
    {
        var launch = spring.Update(new SpringInput(Contact("theo", SpringTargetKind.Theo)), current).LaunchEffect!;
        snapshot = theo.Update(TheoCrystalInput.None, launch.Velocity, current);
    });
    Equal<decimal>(25m - (theo.Tuning.HorizontalFriction / 60m), snapshot!.Speed.X);
    Equal<decimal>(-240m, snapshot.Speed.Y);
    HasTheo(snapshot, TheoCrystalEventKind.ExternalVelocityApplied);
}

static void TheoHorizontalLaunch()
{
    var world = new SimulationWorld();
    var actor = new Actor("theo", 0, 0, 1, 1);
    world.Add(actor);
    var theo = new TheoCrystalController(actor, initialSpeed: new SimVector(0m, 0m));
    var spring = Create(SpringOrientation.Left);
    TheoCrystalSnapshot? snapshot = null;
    world.Step(current =>
    {
        var launch = spring.Update(new SpringInput(Contact("theo", SpringTargetKind.Theo)), current).LaunchEffect!;
        snapshot = theo.Update(TheoCrystalInput.None, launch.Velocity, current);
    });
    Equal<decimal>(-240m, snapshot!.Speed.X);
    Equal(theo.Tuning.Gravity / 60m, snapshot.Speed.Y);
}

static void HeldTheoRejects()
{
    var world = new SimulationWorld();
    var actor = new Actor("theo", 0, 0, 1, 1);
    world.Add(actor);
    var theo = new TheoCrystalController(actor);
    _ = theo.Step(new TheoCrystalInput(TheoCrystalAction.Pickup, new TheoHolderSnapshot("player", new SimPoint(0, 0), 1)), world);
    Throws<InvalidOperationException>(() => theo.Step(
        new TheoCrystalInput(TheoCrystalAction.None, new TheoHolderSnapshot("player", new SimPoint(0, 0), 1)),
        new ExternalVelocityEffect(null, -240m), world));
}

static void GliderVerticalLaunch()
{
    var world = new SimulationWorld();
    var actor = new Actor("glider", 0, 0, 1, 1);
    world.Add(actor);
    var glider = new GliderController(actor, initialSpeed: new SimVector(25m, 20m));
    var spring = Create();
    GliderSnapshot? snapshot = null;
    world.Step(current =>
    {
        var launch = spring.Update(new SpringInput(Contact("glider", SpringTargetKind.Glider)), current).LaunchEffect!;
        snapshot = glider.Update(GliderInput.None, launch.Velocity, current);
    });
    Equal<decimal>(25m - (glider.Tuning.HorizontalFriction / 60m), snapshot!.Speed.X);
    Equal<decimal>(-240m, snapshot.Speed.Y);
    HasGlider(snapshot, GliderEventKind.ExternalVelocityApplied);
}

static void GliderHorizontalLaunch()
{
    var world = new SimulationWorld();
    var actor = new Actor("glider", 0, 0, 1, 1);
    world.Add(actor);
    var glider = new GliderController(actor);
    var spring = Create(SpringOrientation.Right);
    GliderSnapshot? snapshot = null;
    world.Step(current =>
    {
        var launch = spring.Update(new SpringInput(Contact("glider", SpringTargetKind.Glider)), current).LaunchEffect!;
        snapshot = glider.Update(GliderInput.None, launch.Velocity, current);
    });
    Equal<decimal>(240m, snapshot!.Speed.X);
    Equal(glider.Tuning.Gravity / 60m, snapshot.Speed.Y);
}

static void HeldGliderRejects()
{
    var world = new SimulationWorld();
    var actor = new Actor("glider", 0, 0, 1, 1);
    world.Add(actor);
    var glider = new GliderController(actor);
    _ = glider.Step(new GliderInput(GliderAction.Pickup, new GliderHolderSnapshot("player", new SimPoint(0, 0), 1)), world);
    Throws<InvalidOperationException>(() => glider.Step(
        new GliderInput(GliderAction.None, new GliderHolderSnapshot("player", new SimPoint(0, 0), 1)),
        new ExternalVelocityEffect(null, -240m), world));
}

static void InvalidLaunchSpeed() =>
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: SpringTuning.PartialBaseline with { LaunchSpeed = 0m }));

static void InvalidRetractedTicks() =>
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: SpringTuning.PartialBaseline with { RetractedTicks = 0 }));

static void InvalidCooldownTicks() =>
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: SpringTuning.PartialBaseline with { CooldownTicks = 0 }));

static void SurfaceIsolation()
{
    var text = string.Join(' ', typeof(SpringController).Assembly.GetExportedTypes().SelectMany(type =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Window", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Rendering", "CelesteDesktop.Desktop", "CelesteDesktop.Player", "CelesteDesktop.Entity.Theo", "CelesteDesktop.Entity.Glider" })
    {
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
    }
}

static (SimulationWorld World, SpringController Spring) EnterCooldown()
{
    var world = new SimulationWorld();
    var spring = Create();
    _ = spring.Step(new SpringInput(Contact()), world);
    _ = spring.Step(SpringInput.None, world);
    _ = spring.Step(SpringInput.None, world);
    _ = spring.Step(SpringInput.None, world);
    return (world, spring);
}

static void AssertLaunch(SpringOrientation orientation, decimal? x, decimal? y)
{
    var world = new SimulationWorld();
    var snapshot = Create(orientation).Step(new SpringInput(Contact()), world);
    Equal(x, snapshot.LaunchEffect!.Velocity.SpeedX);
    Equal(y, snapshot.LaunchEffect.Velocity.SpeedY);
}

static SpringController Create(
    SpringOrientation orientation = SpringOrientation.Up,
    string id = "spring",
    SpringTuning? tuning = null) =>
    new(id, new SimPoint(4, 5), orientation, tuning);

static SpringContact Contact(
    string id = "player",
    SpringTargetKind kind = SpringTargetKind.Player,
    bool canActivate = true) =>
    new(id, kind, new SimPoint(4, 4), new SimVector(0m, 80m), canActivate);

static string Format(SpringSnapshot snapshot) =>
    $"{snapshot.Tick}:{snapshot.State}:{snapshot.RetractedTicksRemaining}:{snapshot.CooldownTicksRemaining}:{snapshot.Armed}:{snapshot.LaunchEffect?.TargetId}:{string.Join(',', snapshot.Events.Select(item => item.EventId))}";

static void Has(SpringSnapshot snapshot, SpringEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing Spring event {kind}.");

static void HasTheo(TheoCrystalSnapshot snapshot, TheoCrystalEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing Theo event {kind}.");

static void HasGlider(GliderSnapshot snapshot, GliderEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing Glider event {kind}.");

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
