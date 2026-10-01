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
    ("default catch-up limit is four", DefaultCatchUpLimit),
    ("zero catch-up limit is rejected", ZeroCatchUpRejected),
    ("excessive catch-up limit is rejected", ExcessiveCatchUpRejected),
    ("nonpositive clock frequency is rejected", NonpositiveClockFrequencyRejected),
    ("host requires a new session", HostRequiresNewSession),
    ("host can run only once", HostRunsOnce),
    ("maximum ticks must be positive", MaximumTicksPositive),
    ("one host tick advances one App tick", OneHostTick),
    ("four host ticks advance four App ticks", FourHostTicks),
    ("bounded run stops App", BoundedRunStopsApp),
    ("lifecycle diagnostics are explicit", LifecycleDiagnostics),
    ("diagnostic sequences are monotonic", DiagnosticSequencesMonotonic),
    ("input ticks are consecutive", InputTicksConsecutive),
    ("host waits before first tick", WaitsBeforeFirstTick),
    ("pre-cancelled run is contained", PreCancelledRunContained),
    ("cancel diagnostic is explicit", CancelDiagnosticExplicit),
    ("catch-up is bounded", CatchUpIsBounded),
    ("dropped backlog is explicit", DroppedBacklogExplicit),
    ("normal cadence drops nothing", NormalCadenceDropsNothing),
    ("null input faults host", NullInputFaultsHost),
    ("wrong input tick faults host", WrongInputTickFaultsHost),
    ("host failure captures exception detail", FailureCapturesDetail),
    ("presentation failure does not stop host ticks", PresentationFailureIsIsolated),
    ("dispose releases App session", DisposeReleasesSession),
    ("same clock and inputs replay deterministically", ReplayDeterministically),
    ("public host surface has no GUI desktop install or live input", PublicSurfaceIsIsolated)
};

var failed = 0;
foreach (var test in tests)
{
    try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception exception) { failed++; Console.Error.WriteLine($"FAIL {test.Name}\n{exception}"); }
}
Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void DefaultCatchUpLimit() => Equal(4, new OfflineAppHostOptions().MaximumCatchUpTicks);
static void ZeroCatchUpRejected() => Throws<ArgumentOutOfRangeException>(() => new OfflineAppHostOptions(0));
static void ExcessiveCatchUpRejected() => Throws<ArgumentOutOfRangeException>(() => new OfflineAppHostOptions(61));
static void NonpositiveClockFrequencyRejected() { using var f = new Fixture(clock: new ManualClock(0)); Throws<ArgumentOutOfRangeException>(() => f.CreateHost()); }
static void HostRequiresNewSession() { using var f = new Fixture(); f.Session.Start(); using var host = f.CreateHost(); Throws<InvalidOperationException>(() => Run(host, 1)); }
static void HostRunsOnce() { using var f = new Fixture(); using var host = f.CreateHost(); _ = Run(host, 1); Throws<InvalidOperationException>(() => Run(host, 1)); }
static void MaximumTicksPositive() { using var f = new Fixture(); using var host = f.CreateHost(); Throws<ArgumentOutOfRangeException>(() => Run(host, 0)); }

static void OneHostTick()
{
    using var f = new Fixture(); using var host = f.CreateHost();
    var result = Run(host, 1);
    Equal(1, result.ExecutedTicks); Equal(1L, f.Session.Tick);
}

static void FourHostTicks()
{
    using var f = new Fixture(); using var host = f.CreateHost();
    var result = Run(host, 4);
    Equal(4, result.ExecutedTicks); Equal(4L, f.Session.Tick);
}

static void BoundedRunStopsApp() { using var f = new Fixture(); using var host = f.CreateHost(); var result = Run(host, 2); Equal(AppLifecycleState.Stopped, result.FinalLifecycle); }

static void LifecycleDiagnostics()
{
    using var f = new Fixture(); using var host = f.CreateHost(); var result = Run(host, 2);
    Assert(result.Events.Any(item => item.EventId == AppHostEventIds.Started), "Host start event missing.");
    Assert(result.Events.Any(item => item.EventId == AppHostEventIds.Stopped), "Host stop event missing.");
}

