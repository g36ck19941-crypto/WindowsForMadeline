using System.Reflection;
using CelesteDesktop.Entity.Refill;
using CelesteDesktop.Entity.Spring;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial state is available", InitialState),
    ("default input keeps Refill enabled", DefaultInputIsEnabled),
    ("entity ID is required", EntityIdRequired),
    ("contact target ID is required", ContactIdRequired),
    ("contact rejects negative dashes", NegativeDashesRejected),
    ("contact rejects excess dashes", ExcessDashesRejected),
    ("contact rejects zero maximum dashes", ZeroMaximumDashesRejected),
    ("contact rejects negative stamina", NegativeStaminaRejected),
    ("contact rejects excess stamina", ExcessStaminaRejected),
    ("contact rejects zero maximum stamina", ZeroMaximumStaminaRejected),
    ("disabled input rejects contact", DisabledContactRejected),
    ("external resource effect requires a value", ResourceValueRequired),
    ("external resource effect rejects negative charges", NegativeChargesRejected),
    ("external resource effect rejects negative stamina", NegativeResourceStaminaRejected),
    ("collection enters cooldown", CollectionStartsCooldown),
    ("collection preserves target identity", TargetIdentity),
    ("restore effect carries maxima", RestoreCarriesMaxima),
    ("cooldown duration is exact", CooldownDuration),
    ("respawn event is explicit", RespawnEvent),
    ("sticky contact cannot recollect after respawn", StickyContact),
    ("released contact rearms Refill", ReleaseRearms),
    ("full-resource contact is ignored", FullContactIgnored),
    ("ineligible contact is ignored", IneligibleIgnored),
    ("contact during cooldown is ignored", CooldownIgnores),
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
    ("two Refills remain isolated", TwoRefillIsolation),
    ("disabled Refill does not affect Spring", SpringIsolation),
    ("Player receives dash restoration", PlayerDashRestoration),
    ("Player receives stamina restoration", PlayerStaminaRestoration),
    ("Player clamps external resources to its maxima", PlayerResourceClamp),
    ("Player resource application event is explicit", PlayerResourceEvent),
    ("invalid respawn duration is rejected", InvalidRespawnTicks),
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
    var refill = Create();
    Equal(RefillState.Available, refill.State);
    Equal(new SimPoint(4, 5), refill.Position);
}

static void DefaultInputIsEnabled()
{
    var snapshot = Create().Step(default, new SimulationWorld());
    Equal(RefillState.Available, snapshot.State);
}

static void EntityIdRequired() =>
    Throws<ArgumentException>(() => new RefillController(" ", new SimPoint(0, 0)));

static void ContactIdRequired() =>
    Throws<ArgumentException>(() => new RefillContact("", new SimPoint(0, 0), 0, 1, 0m, 110m));

static void NegativeDashesRejected() =>
    Throws<ArgumentOutOfRangeException>(() => new RefillContact("p", new SimPoint(0, 0), -1, 1, 0m, 110m));

static void ExcessDashesRejected() =>
    Throws<ArgumentOutOfRangeException>(() => new RefillContact("p", new SimPoint(0, 0), 2, 1, 0m, 110m));

static void ZeroMaximumDashesRejected() =>
    Throws<ArgumentOutOfRangeException>(() => new RefillContact("p", new SimPoint(0, 0), 0, 0, 0m, 110m));

static void NegativeStaminaRejected() =>
    Throws<ArgumentOutOfRangeException>(() => new RefillContact("p", new SimPoint(0, 0), 0, 1, -1m, 110m));

static void ExcessStaminaRejected() =>
    Throws<ArgumentOutOfRangeException>(() => new RefillContact("p", new SimPoint(0, 0), 0, 1, 111m, 110m));

static void ZeroMaximumStaminaRejected() =>
    Throws<ArgumentOutOfRangeException>(() => new RefillContact("p", new SimPoint(0, 0), 0, 1, 0m, 0m));

static void DisabledContactRejected() =>
    Throws<ArgumentException>(() => new RefillInput(Contact(), disableRequested: true));

static void ResourceValueRequired() =>
    Throws<ArgumentException>(() => new ExternalResourceEffect(null, null));

