using System.Text.Json;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

internal static class DuckingCases
{
    public static (string Name, Action Body)[] Tests =>
    [
        ("duck input preserves feet with reduced collision height", DuckPreservesFeet),
        ("grounded duck friction stops without run acceleration", DuckFriction),
        ("release in clear space restores standing bounds", ReleaseRestores),
        ("low ceiling blocks unduck and identifies solid", CeilingBlocks),
        ("repeated blocked rise never overlaps ceiling", RepeatedBlockedRise),
        ("cleared ceiling permits rise on next tick", ClearedCeilingRestores),
        ("jump restores standing height before ordinary jump", JumpRestoresFirst),
        ("blocked unduck prevents jump into ceiling", BlockedJump),
        ("airborne duck input cannot initiate grounded duck", AirborneInput),
        ("duck on one-way platform preserves grounding", OneWayGrounding),
        ("duck snapshots retain old bounds and events", ImmutableSnapshots),
        ("duck clearance replay is deterministic", DeterministicReplay),
        ("invalid duck geometry and friction are rejected", InvalidDucking)
    ];

    private static PlayerInput Input(bool duck = false, bool jump = false) =>
        new(0, 0, jump, jump, duckHeld: duck);

    private static (SimulationWorld World, PlayerNormalController Player) Create(SimVector speed = default)
    {
        var world = new SimulationWorld();
        var actor = new Actor("player", 0, 0, 8, 11);
        world.Add(actor);
        world.Add(new Solid("floor", -100, 11, 200, 8));
        return (world, new PlayerNormalController(actor, initialSpeed: speed));
    }

    private static void DuckPreservesFeet()
    {
        var (world, player) = Create();
        var result = player.Step(Input(duck: true), world);
        Check(result.Ducking && result.Grounded && result.CollisionBounds == new SimRect(0, 5, 8, 6));
        Check(result.Events.Count(e => e.Kind == PlayerNormalEventKind.DuckStarted) == 1);
    }

    private static void DuckFriction()
    {
        var (world, player) = Create(new SimVector(90m, 0m));
        var first = player.Step(new PlayerInput(1, 0, false, false, duckHeld: true), world);
        Check(first.Speed.X == 90m - (500m / 60m));
        for (var i = 0; i < 12; i++) player.Step(new PlayerInput(1, 0, false, false, duckHeld: true), world);
        Check(player.SpeedX == 0m);
    }

    private static void ReleaseRestores()
    {
        var (world, player) = Create();
        player.Step(Input(duck: true), world);
        var result = player.Step(Input(), world);
        Check(!result.Ducking && result.CollisionBounds == new SimRect(0, 0, 8, 11));
        Check(result.Events.Count(e => e.Kind == PlayerNormalEventKind.UnduckCompleted) == 1);
    }

    private static (SimulationWorld World, PlayerNormalController Player, Solid Ceiling) BlockedWorld()
    {
        var (world, player) = Create();
        player.Step(Input(duck: true), world);
        var ceiling = new Solid("low-ceiling", -20, 0, 40, 5);
        world.Add(ceiling);
        return (world, player, ceiling);
    }

    private static void CeilingBlocks()
    {
        var (world, player, _) = BlockedWorld();
        var result = player.Step(Input(), world);
        Check(result.Ducking && result.UnduckBlockingSolidId == "low-ceiling");
        Check(result.CollisionBounds.Bottom == 11 && result.CollisionBounds.Height == 6);
        Check(result.Events.Single(e => e.Kind == PlayerNormalEventKind.UnduckBlocked).SolidId == "low-ceiling");
    }

    private static void RepeatedBlockedRise()
    {
        var (world, player, ceiling) = BlockedWorld();
        for (var i = 0; i < 20; i++)
        {
            var result = player.Step(Input(), world);
            Check(result.Ducking && result.Grounded && !result.CollisionBounds.Intersects(ceiling.Bounds));
        }
    }

    private static void ClearedCeilingRestores()
    {
        var (world, player, ceiling) = BlockedWorld();
        player.Step(Input(), world);
        var result = player.Step(Input(), world, current => ceiling.Move(50m, 0m, current));
        Check(!result.Ducking && result.CollisionBounds.Height == 11 && result.CollisionBounds.Bottom == 11);
        Check(result.UnduckBlockingSolidId is null);
    }

    private static void JumpRestoresFirst()
    {
        var (world, player) = Create();
        player.Step(Input(duck: true), world);
        var result = player.Step(Input(duck: true, jump: true), world);
        Check(!result.Ducking && result.CollisionBounds.Height == 11 && result.Speed.Y == -105m);
        Check(result.Events.Any(e => e.Kind == PlayerNormalEventKind.UnduckCompleted));
        Check(result.Events.Any(e => e.Kind == PlayerNormalEventKind.Jumped));
    }

    private static void BlockedJump()
    {
        var (world, player, ceiling) = BlockedWorld();
        var result = player.Step(Input(jump: true), world);
        Check(result.Ducking && result.Speed.Y == 0m && result.Grounded);
        Check(!result.CollisionBounds.Intersects(ceiling.Bounds));
        Check(!result.Events.Any(e => e.Kind == PlayerNormalEventKind.Jumped));
    }

    private static void AirborneInput()
    {
        var world = new SimulationWorld();
        var actor = new Actor("player", 0, 0, 8, 11);
        world.Add(actor);
        var result = new PlayerNormalController(actor).Step(Input(duck: true), world);
        Check(!result.Ducking && result.CollisionBounds.Height == 11);
    }

    private static void OneWayGrounding()
    {
        var world = new SimulationWorld();
        var actor = new Actor("player", 0, 0, 8, 11);
        world.Add(actor);
        world.Add(new OneWayPlatform("one-way", -20, 11, 40, 2));
        var player = new PlayerNormalController(actor);
        var duck = player.Step(Input(duck: true), world);
        Check(duck.Ducking && duck.Grounded && duck.GroundedOneWayPlatformId == "one-way");
        var stand = player.Step(Input(), world);
        Check(!stand.Ducking && stand.Grounded && stand.GroundedOneWayPlatformId == "one-way");
    }

    private static void ImmutableSnapshots()
    {
        var (world, player) = Create();
        var first = player.Step(Input(duck: true), world);
        player.Step(Input(), world);
        Check(first.Ducking && first.CollisionBounds.Height == 6 && first.Events.Single().Kind == PlayerNormalEventKind.DuckStarted);
    }

    private static string Replay()
    {
        var (world, player, ceiling) = BlockedWorld();
        var snapshots = new List<PlayerNormalSnapshot>();
        for (var i = 0; i < 6; i++)
            snapshots.Add(player.Step(Input(), world, current => { if (i == 3) ceiling.Move(50m, 0m, current); }));
        return JsonSerializer.Serialize(snapshots);
    }

    private static void DeterministicReplay() => Check(Replay() == Replay());

    private static void InvalidDucking()
    {
        var (_, player) = Create();
        Throws<ArgumentOutOfRangeException>(() => new PlayerNormalController(player.Actor, duckHeight: 0));
        Throws<ArgumentOutOfRangeException>(() => new PlayerNormalController(player.Actor, duckHeight: 12));
        Throws<ArgumentOutOfRangeException>(() => new PlayerNormalController(player.Actor,
            NormalJumpTuning.ReferencePartial with { DuckFriction = 0m }));
    }

    private static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Ducking clearance invariant failed.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
