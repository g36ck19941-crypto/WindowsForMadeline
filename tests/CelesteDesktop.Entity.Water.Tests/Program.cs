using System.Reflection;
using CelesteDesktop.Entity.Refill;
using CelesteDesktop.Entity.Water;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial state is active", InitialState),
    ("volume bounds are preserved", BoundsPreserved),
    ("entity ID is required", EntityIdRequired),
    ("maximum horizontal speed must be positive", InvalidHorizontalSpeed),
    ("maximum vertical speed must be positive", InvalidVerticalSpeed),
    ("neutral buoyancy must be bounded", InvalidBuoyancy),
    ("horizontal acceleration must be positive", InvalidHorizontalAcceleration),
    ("vertical acceleration must be positive", InvalidVerticalAcceleration),
    ("contact target ID is required", ContactIdRequired),
    ("horizontal swim axis is bounded", HorizontalAxisBounded),
    ("vertical swim axis is bounded", VerticalAxisBounded),
    ("disabled input rejects contacts", DisabledContactRejected),
    ("duplicate target contacts are rejected", DuplicateTargetsRejected),
    ("nonoverlapping contact is ignored", NonoverlapIgnored),
    ("ineligible contact is ignored", IneligibleIgnored),
    ("overlap emits entered", EnteredEvent),
    ("overlap emits submerged", SubmergedEvent),
    ("motion preserves target identity", MotionTargetIdentity),
    ("neutral water drags positive horizontal speed", PositiveHorizontalDrag),
    ("neutral water drags negative horizontal speed", NegativeHorizontalDrag),
    ("neutral water applies upward buoyancy", NeutralBuoyancy),
    ("left input targets left swim", SwimLeft),
    ("right input targets right swim", SwimRight),
    ("up input targets upward swim", SwimUp),
    ("down input targets downward swim", SwimDown),
    ("horizontal speed is capped", HorizontalCap),
    ("upward speed is capped", UpwardCap),
    ("downward speed is capped", DownwardCap),
    ("sticky overlap does not reenter", StickyDoesNotReenter),
    ("leaving emits exit", ExitEvent),
    ("reentry emits a new enter", ReentryEvent),
    ("multiple occupants are ordered", MultipleOccupantsOrdered),
    ("contact order does not change output", InputOrderDeterministic),
    ("snapshot occupants are immutable", OccupantsImmutable),
    ("snapshot effects are immutable", EffectsImmutable),
    ("snapshot events are immutable", EventsImmutable),
    ("event IDs are stable and unique", EventIds),
    ("disable exits current occupants", DisableExitsOccupants),
    ("repeated disabled input is quiet", RepeatedDisabledQuiet),
    ("enable transition is explicit", EnableTransition),
    ("update requires active step", RequiresStep),
    ("controller updates once per tick", OncePerTick),
    ("same inputs replay deterministically", Replay),
    ("two Water volumes remain isolated", TwoWaterIsolation),
    ("disabled Water does not affect Refill", RefillIsolation),
    ("Player applies Water velocity", PlayerAppliesVelocity),
    ("Player velocity application event is explicit", PlayerApplicationEvent),
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

static void InitialState() => Equal(WaterState.Active, Create().State);

static void BoundsPreserved() => Equal(new SimRect(0, 0, 10, 8), Create().Bounds);

static void EntityIdRequired() =>
    Throws<ArgumentException>(() => new WaterController(" ", new SimRect(0, 0, 1, 1)));

static void InvalidHorizontalSpeed() => Throws<ArgumentOutOfRangeException>(() =>
    Create(tuning: Tuning(maximumHorizontalSpeed: 0m)));

static void InvalidVerticalSpeed() => Throws<ArgumentOutOfRangeException>(() =>
    Create(tuning: Tuning(maximumVerticalSpeed: 0m)));

static void InvalidBuoyancy()
{
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: Tuning(neutralBuoyancySpeed: 0m)));
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: Tuning(neutralBuoyancySpeed: 61m)));
}

static void InvalidHorizontalAcceleration() => Throws<ArgumentOutOfRangeException>(() =>
    Create(tuning: Tuning(horizontalAcceleration: 0m)));

static void InvalidVerticalAcceleration() => Throws<ArgumentOutOfRangeException>(() =>
    Create(tuning: Tuning(verticalAcceleration: 0m)));

static void ContactIdRequired() => Throws<ArgumentException>(() => Contact(""));