static void DiagnosticSequencesMonotonic()
{
    using var f = new Fixture(); using var host = f.CreateHost(); var result = Run(host, 4);
    Equal(result.Events.Count, result.Events.Select(item => item.Sequence).Distinct().Count());
    Assert(result.Events.Zip(result.Events.Skip(1)).All(pair => pair.First.Sequence < pair.Second.Sequence), "Host sequences are not increasing.");
}

static void InputTicksConsecutive()
{
    var seen = new List<long>(); using var f = new Fixture(inputFactory: tick => { seen.Add(tick); return Input(tick); }); using var host = f.CreateHost();
    _ = Run(host, 4); Assert(seen.SequenceEqual(new long[] { 1, 2, 3, 4 }), "Input ticks were not consecutive.");
}

static void WaitsBeforeFirstTick() { var clock = new ManualClock(); using var f = new Fixture(clock: clock); using var host = f.CreateHost(); _ = Run(host, 1); Equal(1, clock.DelayCalls); }

static void PreCancelledRunContained()
{
    using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    using var f = new Fixture(); using var host = f.CreateHost(); var result = host.RunAsync(null, cancellation.Token).GetAwaiter().GetResult();
    Assert(result.Cancelled, "Cancellation was not reported."); Equal(0, result.ExecutedTicks); Equal(AppLifecycleState.Stopped, result.FinalLifecycle);
}

static void CancelDiagnosticExplicit()
{
    using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    using var f = new Fixture(); using var host = f.CreateHost(); var result = host.RunAsync(null, cancellation.Token).GetAwaiter().GetResult();
    Assert(result.Events.Any(item => item.EventId == AppHostEventIds.Cancelled), "Cancel event missing.");
}

static void CatchUpIsBounded()
{
    var clock = new ManualClock(60, 10); using var f = new Fixture(clock: clock); using var host = f.CreateHost(new OfflineAppHostOptions(4));
    var result = Run(host, 4); Equal(4, result.ExecutedTicks); Equal(6, result.DroppedIntervals);
}

static void DroppedBacklogExplicit()
{
    var clock = new ManualClock(60, 10); using var f = new Fixture(clock: clock); using var host = f.CreateHost(new OfflineAppHostOptions(4));
    var result = Run(host, 4); Assert(result.Events.Any(item => item.EventId == AppHostEventIds.BacklogDropped && item.Detail == "dropped=6"), "Backlog event missing.");
}

static void NormalCadenceDropsNothing() { using var f = new Fixture(); using var host = f.CreateHost(); Equal(0, Run(host, 4).DroppedIntervals); }
static void NullInputFaultsHost() { using var f = new Fixture(inputFactory: _ => null!); using var host = f.CreateHost(); Throws<AppHostException>(() => Run(host, 1)); }
static void WrongInputTickFaultsHost() { using var f = new Fixture(inputFactory: tick => Input(tick + 1)); using var host = f.CreateHost(); Throws<AppHostException>(() => Run(host, 1)); }

static void FailureCapturesDetail()
{
    var events = new List<AppHostDiagnosticEvent>(); using var f = new Fixture(inputFactory: _ => throw new InvalidOperationException("generated input failure", new ApplicationException("inner")));
    using var host = f.CreateHost(diagnostics: events.Add); Throws<AppHostException>(() => Run(host, 1));
    var fault = events.Single(item => item.EventId == AppHostEventIds.Faulted);
    Equal(typeof(InvalidOperationException).FullName, fault.Exception!.Type); Equal("generated input failure", fault.Exception.Message); Equal(typeof(ApplicationException).FullName, fault.Exception.InnerType);
}

static void PresentationFailureIsIsolated()
{
    using var f = new Fixture(presentation: new HostPresentation(throwOnCall: 1)); using var host = f.CreateHost();
    var result = Run(host, 4); Equal(4, result.ExecutedTicks); Equal(4L, f.Session.Tick);
}

static void DisposeReleasesSession()
{
    using var f = new Fixture(); var host = f.CreateHost(); _ = Run(host, 1); host.Dispose(); Equal(AppLifecycleState.Disposed, f.Session.State);
}

static void ReplayDeterministically()
{
    static string Once()
    {
        using var f = new Fixture(); using var host = f.CreateHost(); var result = Run(host, 8);
        return string.Join('|', result.Events.Select(item => $"{item.EventId}:{item.AppTick}:{item.ExecutedTicks}:{item.DroppedIntervals}"));
    }
    Equal(Once(), Once());
}

