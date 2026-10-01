using CelesteDesktop.Animation;
using CelesteDesktop.App;
using CelesteDesktop.Entity.Bumper;
using CelesteDesktop.Entity.Glider;
using CelesteDesktop.Entity.Puffer;
using CelesteDesktop.Entity.Refill;
using CelesteDesktop.Entity.Seeker;
using CelesteDesktop.Entity.Spring;
using CelesteDesktop.Entity.Theo;
using CelesteDesktop.Entity.Water;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

internal static class Cdr051Demo
{
    public static DemoHeadlessHost Run()
    {
        var normal = RunOnce(advancePerDelay: 1, maximumTicks: 6);
        var normalReplay = RunOnce(advancePerDelay: 1, maximumTicks: 6);
        var backlog = RunOnce(advancePerDelay: 8, maximumTicks: 4);
        var backlogReplay = RunOnce(advancePerDelay: 8, maximumTicks: 4);
        return new DemoHeadlessHost(
            normal,
            backlog,
            normal.Signature == normalReplay.Signature && backlog.Signature == backlogReplay.Signature);
    }

    private static DemoHostRun RunOnce(long advancePerDelay, int maximumTicks)
    {
        var world = new SimulationWorld();
        var playerActor = new Actor("host-player", 0, 0, 8, 8);
        var theoActor = new Actor("host-theo", 30, 0, 8, 8);
        var gliderActor = new Actor("host-glider", 60, 0, 8, 8);
        world.Add(playerActor); world.Add(theoActor); world.Add(gliderActor);
        using var presentation = new DemoAppPresentationStage(failOnCall: 0);
        using var session = new OfflineAppSession(new OfflineAppComponents(
            world,
            new PlayerTraversalController(playerActor),
            new TheoCrystalController(theoActor),
            new GliderController(gliderActor),
            new SpringController("host-spring", new SimPoint(0, 0), SpringOrientation.Up, new SpringTuning(240m, 2, 2)),
            new RefillController("host-refill", new SimPoint(0, 0), new RefillTuning(2)),
            new WaterController("host-water", new SimRect(-10, -10, 30, 30)),
            new BumperController("host-bumper", new SimPoint(0, 0), new BumperTuning(16, 240m, 2)),
            new PufferController("host-puffer", new SimVector(0, 0), -10m, 10m, tuning: new PufferTuning(30m, 16, 1, 240m, 2)),
            new SeekerController("host-seeker", new SimVector(100m, 0), 90m, 110m, tuning: new SeekerTuning(24m, 72m, 240m, 64, 20, 1, 1, 2, 2, 2))),
            presentation);
        var clock = new DemoHostClock(advancePerDelay);
        using var host = new OfflineAppHost(
            session,
            clock,
            new DelegateAppTickInputSource(tick => new OfflineAppTickInput(
                tick,
                new PlayerInput(1, 0, false, false),
                new AnimationTickInput(tick, "idle", 0, 0))),
            new OfflineAppHostOptions(4));
        var result = host.RunAsync(maximumTicks).GetAwaiter().GetResult();
        var signature = string.Join('|', result.Events.Select(item =>
            $"{item.EventId}:{item.AppTick}:{item.ExecutedTicks}:{item.DroppedIntervals}:{item.Detail}"));
        return new DemoHostRun(
            result.ExecutedTicks,
            result.DroppedIntervals,
            clock.DelayCalls,
            result.FinalLifecycle.ToString(),
            string.Join(',', result.Events.Select(item => item.EventId)),
            signature);
    }
}

internal sealed record DemoHeadlessHost(DemoHostRun NormalCadence, DemoHostRun BacklogCadence, bool DeterministicReplay);

internal sealed record DemoHostRun(
    int ExecutedTicks,
    int DroppedIntervals,
    int DelayCalls,
    string FinalLifecycle,
    string EventIds,
    string Signature);

internal sealed class DemoHostClock(long advancePerDelay) : IAppHostClock
{
    private long _timestamp;
    public long Frequency => 60;
    public int DelayCalls { get; private set; }
    public long GetTimestamp() => _timestamp;
    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DelayCalls++;
        _timestamp = checked(_timestamp + advancePerDelay);
        return ValueTask.CompletedTask;
    }
}
