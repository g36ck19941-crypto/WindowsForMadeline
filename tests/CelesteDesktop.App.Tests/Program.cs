using System.Reflection;
using CelesteDesktop.Animation;
using CelesteDesktop.App;
using CelesteDesktop.Contracts.Assets;
using CelesteDesktop.Entity.Bumper;
using CelesteDesktop.Entity.Glider;
using CelesteDesktop.Entity.Puffer;
using CelesteDesktop.Entity.Refill;
using CelesteDesktop.Entity.Seeker;
using CelesteDesktop.Entity.Spring;
using CelesteDesktop.Entity.Theo;
using CelesteDesktop.Entity.Water;
using CelesteDesktop.Player;
using CelesteDesktop.Rendering;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("initial lifecycle is created", InitialLifecycleIsCreated),
    ("start enters running", StartEntersRunning),
    ("start initializes presentation", StartInitializesPresentation),
    ("start diagnostic is explicit", StartDiagnosticIsExplicit),
    ("double start is rejected", DoubleStartRejected),
    ("pause enters paused", PauseEntersPaused),
    ("pause diagnostic is explicit", PauseDiagnosticIsExplicit),
    ("paused tick is rejected", PausedTickRejected),
    ("resume enters running", ResumeEntersRunning),
    ("resume diagnostic is explicit", ResumeDiagnosticIsExplicit),
    ("stop enters stopped", StopEntersStopped),
    ("stop is idempotent", StopIsIdempotent),
    ("stop before start is rejected", StopBeforeStartRejected),
    ("stopped tick is rejected", StoppedTickRejected),
    ("dispose enters disposed", DisposeEntersDisposed),
    ("dispose is idempotent", DisposeIsIdempotent),
    ("disposed operations are rejected", DisposedOperationsRejected),
    ("presentation start failure is isolated", PresentationStartFailureIsIsolated),
    ("ticks must be consecutive", TicksMustBeConsecutive),
    ("animation tick must match app tick", AnimationTickMustMatch),
    ("one app tick advances world once", AppTickAdvancesWorldOnce),
    ("all component snapshots share one tick", ComponentSnapshotsShareTick),
    ("player receives generated input", PlayerReceivesInput),
    ("spring routes velocity to player", SpringRoutesToPlayer),
    ("spring routes velocity to Theo", SpringRoutesToTheo),
    ("spring routes velocity to Glider", SpringRoutesToGlider),
    ("refill routes resources to player", RefillRoutesResources),
    ("water routes velocity to player", WaterRoutesVelocity),
    ("bumper routes velocity to player", BumperRoutesVelocity),
    ("puffer routes velocity to player", PufferRoutesVelocity),
    ("glider holder effect limits player fall", GliderLimitsPlayerFall),
    ("seeker hit stays an observed fact", SeekerHitIsObservedFact),
    ("presentation runs after simulation", PresentationRunsAfterSimulation),
    ("presentation runs once per tick", PresentationRunsOncePerTick),
    ("presentation throw is isolated", PresentationThrowIsIsolated),
    ("failed presentation stays disabled", FailedPresentationStaysDisabled),
    ("simulation continues after presentation failure", SimulationContinuesAfterPresentationFailure),
    ("failed presentation result is disabled", FailedPresentationResultIsDisabled),
    ("velocity conflicts are explicit and first wins", VelocityConflictIsExplicit),
    ("unknown effect target is explicit", UnknownEffectTargetIsExplicit),
    ("Theo failure disables only Theo", TheoFailureIsIsolated),
    ("disabled Theo remains frozen", DisabledTheoRemainsFrozen),
    ("Glider failure disables only Glider", GliderFailureIsIsolated),
    ("component failure captures exception detail", ComponentFailureCapturesDetail),
    ("diagnostic sequences are monotonic", DiagnosticSequencesAreMonotonic),
    ("snapshot events are immutable", SnapshotEventsAreImmutable),
    ("disabled component collection is immutable", DisabledComponentsAreImmutable),
    ("same app inputs replay deterministically", ReplayIsDeterministic),
    ("real animation stage connects rendering", RealAnimationStageConnectsRendering),
    ("public surface has no file GUI desktop or live input dependency", PublicSurfaceIsIsolated)
};

