namespace CelesteDesktop.Simulation.Core;

public sealed class Solid
{
    private readonly SubpixelAccumulator _xRemainder = new();
    private readonly SubpixelAccumulator _yRemainder = new();

    public Solid(string id, int x, int y, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _ = new SimRect(x, y, width, height);
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public string Id { get; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; }
    public int Height { get; }
    public decimal XSubpixel => _xRemainder.Remainder;
    public decimal YSubpixel => _yRemainder.Remainder;
    public SimRect Bounds => new(X, Y, Width, Height);

    public void Move(decimal x, decimal y, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.RequireActiveStep();
        var moveX = _xRemainder.Consume(x);
        var moveY = _yRemainder.Consume(y);
        var riders = world.Actors.Where(actor => actor.IsRiding(this)).ToHashSet();

        MoveAxis(moveX, MovementAxis.Horizontal, riders, world);
        MoveAxis(moveY, MovementAxis.Vertical, riders, world);
    }

    private void MoveAxis(
        int pixels,
        MovementAxis axis,
        IReadOnlySet<Actor> riders,
        SimulationWorld world)
    {
        if (pixels == 0)
        {
            return;
        }

        if (axis == MovementAxis.Horizontal)
        {
            X = checked(X + pixels);
        }
        else
        {
            Y = checked(Y + pixels);
        }

        foreach (var actor in world.Actors)
        {
            if (actor.IsSquished)
            {
                continue;
            }

            if (actor.Bounds.Intersects(Bounds))
            {
                var push = RequiredPush(actor.Bounds, pixels, axis);
                MoveActor(actor, push, pixels, axis, SimulationEventKind.ActorPushed, world);
            }
            else if (riders.Contains(actor))
            {
                MoveActor(actor, pixels, pixels, axis, SimulationEventKind.ActorCarried, world);
            }
        }
    }

    private int RequiredPush(SimRect actor, int directionSource, MovementAxis axis)
    {
        if (axis == MovementAxis.Horizontal)
        {
            return directionSource > 0 ? checked(Bounds.Right - actor.Left) : checked(Bounds.Left - actor.Right);
        }
        return directionSource > 0 ? checked(Bounds.Bottom - actor.Top) : checked(Bounds.Top - actor.Bottom);
    }

    private void MoveActor(
        Actor actor,
        int requested,
        int platformPixels,
        MovementAxis axis,
        SimulationEventKind kind,
        SimulationWorld world)
    {
        var result = actor.MoveExact(requested, axis, world, this);
        actor.LiftSpeed = axis == MovementAxis.Horizontal
            ? actor.LiftSpeed with { X = (decimal)platformPixels * SimulationConstants.TicksPerSecond }
            : actor.LiftSpeed with { Y = (decimal)platformPixels * SimulationConstants.TicksPerSecond };
        world.RecordSolidMotion(actor, this, result, axis, requested, kind);
        if (result.MovedPixels != requested)
        {
            actor.IsSquished = true;
            world.RecordSquish(actor, this, result, axis, requested);
        }
    }
}
