namespace CelesteDesktop.Simulation.Core;

public readonly record struct SimPoint(int X, int Y);

public readonly record struct SimVector(decimal X, decimal Y)
{
    public static SimVector Zero { get; } = new(0m, 0m);
}

public readonly record struct SimRect
{
    public SimRect(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Simulation bounds must have positive dimensions.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public int Left => X;
    public int Top => Y;
    public int Right => checked(X + Width);
    public int Bottom => checked(Y + Height);

    public bool Intersects(SimRect other) =>
        Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;

    public bool OverlapsHorizontally(SimRect other) => Left < other.Right && Right > other.Left;

    public SimRect Offset(int x, int y) => new(checked(X + x), checked(Y + y), Width, Height);
}