var failed = 0;
foreach (var test in tests)
{
    try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception exception) { failed++; Console.Error.WriteLine($"FAIL {test.Name}\n{exception}"); }
}
Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void InitialLifecycleIsCreated() { using var f = new Fixture(); Equal(AppLifecycleState.Created, f.Session.State); Equal(0L, f.Session.Tick); }
static void StartEntersRunning() { using var f = new Fixture(); f.Session.Start(); Equal(AppLifecycleState.Running, f.Session.State); }
static void StartInitializesPresentation() { using var f = new Fixture(); f.Session.Start(); Equal(1, f.Presentation.StartCalls); }
static void StartDiagnosticIsExplicit() { using var f = new Fixture(); f.Session.Start(); Assert(f.Diagnostics.Any(x => x.EventId == AppEventIds.Started), "Start diagnostic missing."); }
static void DoubleStartRejected() { using var f = Started(); Throws<InvalidOperationException>(f.Session.Start); }
static void PauseEntersPaused() { using var f = Started(); f.Session.Pause(); Equal(AppLifecycleState.Paused, f.Session.State); }
static void PauseDiagnosticIsExplicit() { using var f = Started(); f.Session.Pause(); Assert(f.Diagnostics.Any(x => x.EventId == AppEventIds.Paused), "Pause diagnostic missing."); }
static void PausedTickRejected() { using var f = Started(); f.Session.Pause(); Throws<InvalidOperationException>(() => f.Session.Step(Input(1))); }
static void ResumeEntersRunning() { using var f = Started(); f.Session.Pause(); f.Session.Resume(); Equal(AppLifecycleState.Running, f.Session.State); }
static void ResumeDiagnosticIsExplicit() { using var f = Started(); f.Session.Pause(); f.Session.Resume(); Assert(f.Diagnostics.Any(x => x.EventId == AppEventIds.Resumed), "Resume diagnostic missing."); }
static void StopEntersStopped() { using var f = Started(); f.Session.Stop(); Equal(AppLifecycleState.Stopped, f.Session.State); Equal(1, f.Presentation.StopCalls); }
static void StopIsIdempotent() { using var f = Started(); f.Session.Stop(); f.Session.Stop(); Equal(1, f.Presentation.StopCalls); }
static void StopBeforeStartRejected() { using var f = new Fixture(); Throws<InvalidOperationException>(f.Session.Stop); }
static void StoppedTickRejected() { using var f = Started(); f.Session.Stop(); Throws<InvalidOperationException>(() => f.Session.Step(Input(1))); }
static void DisposeEntersDisposed() { var f = Started(); f.Session.Dispose(); Equal(AppLifecycleState.Disposed, f.Session.State); Equal(1, f.Presentation.DisposeCalls); }
static void DisposeIsIdempotent() { var f = Started(); f.Session.Dispose(); f.Session.Dispose(); Equal(1, f.Presentation.DisposeCalls); }
static void DisposedOperationsRejected() { var f = new Fixture(); f.Session.Dispose(); Throws<ObjectDisposedException>(f.Session.Start); Throws<ObjectDisposedException>(f.Session.Pause); }

static void PresentationStartFailureIsIsolated()
{
    var stage = new RecordingPresentation { ThrowOnStart = true };
    using var f = new Fixture(stage);
    f.Session.Start();
    Equal(AppLifecycleState.Running, f.Session.State);
    Assert(f.Session.DisabledComponents.Contains(OfflineAppSession.PresentationComponent), "Presentation was not disabled.");
    Equal(1L, f.Session.Step(Input(1)).Tick);
}