static void HorizontalAxisBounded()
{
    Throws<ArgumentOutOfRangeException>(() => Contact(moveX: -2));
    Throws<ArgumentOutOfRangeException>(() => Contact(moveX: 2));
}

static void VerticalAxisBounded()
{
    Throws<ArgumentOutOfRangeException>(() => Contact(moveY: -2));
    Throws<ArgumentOutOfRangeException>(() => Contact(moveY: 2));
}

static void DisabledContactRejected() => Throws<ArgumentException>(() =>
    new WaterInput([Contact()], disableRequested: true));

static void DuplicateTargetsRejected() => Throws<ArgumentException>(() =>
    new WaterInput([Contact("same"), Contact("same")]));

static void NonoverlapIgnored()
{
    var snapshot = Step(Create(), Contact(bounds: new SimRect(20, 20, 2, 2)));
    Equal(0, snapshot.Occupants.Count);
    Has(snapshot, WaterEventKind.ContactIgnored);
}

static void IneligibleIgnored()
{
    var snapshot = Step(Create(), Contact(canSwim: false));
    Equal(0, snapshot.MotionEffects.Count);
    Has(snapshot, WaterEventKind.ContactIgnored);
}

static void EnteredEvent() => Has(Step(Create(), Contact()), WaterEventKind.Entered);

static void SubmergedEvent() => Has(Step(Create(), Contact()), WaterEventKind.Submerged);

static void MotionTargetIdentity()
{
    var snapshot = Step(Create(), Contact("player-7"));
    Equal("player-7", snapshot.MotionEffects.Single().TargetId);
    Assert(snapshot.Events.Where(item => item.Kind != WaterEventKind.Enabled).All(item => item.TargetId == "player-7"),
        "Water events lost target identity.");
}

static void PositiveHorizontalDrag() => Equal<decimal>(25m, Velocity(Contact(velocity: new SimVector(30m, 0m))).SpeedX!.Value);

static void NegativeHorizontalDrag() => Equal<decimal>(-25m, Velocity(Contact(velocity: new SimVector(-30m, 0m))).SpeedX!.Value);

static void NeutralBuoyancy() => Equal<decimal>(-4m, Velocity(Contact()).SpeedY!.Value);

static void SwimLeft() => Equal<decimal>(-5m, Velocity(Contact(moveX: -1)).SpeedX!.Value);

static void SwimRight() => Equal<decimal>(5m, Velocity(Contact(moveX: 1)).SpeedX!.Value);

static void SwimUp() => Equal<decimal>(-4m, Velocity(Contact(moveY: -1)).SpeedY!.Value);

static void SwimDown() => Equal<decimal>(4m, Velocity(Contact(moveY: 1)).SpeedY!.Value);

static void HorizontalCap()
{
    Equal<decimal>(60m, Velocity(Contact(velocity: new SimVector(500m, 0m))).SpeedX!.Value);
    Equal<decimal>(-60m, Velocity(Contact(velocity: new SimVector(-500m, 0m))).SpeedX!.Value);
}

static void UpwardCap() => Equal<decimal>(-60m, Velocity(Contact(velocity: new SimVector(0m, -500m))).SpeedY!.Value);

static void DownwardCap() => Equal<decimal>(60m, Velocity(Contact(velocity: new SimVector(0m, 500m))).SpeedY!.Value);

static void StickyDoesNotReenter()
{
    var world = new SimulationWorld();
    var water = Create();
    _ = water.Step(new WaterInput([Contact()]), world);
    var second = water.Step(new WaterInput([Contact()]), world);
    Assert(!second.Events.Any(item => item.Kind == WaterEventKind.Entered), "Sticky contact entered twice.");
}

static void ExitEvent()
{
    var world = new SimulationWorld();
    var water = Create();
    _ = water.Step(new WaterInput([Contact()]), world);
    var snapshot = water.Step(WaterInput.None, world);
    Has(snapshot, WaterEventKind.Exited);
    Equal(0, snapshot.Occupants.Count);
}

static void ReentryEvent()
{
    var world = new SimulationWorld();
    var water = Create();
    _ = water.Step(new WaterInput([Contact()]), world);
    _ = water.Step(WaterInput.None, world);
    Has(water.Step(new WaterInput([Contact()]), world), WaterEventKind.Entered);
}

static void MultipleOccupantsOrdered()
{
    var snapshot = Create().Step(new WaterInput([Contact("z"), Contact("a")]), new SimulationWorld());
    Equal("a,z", string.Join(',', snapshot.Occupants));
    Equal("a,z", string.Join(',', snapshot.MotionEffects.Select(item => item.TargetId)));
}