static void NegativeChargesRejected() =>
    Throws<ArgumentOutOfRangeException>(() => new ExternalResourceEffect(-1, null));

static void NegativeResourceStaminaRejected() =>
    Throws<ArgumentOutOfRangeException>(() => new ExternalResourceEffect(null, -1m));

static void CollectionStartsCooldown()
{
    var snapshot = Create().Step(new RefillInput(Contact()), new SimulationWorld());
    Equal(RefillState.Cooldown, snapshot.State);
    Equal(3, snapshot.RespawnTicksRemaining);
    Has(snapshot, RefillEventKind.Collected);
    Has(snapshot, RefillEventKind.CooldownStarted);
    Has(snapshot, RefillEventKind.RestoreIssued);
}

static void TargetIdentity()
{
    var snapshot = Create().Step(new RefillInput(Contact("player-7")), new SimulationWorld());
    Equal("player-7", snapshot.RestoreEffect!.TargetId);
    Assert(snapshot.Events.All(item => item.TargetId == "player-7"), "Collection events lost target identity.");
}

static void RestoreCarriesMaxima()
{
    var snapshot = Create().Step(new RefillInput(Contact(dashes: 0, maximumDashes: 2, stamina: 15m)), new SimulationWorld());
    Equal(2, snapshot.RestoreEffect!.Resources.ChargeCount);
    Equal(110m, snapshot.RestoreEffect.Resources.Stamina);
}

static void CooldownDuration()
{
    var world = new SimulationWorld();
    var refill = Create();
    _ = refill.Step(new RefillInput(Contact()), world);
    Equal(2, refill.Step(RefillInput.None, world).RespawnTicksRemaining);
    Equal(1, refill.Step(RefillInput.None, world).RespawnTicksRemaining);
    Equal(RefillState.Available, refill.Step(RefillInput.None, world).State);
}

static void RespawnEvent()
{
    var (_, _, snapshot) = ReachRespawn();
    Has(snapshot, RefillEventKind.Respawned);
}

static void StickyContact()
{
    var world = new SimulationWorld();
    var refill = Create();
    _ = refill.Step(new RefillInput(Contact()), world);
    _ = refill.Step(new RefillInput(Contact()), world);
    _ = refill.Step(new RefillInput(Contact()), world);
    var respawn = refill.Step(new RefillInput(Contact()), world);
    Equal(RefillState.Available, respawn.State);
    Assert(respawn.RestoreEffect is null, "Sticky contact recollected immediately after respawn.");
}

static void ReleaseRearms()
{
    var (world, refill, _) = ReachRespawn();
    _ = refill.Step(RefillInput.None, world);
    var recollected = refill.Step(new RefillInput(Contact()), world);
    Assert(recollected.RestoreEffect is not null, "Released Refill did not rearm.");
}

static void FullContactIgnored()
{
    var snapshot = Create().Step(new RefillInput(Contact(dashes: 1, stamina: 110m)), new SimulationWorld());
    Equal(RefillState.Available, snapshot.State);
    Assert(snapshot.RestoreEffect is null, "Full-resource contact produced an effect.");
    Has(snapshot, RefillEventKind.ContactIgnored);
}

static void IneligibleIgnored()
{
    var snapshot = Create().Step(new RefillInput(Contact(canActivate: false)), new SimulationWorld());
    Equal(RefillState.Available, snapshot.State);
    Has(snapshot, RefillEventKind.ContactIgnored);
}

static void CooldownIgnores()
{
    var world = new SimulationWorld();
    var refill = Create();
    _ = refill.Step(new RefillInput(Contact()), world);
    var snapshot = refill.Step(new RefillInput(Contact("other")), world);
    Assert(snapshot.RestoreEffect is null, "Cooldown contact produced an effect.");
    Has(snapshot, RefillEventKind.ContactIgnored);
}

static void DisableTransition()
{
    var snapshot = Create().Step(RefillInput.Disabled, new SimulationWorld());
    Equal(RefillState.Disabled, snapshot.State);
    Has(snapshot, RefillEventKind.Disabled);
}