static void TicksMustBeConsecutive() { using var f = Started(); Throws<InvalidOperationException>(() => f.Session.Step(Input(2))); }
static void AnimationTickMustMatch() => Throws<ArgumentException>(() => new OfflineAppTickInput(1, PlayerNone(), Animation(2)));
static void AppTickAdvancesWorldOnce() { using var f = Started(); _ = f.Session.Step(Input(1)); Equal(1L, f.Session.Tick); _ = f.Session.Step(Input(2)); Equal(2L, f.Session.Tick); }

static void ComponentSnapshotsShareTick()
{
    using var f = Started();
    var s = f.Session.Step(Input(1));
    foreach (var tick in new[] { s.Player.Tick, s.Theo!.Tick, s.Glider!.Tick, s.Spring!.Tick, s.Refill!.Tick, s.Water!.Tick, s.Bumper!.Tick, s.Puffer!.Tick, s.Seeker!.Tick }) Equal(1L, tick);
}

static void PlayerReceivesInput()
{
    using var f = Started();
    var s = f.Session.Step(Input(1, player: new PlayerInput(1, 0, false, false)));
    Assert(s.Player.Speed.X > 0m, "Player horizontal input was not applied.");
}

static void SpringRoutesToPlayer()
{
    using var f = Started();
    var spring = new SpringInput(new SpringContact("player", SpringTargetKind.Player, new SimPoint(0, 0), SimVector.Zero));
    var s = f.Session.Step(Input(1, spring: spring));
    Assert(s.Player.Speed.Y < -100m, "Spring velocity was not applied to Player.");
    Assert(s.Events.Any(x => x.EventId == AppEventIds.EffectRouted && x.TargetId == "player"), "Route diagnostic missing.");
}

static void SpringRoutesToTheo()
{
    using var f = Started();
    var spring = new SpringInput(new SpringContact("theo", SpringTargetKind.Theo, new SimPoint(30, 0), SimVector.Zero));
    var s = f.Session.Step(Input(1, spring: spring));
    Assert(s.Theo!.Speed.Y < -100m, "Spring velocity was not applied to Theo.");
    Assert(Math.Abs(s.Player.Speed.Y) < 100m, "Theo-targeted velocity leaked to Player.");
}

static void SpringRoutesToGlider()
{
    using var f = Started();
    var spring = new SpringInput(new SpringContact("glider", SpringTargetKind.Glider, new SimPoint(60, 0), SimVector.Zero));
    var s = f.Session.Step(Input(1, spring: spring));
    Assert(s.Glider!.Speed.Y < -100m, "Spring velocity was not applied to Glider.");
}

static void RefillRoutesResources()
{
    using var f = Started();
    _ = f.Session.Step(Input(1, player: new PlayerInput(1, 0, false, false, dashPressed: true)));
    var contact = new RefillContact("player", new SimPoint(0, 0), 0, 1, 20m, 110m);
    var s = f.Session.Step(Input(2, refill: new RefillInput(contact)));
    Equal(1, s.Player.Dashes);
    Assert(s.Player.Events.Any(x => x.Kind == PlayerTraversalEventKind.ExternalResourcesApplied), "Resource application event missing.");
}

static void WaterRoutesVelocity()
{
    using var f = Started();
    var contact = new WaterContact("player", new SimRect(0, 0, 8, 8), SimVector.Zero, 1, -1);
    var s = f.Session.Step(Input(1, water: new WaterInput([contact])));
    Assert(s.Player.Speed.X > 0m && s.Player.Speed.Y < 0m, "Water velocity was not applied.");
}

static void BumperRoutesVelocity()
{
    using var f = Started();
    var s = f.Session.Step(Input(1, bumper: new BumperInput(new BumperContact("player", new SimPoint(1, 0)))));
    Assert(s.Player.Speed.X > 100m, "Bumper velocity was not applied.");
}