static void InputOrderDeterministic()
{
    static string Run(IEnumerable<WaterContact> contacts) => Format(Create().Step(new WaterInput(contacts), new SimulationWorld()));
    Equal(Run([Contact("z"), Contact("a")]), Run([Contact("a"), Contact("z")]));
}

static void OccupantsImmutable()
{
    var snapshot = Step(Create(), Contact());
    Throws<NotSupportedException>(() => ((IList<string>)snapshot.Occupants).Add("other"));
}

static void EffectsImmutable()
{
    var snapshot = Step(Create(), Contact());
    Throws<NotSupportedException>(() => ((IList<WaterMotionEffect>)snapshot.MotionEffects).Add(snapshot.MotionEffects[0]));
}

static void EventsImmutable()
{
    var snapshot = Step(Create(), Contact());
    Throws<NotSupportedException>(() => ((IList<WaterEvent>)snapshot.Events).Add(snapshot.Events[0]));
}

static void EventIds()
{
    var kinds = Enum.GetValues<WaterEventKind>();
    var ids = kinds.Select(WaterEventIds.For).ToArray();
    Equal(kinds.Length, ids.Distinct(StringComparer.Ordinal).Count());
    Equal("WATER_ENTERED", WaterEventIds.Entered);
    Equal("WATER_MOTION_ISSUED", WaterEventIds.MotionIssued);
    Equal("WATER_EXITED", WaterEventIds.Exited);
}

static void DisableExitsOccupants()
{
    var world = new SimulationWorld();
    var water = Create();
    _ = water.Step(new WaterInput([Contact()]), world);
    var snapshot = water.Step(WaterInput.Disabled, world);
    Equal(WaterState.Disabled, snapshot.State);
    Equal(0, snapshot.Occupants.Count);
    Has(snapshot, WaterEventKind.Exited);
    Has(snapshot, WaterEventKind.Disabled);
}

static void RepeatedDisabledQuiet()
{
    var world = new SimulationWorld();
    var water = Create();
    _ = water.Step(WaterInput.Disabled, world);
    Equal(0, water.Step(WaterInput.Disabled, world).Events.Count);
}

static void EnableTransition()
{
    var world = new SimulationWorld();
    var water = Create();
    _ = water.Step(WaterInput.Disabled, world);
    var snapshot = water.Step(WaterInput.None, world);
    Equal(WaterState.Active, snapshot.State);
    Has(snapshot, WaterEventKind.Enabled);
}

static void RequiresStep() => Throws<InvalidOperationException>(() =>
    Create().Update(WaterInput.None, new SimulationWorld()));

static void OncePerTick()
{
    var world = new SimulationWorld();
    var water = Create();
    world.Step(current =>
    {
        _ = water.Update(WaterInput.None, current);
        Throws<InvalidOperationException>(() => water.Update(WaterInput.None, current));
    });
}

static void Replay()
{
    static string Run()
    {
        var world = new SimulationWorld();
        var water = Create();
        var rows = new List<string>();
        for (var tick = 0; tick < 7; tick++)
        {
            var input = tick switch
            {
                0 or 1 => new WaterInput([Contact(velocity: new SimVector(30m - tick * 5m, tick * -4m))]),
                3 or 4 => new WaterInput([Contact(moveX: 1, moveY: -1)]),
                _ => WaterInput.None
            };
            rows.Add(Format(water.Step(input, world)));
        }
        return string.Join('|', rows);
    }
    Equal(Run(), Run());
}

static void TwoWaterIsolation()
{
    var world = new SimulationWorld();
    var first = Create("first");
    var second = Create("second");
    WaterSnapshot? a = null;
    WaterSnapshot? b = null;
    world.Step(current =>
    {
        a = first.Update(new WaterInput([Contact()]), current);
        b = second.Update(WaterInput.None, current);
    });
    Equal(1, a!.Occupants.Count);
    Equal(0, b!.Occupants.Count);
}

static void RefillIsolation()
{
    var world = new SimulationWorld();
    var water = Create();
    var refill = new RefillController("refill", new SimPoint(0, 0), new RefillTuning(3));
    RefillSnapshot? refillSnapshot = null;
    world.Step(current =>
    {
        _ = water.Update(WaterInput.Disabled, current);
        refillSnapshot = refill.Update(new RefillInput(new RefillContact("player", new SimPoint(0, 0), 0, 1, 20m, 110m)), current);
    });
    Assert(refillSnapshot!.RestoreEffect is not null, "Disabled Water affected Refill.");
}

