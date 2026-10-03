using CelesteDesktop.Simulation.Core;

internal static class HeightResizeCases
{
    public static (string Name, Action Body)[] Tests =>
    [
        ("height resize preserves feet and width", PreservesFeet),
        ("height growth refuses solid overlap transactionally", RefusesOverlap),
        ("height resize preserves both subpixel remainders", PreservesRemainders),
        ("height snapshots capture old size immutably", ImmutableSnapshot),
        ("height resize requires active step and registered actor", RequiresWorldMembership),
        ("invalid height resize leaves actor unchanged", InvalidHeight)
    ];

    private static (SimulationWorld World, Actor Actor) Create()
    {
        var world = new SimulationWorld();
        var actor = new Actor("actor", -3, 0, 8, 11);
        world.Add(actor);
        return (world, actor);
    }

    private static void PreservesFeet()
    {
        var (world, actor) = Create();
        world.Step(_ => Check(actor.TryResizeHeightKeepingBottom(6, world).Applied));
        Check(actor.Bounds == new SimRect(-3, 5, 8, 6));
        world.Step(_ => Check(actor.TryResizeHeightKeepingBottom(11, world).Applied));
        Check(actor.Bounds == new SimRect(-3, 0, 8, 11));
    }

    private static void RefusesOverlap()
    {
        var (world, actor) = Create();
        world.Step(_ => actor.TryResizeHeightKeepingBottom(6, world));
        world.Add(new Solid("ceiling", -10, 0, 20, 5));
        world.Step(_ =>
        {
            var result = actor.TryResizeHeightKeepingBottom(11, world);
            Check(!result.Applied && result.BlockingSolidId == "ceiling");
        });
        Check(actor.Bounds == new SimRect(-3, 5, 8, 6));
    }

    private static void PreservesRemainders()
    {
        var (world, actor) = Create();
        world.Step(_ => { actor.MoveX(0.4m, world); actor.MoveY(-0.4m, world); });
        world.Step(_ => actor.TryResizeHeightKeepingBottom(6, world));
        Check(actor.XSubpixel == 0.4m && actor.YSubpixel == -0.4m);
        world.Step(_ => actor.TryResizeHeightKeepingBottom(11, world));
        Check(actor.XSubpixel == 0.4m && actor.YSubpixel == -0.4m);
    }

    private static void ImmutableSnapshot()
    {
        var (world, actor) = Create();
        var before = world.CaptureSnapshot();
        world.Step(_ => actor.TryResizeHeightKeepingBottom(6, world));
        Check(before.Actors.Single().Height == 11 && before.Actors.Single().Position.Y == 0);
        Check(world.CaptureSnapshot().Actors.Single().Height == 6);
    }

    private static void RequiresWorldMembership()
    {
        var (world, actor) = Create();
        Throws<InvalidOperationException>(() => actor.TryResizeHeightKeepingBottom(6, world));
        var otherWorld = new SimulationWorld();
        otherWorld.Step(_ => Throws<ArgumentException>(() => actor.TryResizeHeightKeepingBottom(6, otherWorld)));
        Check(actor.Height == 11);
    }

    private static void InvalidHeight()
    {
        var (world, actor) = Create();
        world.Step(_ => Throws<ArgumentOutOfRangeException>(() => actor.TryResizeHeightKeepingBottom(0, world)));
        Check(actor.Bounds == new SimRect(-3, 0, 8, 11));
    }

    private static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Height resize invariant failed.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