static void PufferRoutesVelocity()
{
    using var f = Started();
    var contact = new PufferContact("player", new SimVector(1m, 0m));
    _ = f.Session.Step(Input(1, puffer: new PufferInput(contact)));
    var s = f.Session.Step(Input(2, puffer: new PufferInput(contact)));
    Assert(s.Player.Speed.X > 100m, "Puffer velocity was not applied.");
}

static void GliderLimitsPlayerFall()
{
    using var f = Started();
    var holder = new GliderHolderSnapshot("player", new SimPoint(60, 0), 1, holderVerticalSpeed: 500m);
    _ = f.Session.Step(Input(1,
        glider: new GliderInput(GliderAction.Pickup, holder),
        bumper: new BumperInput(new BumperContact("player", new SimPoint(0, 1)))));
    var s = f.Session.Step(Input(2, glider: new GliderInput(GliderAction.None, holder), player: PlayerNone()));
    Assert(s.Player.Speed.Y <= f.Components.Glider.Tuning.MaximumHolderFallSpeed, "Glider fall limit was not routed.");
}

static void SeekerHitIsObservedFact()
{
    using var f = Started();
    var target = new SeekerTarget("player", new SimVector(101m, 0m));
    _ = f.Session.Step(Input(1, seeker: new SeekerInput(target)));
    _ = f.Session.Step(Input(2, seeker: new SeekerInput(target)));
    _ = f.Session.Step(Input(3, seeker: new SeekerInput(target)));
    _ = f.Session.Step(Input(4, seeker: new SeekerInput(target)));
    var s = f.Session.Step(Input(5, seeker: new SeekerInput(new SeekerTarget("player", target.Center, isTouching: true))));
    Assert(s.Seeker!.HitEffect is not null, "Seeker hit fact missing.");
    Assert(s.Events.Any(x => x.EventId == AppEventIds.SeekerHitObserved && x.Detail == "hit-fact-only"), "Seeker hit diagnostic missing.");
    Equal(AppLifecycleState.Running, s.Lifecycle);
}

static void PresentationRunsAfterSimulation()
{
    Fixture? fixture = null;
    var stage = new RecordingPresentation { OnPresent = input => Equal(input.Tick, fixture!.Session.Tick) };
    using (fixture = Started(stage)) _ = fixture.Session.Step(Input(1));
}

static void PresentationRunsOncePerTick() { using var f = Started(); _ = f.Session.Step(Input(1)); _ = f.Session.Step(Input(2)); Equal(2, f.Presentation.PresentCalls); }

static void PresentationThrowIsIsolated()
{
    var stage = new RecordingPresentation { ThrowOnPresentCall = 1 };
    using var f = Started(stage);
    var s = f.Session.Step(Input(1));
    Equal(AppLifecycleState.Running, s.Lifecycle);
    Assert(s.DisabledComponents.Contains(OfflineAppSession.PresentationComponent), "Presentation not isolated.");
}

static void FailedPresentationStaysDisabled()
{
    var stage = new RecordingPresentation { ThrowOnPresentCall = 1 };
    using var f = Started(stage);
    _ = f.Session.Step(Input(1)); _ = f.Session.Step(Input(2));
    Equal(1, stage.PresentCalls);
}

static void SimulationContinuesAfterPresentationFailure()
{
    var stage = new RecordingPresentation { ThrowOnPresentCall = 1 };
    using var f = Started(stage);
    _ = f.Session.Step(Input(1)); var s = f.Session.Step(Input(2, player: new PlayerInput(1, 0, false, false)));
    Equal(2L, s.Simulation.Tick); Assert(s.Player.Speed.X > 0m, "Simulation stopped after presentation failure.");
}

static void FailedPresentationResultIsDisabled()
{
    var stage = new RecordingPresentation { ReturnFailure = true };
    using var f = Started(stage);
    var s = f.Session.Step(Input(1));
    Assert(s.DisabledComponents.Contains(OfflineAppSession.PresentationComponent), "Failed result did not disable presentation.");
}

