namespace CelesteDesktop.Desktop;

public readonly record struct DesktopRect
{
    public DesktopRect(int left, int top, int width, int height)
    {
        if (width <= 0 || height <= 0 || width > 65_536 || height > 65_536)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Desktop surface dimensions must be positive and bounded.");
        }

        _ = checked(left + width);
        _ = checked(top + height);
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public int Left { get; }
    public int Top { get; }
    public int Width { get; }
    public int Height { get; }
    public int Right => checked(Left + Width);
    public int Bottom => checked(Top + Height);
}