static void PlayerAppliesVelocity()
{
    var snapshot = ApplyWaterToPlayer();
    Equal<decimal>(60m, snapshot.Speed.X);
    Equal<decimal>(-4m, snapshot.Speed.Y);
}

static void PlayerApplicationEvent()
{
    var snapshot = ApplyWaterToPlayer();
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied),
        "Player did not record Water velocity application.");
}

static void SurfaceIsolation()
{
    var assembly = typeof(WaterController).Assembly;
    var forbidden = new[] { "System.IO", "System.Windows", "CelesteDesktop.Rendering", "CelesteDesktop.Desktop", "CelesteDesktop.Player", "CelesteDesktop.Entity.Refill" };
    var references = assembly.GetReferencedAssemblies().Select(item => item.Name ?? string.Empty).ToArray();
    Assert(!references.Any(name => forbidden.Any(item => name.StartsWith(item, StringComparison.Ordinal))),
        "Water assembly references a forbidden layer.");
    foreach (var type in assembly.GetExportedTypes())
    {
        foreach (var memberType in PublicMemberTypes(type))
        {
            Assert(!forbidden.Any(item => (memberType.Namespace ?? string.Empty).StartsWith(item, StringComparison.Ordinal)),
                $"Forbidden public type {memberType.FullName}.");
        }
    }
}

static PlayerNormalSnapshot ApplyWaterToPlayer()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 1, 1, 1, 1);
    world.Add(actor);
    var player = new PlayerNormalController(actor, initialSpeed: new SimVector(100m, 0m));
    var water = Create();
    PlayerNormalSnapshot? playerSnapshot = null;
    world.Step(current =>
    {
        var waterSnapshot = water.Update(new WaterInput([Contact(
            bounds: actor.Bounds,
            velocity: new SimVector(player.SpeedX, player.SpeedY))]), current);
        playerSnapshot = player.Update(
            new PlayerInput(0, 0, false, false),
            new PlayerExternalEffects(null, waterSnapshot.MotionEffects.Single().Velocity),
            current);
    });
    return playerSnapshot!;
}

static ExternalVelocityEffect Velocity(WaterContact contact) =>
    Step(Create(), contact).MotionEffects.Single().Velocity;

static WaterSnapshot Step(WaterController water, WaterContact contact) =>
    water.Step(new WaterInput([contact]), new SimulationWorld());

static WaterController Create(string id = "water", WaterTuning? tuning = null) =>
    new(id, new SimRect(0, 0, 10, 8), tuning ?? Tuning());

static WaterTuning Tuning(
    decimal maximumHorizontalSpeed = 60m,
    decimal maximumVerticalSpeed = 60m,
    decimal neutralBuoyancySpeed = 20m,
    decimal horizontalAcceleration = 300m,
    decimal verticalAcceleration = 240m) =>
    new(maximumHorizontalSpeed, maximumVerticalSpeed, neutralBuoyancySpeed, horizontalAcceleration, verticalAcceleration);

static WaterContact Contact(
    string id = "player",
    SimRect? bounds = null,
    SimVector velocity = default,
    int moveX = 0,
    int moveY = 0,
    bool canSwim = true) =>
    new(id, bounds ?? new SimRect(1, 1, 2, 2), velocity, moveX, moveY, canSwim);

static string Format(WaterSnapshot snapshot) =>
    $"{snapshot.Tick}:{snapshot.State}:{string.Join(',', snapshot.Occupants)}:" +
    $"{string.Join(',', snapshot.MotionEffects.Select(item => $"{item.TargetId}@{item.Velocity.SpeedX}/{item.Velocity.SpeedY}"))}:" +
    string.Join(',', snapshot.Events.Select(item => $"{item.EventId}/{item.TargetId}"));

static IEnumerable<Type> PublicMemberTypes(Type type)
{
    const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
    foreach (var property in type.GetProperties(flags)) yield return property.PropertyType;
    foreach (var constructor in type.GetConstructors(flags))
    foreach (var parameter in constructor.GetParameters()) yield return parameter.ParameterType;
    foreach (var method in type.GetMethods(flags).Where(item => item.DeclaringType == type))
    {
        yield return method.ReturnType;
        foreach (var parameter in method.GetParameters()) yield return parameter.ParameterType;
    }
}

static void Has(WaterSnapshot snapshot, WaterEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing Water event {kind}.");

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
