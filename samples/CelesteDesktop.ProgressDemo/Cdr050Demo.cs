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

internal static class Cdr050Demo
{
    public static DemoApp Run()
    {
        var first = RunOnce();
        var second = RunOnce();
        return first with { DeterministicReplay = first.Signature == second.Signature };
    }

    private static DemoApp RunOnce()
    {
        var world = new SimulationWorld();
        var playerActor = new Actor("app-player", 0, 0, 8, 8);
        var theoActor = new Actor("app-theo", 30, 0, 8, 8);
        var gliderActor = new Actor("app-glider", 60, 0, 8, 8);
        world.Add(playerActor);
        world.Add(theoActor);
        world.Add(gliderActor);

        var components = new OfflineAppComponents(
            world,
            new PlayerTraversalController(playerActor),
            new TheoCrystalController(theoActor),
            new GliderController(gliderActor),
            new SpringController("app-spring", new SimPoint(0, 0), SpringOrientation.Up, new SpringTuning(240m, 2, 2)),
            new RefillController("app-refill", new SimPoint(0, 0), new RefillTuning(2)),
            new WaterController("app-water", new SimRect(-10, -10, 30, 30)),
            new BumperController("app-bumper", new SimPoint(0, 0), new BumperTuning(16, 240m, 2)),
            new PufferController("app-puffer", new SimVector(0, 0), -10m, 10m, tuning: new PufferTuning(30m, 16, 1, 240m, 2)),
            new SeekerController("app-seeker", new SimVector(100m, 0), 90m, 110m, tuning: new SeekerTuning(24m, 72m, 240m, 64, 20, 1, 1, 2, 2, 2)));

        var diagnostics = new List<AppDiagnosticEvent>();
        using var presentation = new DemoAppPresentationStage(failOnCall: 2);
        using var session = new OfflineAppSession(components, presentation, diagnostics.Add);
        session.Start();

        var rows = new List<DemoAppRow>();
        rows.Add(Row(session.Step(Input(1,
            spring: new SpringInput(new SpringContact("app-player", SpringTargetKind.Player, new SimPoint(0, 0), SimVector.Zero))))));
        rows.Add(Row(session.Step(Input(2, player: new PlayerInput(1, 0, false, false, dashPressed: true)))));
        session.Pause();
        session.Resume();
        rows.Add(Row(session.Step(Input(3,
            refill: new RefillInput(new RefillContact("app-player", new SimPoint(0, 0), 0, 1, 20m, 110m))))));
        rows.Add(Row(session.Step(Input(4, player: new PlayerInput(1, 0, false, false)))));
        session.Stop();

        var signature = string.Join('|', rows.Select(row =>
            $"{row.Tick}:{row.WorldTick}:{row.PlayerX}:{row.PlayerY}:{row.PlayerSpeedX}:{row.PlayerSpeedY}:{row.EventIds}:{row.DisabledComponents}"));
        return new DemoApp(
            rows,
            diagnostics.Count(item => item.EventId == AppEventIds.SimulationCompleted),
            diagnostics.Count(item => item.EventId == AppEventIds.EffectRouted),
            presentation.PresentCalls,
            diagnostics.Count(item => item.EventId == AppEventIds.ComponentDisabled),
            diagnostics.Any(item => item.EventId == AppEventIds.Paused),
            diagnostics.Any(item => item.EventId == AppEventIds.Resumed),
            session.State.ToString(),
            signature,
            false);
    }

    private static OfflineAppTickInput Input(
        long tick,
        PlayerInput? player = null,
        SpringInput? spring = null,
        RefillInput? refill = null) => new(
            tick,
            player ?? new PlayerInput(0, 0, false, false),
            new AnimationTickInput(tick, "idle", 0, 0),
            TheoCrystalInput.None,
            GliderInput.None,
            spring ?? SpringInput.None,
            refill ?? RefillInput.None,
            WaterInput.None,
            BumperInput.None,
            PufferInput.None,
            SeekerInput.None);

    private static DemoAppRow Row(OfflineAppSnapshot snapshot) => new(
        snapshot.Tick,
        snapshot.Simulation.Tick,
        snapshot.Player.Position.X,
        snapshot.Player.Position.Y,
        snapshot.Player.Speed.X,
        snapshot.Player.Speed.Y,
        string.Join(',', snapshot.Events.Select(item => item.EventId)),
        string.Join(',', snapshot.DisabledComponents));
}

internal sealed record DemoApp(
    IReadOnlyList<DemoAppRow> Rows,
    int SimulationCompletedCount,
    int EffectRoutedCount,
    int PresentationCalls,
    int ComponentDisabledCount,
    bool Paused,
    bool Resumed,
    string FinalLifecycle,
    string Signature,
    bool DeterministicReplay);

internal sealed record DemoAppRow(
    long Tick,
    long WorldTick,
    decimal PlayerX,
    decimal PlayerY,
    decimal PlayerSpeedX,
    decimal PlayerSpeedY,
    string EventIds,
    string DisabledComponents);

internal sealed class DemoAppPresentationStage(int failOnCall) : IAppPresentationStage
{
    private static readonly Bgra32Frame Frame = new(
        2,
        2,
        Enumerable.Repeat(new byte[] { 32, 64, 128, 255 }, 4).SelectMany(value => value).ToArray());

    public int PresentCalls { get; private set; }
    public void Start() { }
    public OfflineAnimationPresentationResult Present(AnimationTickInput input)
    {
        PresentCalls++;
        if (PresentCalls == failOnCall)
            throw new InvalidOperationException("program-generated presentation isolation probe");
        var snapshot = new AnimationFrameSnapshot(
            input.Tick,
            "player",
            "idle",
            0,
            "idle00",
            Frame,
            new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0),
            null,
            false);
        return new OfflineAnimationPresentationResult(
            snapshot,
            Frame,
            new RenderPresentationResult(true, false, true, Frame.ContentSha256, PresentCalls, null));
    }
    public void Stop() { }
    public void Dispose() { }
}
