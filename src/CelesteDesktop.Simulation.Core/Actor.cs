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
    public int Height { get; private set; }
    public decimal XSubpixel => _xRemainder.Remainder;
    public decimal YSubpixel => _yRemainder.Remainder;
    public SimVector LiftSpeed { get; internal set; }
    public bool IsSquished { get; internal set; }
    public SimRect Bounds => new(X, Y, Width, Height);

    public ActorResizeResult TryResizeHeightKeepingBottom(int height, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.RequireActiveStep();
        if (!world.Actors.Contains(this))
        {
            throw new ArgumentException("Actor is not registered in this world.", nameof(world));
        }

        var nextY = checked(Bounds.Bottom - height);
        var candidate = new SimRect(X, nextY, Width, height);
        if (IsSquished)
        {
            return new ActorResizeResult(false, null);
        }
        var blocker = world.FirstCollision(candidate, ignoredSolid: null);
        if (blocker is not null)
        {
            return new ActorResizeResult(false, blocker.Id);
        }

        Y = nextY;
        Height = height;
        return new ActorResizeResult(true, null);
    }

    public ActorMoveResult MoveX(decimal displacement, SimulationWorld world)
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
        return new ActorMoveResult(
            MovementAxis.Horizontal,
            pixels,
            result.MovedPixels,
            result.Blocker?.Id);
    }

    public ActorMoveResult MoveY(decimal displacement, SimulationWorld world)
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
        return new ActorMoveResult(
            MovementAxis.Vertical,
            pixels,
            result.MovedPixels,
            result.Blocker?.Id);
    }

    public ActorMoveResult MoveYWithOneWayPlatforms(
        decimal displacement,
        SimulationWorld world,
        string? ignoredOneWayPlatformId = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.RequireActiveStep();
        var pixels = _yRemainder.Consume(displacement);
        if (pixels == 0 || IsSquished)
        {
            return new ActorMoveResult(MovementAxis.Vertical, pixels, 0, null, null);
        }

        var direction = Math.Sign(pixels);
        var requested = Math.Abs((long)pixels);
        var moved = 0;
        Solid? solidBlocker = null;
        OneWayPlatform? oneWayBlocker = null;
        for (long index = 0; index < requested; index++)
        {
            var current = Bounds;
            var candidate = current.Offset(0, direction);
            solidBlocker = world.FirstCollision(candidate, ignoredSolid: null);
            if (solidBlocker is not null)
            {
                break;
            }

            if (direction > 0)
            {
                oneWayBlocker = world.FirstOneWayCollision(
                    current,
                    candidate,
                    ignoredOneWayPlatformId);
                if (oneWayBlocker is not null)
                {
                    break;
                }
            }

            Y = checked(Y + direction);
            moved += direction;
        }

        if (moved != pixels)
        {
            _yRemainder.Reset();
            if (solidBlocker is not null)
            {
                world.RecordBlocked(
                    this,
                    new ExactMoveResult(moved, solidBlocker),
                    MovementAxis.Vertical,
                    pixels);
            }
        }

        return new ActorMoveResult(
            MovementAxis.Vertical,
            pixels,
            moved,
            solidBlocker?.Id,
            oneWayBlocker?.Id);
    }

    public ActorMoveResult MoveXExact(int pixels, SimulationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.RequireActiveStep();
        var result = MoveExact(pixels, MovementAxis.Horizontal, world, ignoredSolid: null);
        if (result.MovedPixels != pixels)
        {
            world.RecordBlocked(this, result, MovementAxis.Horizontal, pixels);
        }
        return new ActorMoveResult(
            MovementAxis.Horizontal,
            pixels,
            result.MovedPixels,
            result.Blocker?.Id);
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

public sealed record ActorMoveResult(
    MovementAxis Axis,
    int RequestedPixels,
    int MovedPixels,
    string? BlockingSolidId,
    string? BlockingOneWayPlatformId = null)
{
    public bool Blocked => RequestedPixels != MovedPixels;
    public string? BlockingSurfaceId => BlockingSolidId ?? BlockingOneWayPlatformId;
}

public sealed record ActorResizeResult(bool Applied, string? BlockingSolidId);
