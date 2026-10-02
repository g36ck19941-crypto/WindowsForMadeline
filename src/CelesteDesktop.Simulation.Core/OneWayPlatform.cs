namespace CelesteDesktop.Simulation.Core;

public sealed class OneWayPlatform
{
    public OneWayPlatform(string id, int x, int y, int width, int height)
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
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public SimRect Bounds => new(X, Y, Width, Height);
}