static void VelocityConflictIsExplicit()
{
    using var f = Started();
    var spring = new SpringInput(new SpringContact("player", SpringTargetKind.Player, new SimPoint(0, 0), SimVector.Zero));
    var water = new WaterInput([new WaterContact("player", new SimRect(0, 0, 8, 8), SimVector.Zero, 1, 1)]);
    var s = f.Session.Step(Input(1, spring: spring, water: water));
    Assert(s.Events.Any(x => x.EventId == AppEventIds.EffectConflict && x.Detail == "y"), "Velocity conflict not recorded.");
    Assert(s.Player.Speed.Y < -100m, "First routed Y velocity did not win.");
}

static void UnknownEffectTargetIsExplicit()
{
    using var f = Started();
    var s = f.Session.Step(Input(1, bumper: new BumperInput(new BumperContact("missing", new SimPoint(1, 0)))));
    Assert(s.Events.Any(x => x.EventId == AppEventIds.EffectTargetUnresolved && x.TargetId == "missing"), "Unknown target not recorded.");
}

static void TheoFailureIsIsolated()
{
    using var f = Started();
    var invalid = new TheoCrystalInput(TheoCrystalAction.Drop, new TheoHolderSnapshot("player", new SimPoint(0, 0), 1));
    var s = f.Session.Step(Input(1, theo: invalid, player: new PlayerInput(1, 0, false, false)));
    Assert(s.DisabledComponents.Contains(OfflineAppSession.TheoComponent), "Theo was not disabled.");
    Assert(s.Player.Speed.X > 0m && s.Glider is not null, "Theo failure affected other components.");
}

static void DisabledTheoRemainsFrozen()
{
    using var f = Started();
    var invalid = new TheoCrystalInput(TheoCrystalAction.Drop, new TheoHolderSnapshot("player", new SimPoint(0, 0), 1));
    _ = f.Session.Step(Input(1, theo: invalid));
    var s = f.Session.Step(Input(2, theo: invalid));
    Assert(s.DisabledComponents.Contains(OfflineAppSession.TheoComponent), "Theo disable did not persist.");
    Equal(2L, s.Player.Tick);
}

static void GliderFailureIsIsolated()
{
    using var f = Started();
    var invalid = new GliderInput(GliderAction.Drop, new GliderHolderSnapshot("player", new SimPoint(0, 0), 1));
    var s = f.Session.Step(Input(1, glider: invalid));
    Assert(s.DisabledComponents.Contains(OfflineAppSession.GliderComponent), "Glider was not disabled.");
    Assert(s.Theo is not null && s.Player is not null, "Glider failure affected other components.");
}

static void ComponentFailureCapturesDetail()
{
    using var f = Started();
    var invalid = new TheoCrystalInput(TheoCrystalAction.Throw, new TheoHolderSnapshot("player", new SimPoint(0, 0), 1));
    var s = f.Session.Step(Input(1, theo: invalid));
    var failure = s.Events.Single(x => x.EventId == AppEventIds.ComponentDisabled && x.ComponentId == OfflineAppSession.TheoComponent);
    Assert(failure.Exception is { Type.Length: > 0, Message.Length: > 0, Stack.Length: > 0 }, "Exception detail is incomplete.");
}

static void DiagnosticSequencesAreMonotonic()
{
    using var f = Started(); _ = f.Session.Step(Input(1)); _ = f.Session.Step(Input(2)); f.Session.Stop();
    Equal(f.Diagnostics.Count, f.Diagnostics.Select(x => x.Sequence).Distinct().Count());
    Assert(f.Diagnostics.Zip(f.Diagnostics.Skip(1)).All(x => x.First.Sequence < x.Second.Sequence), "Diagnostic sequence did not increase.");
}