static void DisableClearsCooldown()
{
    var world = new SimulationWorld();
    var refill = Create();
    _ = refill.Step(new RefillInput(Contact()), world);
    var snapshot = refill.Step(RefillInput.Disabled, world);
    Equal(0, snapshot.RespawnTicksRemaining);
}

static void RepeatedDisabledQuiet()
{
    var world = new SimulationWorld();
    var refill = Create();
    _ = refill.Step(RefillInput.Disabled, world);
    Equal(0, refill.Step(RefillInput.Disabled, world).Events.Count);
}

static void EnableTransition()
{
    var world = new SimulationWorld();
    var refill = Create();
    _ = refill.Step(RefillInput.Disabled, world);
    var snapshot = refill.Step(RefillInput.None, world);
    Equal(RefillState.Available, snapshot.State);
    Has(snapshot, RefillEventKind.Enabled);
}

static void EnableRequiresRelease()
{
    var world = new SimulationWorld();
    var refill = Create();
    _ = refill.Step(RefillInput.Disabled, world);
    _ = refill.Step(new RefillInput(Contact()), world);
    var ignored = refill.Step(new RefillInput(Contact()), world);
    Assert(ignored.RestoreEffect is null, "Enable while touching bypassed release-to-rearm.");
    _ = refill.Step(RefillInput.None, world);
    Assert(refill.Step(new RefillInput(Contact()), world).RestoreEffect is not null, "Release did not rearm enabled Refill.");
}

static void RequiresStep() =>
    Throws<InvalidOperationException>(() => Create().Update(RefillInput.None, new SimulationWorld()));

static void OncePerTick()
{
    var world = new SimulationWorld();
    var refill = Create();
    world.Step(current =>
    {
        _ = refill.Update(RefillInput.None, current);
        Throws<InvalidOperationException>(() => refill.Update(RefillInput.None, current));
    });
}

static void EventsImmutable()
{
    var snapshot = Create().Step(new RefillInput(Contact()), new SimulationWorld());
    Throws<NotSupportedException>(() => ((IList<RefillEvent>)snapshot.Events).Add(snapshot.Events[0]));
}

static void EventIds()
{
    var kinds = Enum.GetValues<RefillEventKind>();
    var ids = kinds.Select(RefillEventIds.For).ToArray();
    Equal(kinds.Length, ids.Distinct(StringComparer.Ordinal).Count());
    Equal("REFILL_COLLECTED", RefillEventIds.Collected);
    Equal("REFILL_RESTORE_ISSUED", RefillEventIds.RestoreIssued);
    Equal("REFILL_RESPAWNED", RefillEventIds.Respawned);
}

static void Replay()
{
    static string Run()
    {
        var world = new SimulationWorld();
        var refill = Create();
        var rows = new List<string>();
        for (var i = 0; i < 9; i++)
        {
            var input = i is 0 or 5 ? new RefillInput(Contact()) : RefillInput.None;
            rows.Add(Format(refill.Step(input, world)));
        }
        return string.Join("|", rows);
    }
    Equal(Run(), Run());
}

static void TwoRefillIsolation()
{
    var world = new SimulationWorld();
    var first = Create("first");
    var second = Create("second");
    world.Step(current =>
    {
        _ = first.Update(new RefillInput(Contact()), current);
        _ = second.Update(RefillInput.None, current);
    });
    Equal(RefillState.Cooldown, first.State);
    Equal(RefillState.Available, second.State);
}

static void SpringIsolation()
{
    var world = new SimulationWorld();
    var refill = Create();
    var spring = new SpringController("spring", new SimPoint(0, 0), SpringOrientation.Up);
    SpringSnapshot? springSnapshot = null;
    world.Step(current =>
    {
        _ = refill.Update(RefillInput.Disabled, current);
        springSnapshot = spring.Update(new SpringInput(new SpringContact("player", SpringTargetKind.Player, new SimPoint(0, 0), SimVector.Zero)), current);
    });
    Assert(springSnapshot!.LaunchEffect is not null, "Disabled Refill affected Spring.");
}

static void PlayerDashRestoration()
{
    var snapshot = ApplyRefillToPlayer(out _);
    Equal(1, snapshot.Dashes);
}

static void PlayerStaminaRestoration()
{
    var snapshot = ApplyRefillToPlayer(out _);
    Equal(110m, snapshot.Stamina);
}

