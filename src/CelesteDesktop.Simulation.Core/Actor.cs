namespace CelesteDesktop.Simulation.Core;

public sealed class Actor
{
    private readonly SubpixelAccumulator _xRemainder = new();
    private readonly SubpixelAccumulator _yRemainder = new();

    public Actor(string id, int x, int y, int width, int height)
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
    public SimVector LiftSpeed { get; internal set; }
    public bool IsSquished { get; internal set; }
    public SimRect Bounds => new(X, Y, Width, Height);

    public void MoveX(decimal displacement, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.RequireActiveStep();
        var pixels = _xRemainder.Consume(displacement);
        var result = MoveExact(pixels, MovementAxis.Horizontal, world, ignoredSolid: null);
        if (result.MovedPixels != pixels)
        {
            _xRemainder.Reset();
            world.RecordBlocked(this, result, MovementAxis.Horizontal, pixels);
        }
    }

    public void MoveY(decimal displacement, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.RequireActiveStep();
        var pixels = _yRemainder.Consume(displacement);
        var result = MoveExact(pixels, MovementAxis.Vertical, world, ignoredSolid: null);
        if (result.MovedPixels != pixels)
        {
            _yRemainder.Reset();
            world.RecordBlocked(this, result, MovementAxis.Vertical, pixels);
        }
    }

    public bool IsRiding(Solid solid)
    {
        ArgumentNullException.ThrowIfNull(solid);
        return !IsSquished && Bounds.Bottom == solid.Bounds.Top && Bounds.OverlapsHorizontally(solid.Bounds);
    }

    internal ExactMoveResult MoveExact(
        int pixels,
        MovementAxis axis,
        SimulationWorld world,
        Solid? ignoredSolid)
    {
        if (pixels == 0 || IsSquished)
        {
            return new ExactMoveResult(0, null);
        }

        var direction = Math.Sign(pixels);
        var requested = Math.Abs((long)pixels);
        var moved = 0;
        Solid? blocker = null;
        for (long index = 0; index < requested; index++)
        {
            var candidate = axis == MovementAxis.Horizontal
                ? Bounds.Offset(direction, 0)
                : Bounds.Offset(0, direction);
            blocker = world.FirstCollision(candidate, ignoredSolid);
            if (blocker is not null)
            {
                break;
            }

            if (axis == MovementAxis.Horizontal)
            {
                X = checked(X + direction);
            }
            else
            {
                Y = checked(Y + direction);
            }
            moved += direction;
        }

        return new ExactMoveResult(moved, blocker);
    }

    internal void ResetForTick()
    {
        LiftSpeed = SimVector.Zero;
    }

    internal sealed record ExactMoveResult(int MovedPixels, Solid? Blocker);
}