static void SnapshotEventsAreImmutable()
{
    using var f = Started(); var events = f.Session.Step(Input(1)).Events;
    Throws<NotSupportedException>(() => ((IList<AppDiagnosticEvent>)events).Add(events[0]));
}

static void DisabledComponentsAreImmutable()
{
    var stage = new RecordingPresentation { ThrowOnStart = true };
    using var f = Started(stage); var disabled = f.Session.Step(Input(1)).DisabledComponents;
    Throws<NotSupportedException>(() => ((IList<string>)disabled).Add("x"));
}

static void ReplayIsDeterministic()
{
    static string Run()
    {
        using var f = Started(); var rows = new List<string>();
        for (var tick = 1; tick <= 5; tick++)
        {
            var spring = tick == 1 ? new SpringInput(new SpringContact("player", SpringTargetKind.Player, new SimPoint(0, 0), SimVector.Zero)) : SpringInput.None;
            var s = f.Session.Step(Input(tick, player: new PlayerInput(tick % 2, 0, false, false), spring: spring));
            rows.Add($"{s.Tick}:{s.Player.Position.X}:{s.Player.Position.Y}:{s.Player.Speed.X}:{s.Player.Speed.Y}:{string.Join(',', s.DisabledComponents)}");
        }
        return string.Join('|', rows);
    }
    Equal(Run(), Run());
}

static void RealAnimationStageConnectsRendering()
{
    var backend = new RecordingBackend();
    var entity = new EntityAssetCatalog("player", "idle", new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0), null,
        [new CatalogAnimationDescriptor("idle", 0.1, true, null, [new CatalogFrameDescriptor("idle00", SharedFrame.Value)])]);
    using var stage = new OfflineAnimationAppPresentationStage(entity, new RecordingFactory(backend), new PresentationGeometry(0, 0, 2, 2, 96, 96), 2, 2, _ => { });
    stage.Start(); var result = stage.Present(new AnimationTickInput(1, "idle", 0, 0)); stage.Stop();
    Assert(result.Presentation.Succeeded, "Real presentation stage failed.");
    Equal(SharedFrame.Value.ContentSha256, backend.LastFingerprint);
}

static void PublicSurfaceIsIsolated()
{
    var text = string.Join(' ', typeof(OfflineAppSession).Assembly.GetExportedTypes().SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Desktop.Windows", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Install" })
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
}

static Fixture Started(RecordingPresentation? stage = null) { var fixture = new Fixture(stage); fixture.Session.Start(); return fixture; }

static OfflineAppTickInput Input(
    long tick,
    PlayerInput? player = null,
    TheoCrystalInput? theo = null,
    GliderInput? glider = null,
    SpringInput? spring = null,
    RefillInput? refill = null,
    WaterInput? water = null,
    BumperInput? bumper = null,
    PufferInput? puffer = null,
    SeekerInput? seeker = null) => new(
        tick,
        player ?? PlayerNone(),
        Animation(tick),
        theo ?? TheoCrystalInput.None,
        glider ?? GliderInput.None,
        spring ?? SpringInput.None,
        refill ?? RefillInput.None,
        water ?? WaterInput.None,
        bumper ?? BumperInput.None,
        puffer ?? PufferInput.None,
        seeker ?? SeekerInput.None);

static AnimationTickInput Animation(long tick) => new(tick, "idle", 0, 0);
static PlayerInput PlayerNone() => new(0, 0, false, false);
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

internal static class SharedFrame
{
    public static Bgra32Frame Value { get; } = new(
        2,
        2,
        Enumerable.Repeat(new byte[] { 32, 64, 128, 255 }, 4).SelectMany(x => x).ToArray());
}