static void PlayerResourceClamp()
{
    var (world, player) = CreatePlayer();
    var snapshot = player.Step(
        new PlayerInput(0, 0, false, false),
        new PlayerExternalEffects(null, resources: new ExternalResourceEffect(8, 999m)),
        world);
    Equal(1, snapshot.Dashes);
    Equal(110m, snapshot.Stamina);
}

static void PlayerResourceEvent()
{
    var snapshot = ApplyRefillToPlayer(out _);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerTraversalEventKind.ExternalResourcesApplied),
        "Player did not record external resource application.");
}

static void InvalidRespawnTicks() =>
    Throws<ArgumentOutOfRangeException>(() => Create(tuning: new RefillTuning(0)));

static void SurfaceIsolation()
{
    var assembly = typeof(RefillController).Assembly;
    var forbidden = new[] { "System.IO", "System.Windows", "CelesteDesktop.Rendering", "CelesteDesktop.Desktop", "CelesteDesktop.Player", "CelesteDesktop.Entity.Spring" };
    var references = assembly.GetReferencedAssemblies().Select(item => item.Name ?? string.Empty).ToArray();
    Assert(!references.Any(name => forbidden.Any(item => name.StartsWith(item, StringComparison.Ordinal))),
        "Refill assembly references a forbidden layer.");
    foreach (var type in assembly.GetExportedTypes())
    {
        foreach (var memberType in PublicMemberTypes(type))
        {
            Assert(!forbidden.Any(item => (memberType.Namespace ?? string.Empty).StartsWith(item, StringComparison.Ordinal)),
                $"Forbidden public type {memberType.FullName}.");
        }
    }
}

static PlayerTraversalSnapshot ApplyRefillToPlayer(out RefillSnapshot refillSnapshot)
{
    var (world, player) = CreatePlayer();
    _ = player.Step(
        new PlayerInput(0, 0, false, false),
        new PlayerExternalEffects(null, resources: new ExternalResourceEffect(0, 25m)),
        world);
    var refill = Create();
    PlayerTraversalSnapshot? playerSnapshot = null;
    RefillSnapshot? localRefill = null;
    world.Step(current =>
    {
        localRefill = refill.Update(new RefillInput(Contact("player", 0, 1, 25m)), current);
        playerSnapshot = player.Update(
            new PlayerInput(0, 0, false, false),
            new PlayerExternalEffects(null, resources: localRefill.RestoreEffect!.Resources),
            current);
    });
    refillSnapshot = localRefill!;
    return playerSnapshot!;
}

static (SimulationWorld World, PlayerTraversalController Player) CreatePlayer()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 1, 1);
    world.Add(actor);
    return (world, new PlayerTraversalController(actor));
}

static (SimulationWorld World, RefillController Refill, RefillSnapshot Snapshot) ReachRespawn()
{
    var world = new SimulationWorld();
    var refill = Create();
    _ = refill.Step(new RefillInput(Contact()), world);
    _ = refill.Step(RefillInput.None, world);
    _ = refill.Step(RefillInput.None, world);
    var snapshot = refill.Step(RefillInput.None, world);
    return (world, refill, snapshot);
}

static RefillController Create(string id = "refill", RefillTuning? tuning = null) =>
    new(id, new SimPoint(4, 5), tuning ?? new RefillTuning(3));

static RefillContact Contact(
    string id = "player",
    int dashes = 0,
    int maximumDashes = 1,
    decimal stamina = 50m,
    decimal maximumStamina = 110m,
    bool canActivate = true) =>
    new(id, new SimPoint(4, 5), dashes, maximumDashes, stamina, maximumStamina, canActivate);

static string Format(RefillSnapshot snapshot) =>
    $"{snapshot.Tick}:{snapshot.State}:{snapshot.RespawnTicksRemaining}:{snapshot.Armed}:{snapshot.RestoreEffect?.TargetId}:{string.Join(',', snapshot.Events.Select(item => item.EventId))}";

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

static void Has(RefillSnapshot snapshot, RefillEventKind kind) =>
    Assert(snapshot.Events.Any(item => item.Kind == kind), $"Missing Refill event {kind}.");

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