static void PublicSurfaceIsIsolated()
{
    var text = string.Join(' ', typeof(OfflineAppHost).Assembly.GetExportedTypes().SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Desktop.Windows", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Install" })
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
}

static OfflineAppHostResult Run(OfflineAppHost host, int maximumTicks) => host.RunAsync(maximumTicks).GetAwaiter().GetResult();
static OfflineAppTickInput Input(long tick) => new(tick, new PlayerInput(1, 0, false, false), new AnimationTickInput(tick, "idle", 0, 0));
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

internal sealed class Fixture : IDisposable
{
    private readonly Func<long, OfflineAppTickInput> _inputFactory;

    public Fixture(ManualClock? clock = null, Func<long, OfflineAppTickInput>? inputFactory = null, HostPresentation? presentation = null)
    {
        Clock = clock ?? new ManualClock();
        _inputFactory = inputFactory ?? ProgramInput;
        Presentation = presentation ?? new HostPresentation();
        var world = new SimulationWorld();
        var player = new Actor("player", 0, 0, 8, 8);
        var theo = new Actor("theo", 30, 0, 8, 8);
        var glider = new Actor("glider", 60, 0, 8, 8);
        world.Add(player); world.Add(theo); world.Add(glider);
        Session = new OfflineAppSession(new OfflineAppComponents(
            world,
            new PlayerTraversalController(player),
            new TheoCrystalController(theo),
            new GliderController(glider),
            new SpringController("spring", new SimPoint(0, 0), SpringOrientation.Up, new SpringTuning(240m, 2, 2)),
            new RefillController("refill", new SimPoint(0, 0), new RefillTuning(2)),
            new WaterController("water", new SimRect(-10, -10, 30, 30)),
            new BumperController("bumper", new SimPoint(0, 0), new BumperTuning(16, 240m, 2)),
            new PufferController("puffer", new SimVector(0, 0), -10m, 10m, tuning: new PufferTuning(30m, 16, 1, 240m, 2)),
            new SeekerController("seeker", new SimVector(100m, 0), 90m, 110m, tuning: new SeekerTuning(24m, 72m, 240m, 64, 20, 1, 1, 2, 2, 2))),
            Presentation);
    }

    public ManualClock Clock { get; }
    public HostPresentation Presentation { get; }
    public OfflineAppSession Session { get; }
    public OfflineAppHost CreateHost(OfflineAppHostOptions? options = null, Action<AppHostDiagnosticEvent>? diagnostics = null) =>
        new(Session, Clock, new DelegateAppTickInputSource(_inputFactory), options, diagnostics);
    public void Dispose() { if (Session.State != AppLifecycleState.Disposed) Session.Dispose(); }
    private static OfflineAppTickInput ProgramInput(long tick) => new(tick, new PlayerInput(1, 0, false, false), new AnimationTickInput(tick, "idle", 0, 0));
}

internal sealed class ManualClock(long frequency = 60, long advancePerDelay = 1) : IAppHostClock
{
    public long Frequency { get; } = frequency;
    public long Timestamp { get; private set; }
    public int DelayCalls { get; private set; }
    public long GetTimestamp() => Timestamp;
    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DelayCalls++;
        Timestamp = checked(Timestamp + advancePerDelay);
        return ValueTask.CompletedTask;
    }
}

internal sealed class HostPresentation(int throwOnCall = 0) : IAppPresentationStage
{
    private static readonly Bgra32Frame Frame = new(1, 1, new byte[] { 0, 0, 0, 0 });
    private int _calls;
    public void Start() { }
    public OfflineAnimationPresentationResult Present(AnimationTickInput input)
    {
        _calls++;
        if (_calls == throwOnCall) throw new InvalidOperationException("generated presentation failure");
        var snapshot = new AnimationFrameSnapshot(input.Tick, "player", "idle", 0, "idle00", Frame, new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0), null, false);
        return new OfflineAnimationPresentationResult(snapshot, Frame, new RenderPresentationResult(true, false, true, Frame.ContentSha256, _calls, null));
    }
    public void Stop() { }
    public void Dispose() { }
}