internal sealed class Fixture : IDisposable
{
    public Fixture(RecordingPresentation? presentation = null)
    {
        Presentation = presentation ?? new RecordingPresentation();
        var world = new SimulationWorld();
        var playerActor = new Actor("player", 0, 0, 8, 8);
        var theoActor = new Actor("theo", 30, 0, 8, 8);
        var gliderActor = new Actor("glider", 60, 0, 8, 8);
        world.Add(playerActor); world.Add(theoActor); world.Add(gliderActor);
        Components = new OfflineAppComponents(
            world,
            new PlayerTraversalController(playerActor),
            new TheoCrystalController(theoActor),
            new GliderController(gliderActor),
            new SpringController("spring", new SimPoint(0, 0), SpringOrientation.Up, new SpringTuning(240m, 2, 2)),
            new RefillController("refill", new SimPoint(0, 0), new RefillTuning(2)),
            new WaterController("water", new SimRect(-10, -10, 30, 30)),
            new BumperController("bumper", new SimPoint(0, 0), new BumperTuning(16, 240m, 2)),
            new PufferController("puffer", new SimVector(0m, 0m), -10m, 10m, tuning: new PufferTuning(30m, 16, 1, 240m, 2)),
            new SeekerController("seeker", new SimVector(100m, 0m), 90m, 110m, tuning: new SeekerTuning(24m, 72m, 240m, 64, 20, 1, 1, 2, 2, 2)));
        Session = new OfflineAppSession(Components, Presentation, Diagnostics.Add);
    }

    public OfflineAppComponents Components { get; }
    public RecordingPresentation Presentation { get; }
    public List<AppDiagnosticEvent> Diagnostics { get; } = [];
    public OfflineAppSession Session { get; }
    public void Dispose() => Session.Dispose();
}

internal sealed class RecordingPresentation : IAppPresentationStage
{
    public bool ThrowOnStart { get; set; }
    public int ThrowOnPresentCall { get; set; }
    public bool ReturnFailure { get; set; }
    public bool ThrowOnStop { get; set; }
    public bool ThrowOnDispose { get; set; }
    public Action<AnimationTickInput>? OnPresent { get; set; }
    public int StartCalls { get; private set; }
    public int PresentCalls { get; private set; }
    public int StopCalls { get; private set; }
    public int DisposeCalls { get; private set; }

    public void Start() { StartCalls++; if (ThrowOnStart) throw new InvalidOperationException("synthetic start failure"); }
    public OfflineAnimationPresentationResult Present(AnimationTickInput input)
    {
        PresentCalls++; OnPresent?.Invoke(input);
        if (ThrowOnPresentCall == PresentCalls) throw new InvalidOperationException("synthetic presentation failure", new ApplicationException("inner"));
        var snapshot = new AnimationFrameSnapshot(input.Tick, "player", "idle", 0, "idle00", ProgramFrame.Value, new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0), null, false);
        return new OfflineAnimationPresentationResult(snapshot, ProgramFrame.Value,
            new RenderPresentationResult(!ReturnFailure, false, true, ProgramFrame.Value.ContentSha256, PresentCalls, ReturnFailure ? "SYNTHETIC_FAILURE" : null));
    }
    public void Stop() { StopCalls++; if (ThrowOnStop) throw new InvalidOperationException("synthetic stop failure"); }
    public void Dispose() { DisposeCalls++; if (ThrowOnDispose) throw new InvalidOperationException("synthetic dispose failure"); }

    private static class ProgramFrame
    {
        public static Bgra32Frame Value { get; } = new(2, 2, Enumerable.Repeat(new byte[] { 32, 64, 128, 255 }, 4).SelectMany(x => x).ToArray());
    }
}

internal sealed class RecordingFactory(RecordingBackend backend) : IRenderPresenterBackendFactory
{
    public IRenderPresenterBackend Create() => backend;
}

internal sealed class RecordingBackend : IRenderPresenterBackend
{
    public string Name => "Recording";
    public string? LastFingerprint { get; private set; }
    public void Initialize(PresentationGeometry geometry) { }
    public void Upload(Bgra32Frame frame) => LastFingerprint = frame.ContentSha256;
    public void Submit() { }
    public void WaitForPresented() { }
    public void Dispose() { }
}
