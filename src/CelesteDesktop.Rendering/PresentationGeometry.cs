namespace CelesteDesktop.Rendering;

public sealed record PresentationGeometry
{
    public PresentationGeometry(
        int virtualLeft,
        int virtualTop,
        double widthDips,
        double heightDips,
        double dpiX,
        double dpiY)
    {
        if (!double.IsFinite(widthDips) || widthDips <= 0 ||
            !double.IsFinite(heightDips) || heightDips <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(widthDips), "Logical dimensions must be finite and positive.");
        }

        if (!double.IsFinite(dpiX) || dpiX <= 0 ||
            !double.IsFinite(dpiY) || dpiY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpiX), "DPI values must be finite and positive.");
        }

        VirtualLeft = virtualLeft;
        VirtualTop = virtualTop;
        WidthDips = widthDips;
        HeightDips = heightDips;
        DpiX = dpiX;
        DpiY = dpiY;
        PixelWidth = ToPixels(widthDips, dpiX);
        PixelHeight = ToPixels(heightDips, dpiY);
    }

    public int VirtualLeft { get; }
    public int VirtualTop { get; }
    public double WidthDips { get; }
    public double HeightDips { get; }
    public double DpiX { get; }
    public double DpiY { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }

    private static int ToPixels(double dips, double dpi)
    {
        var pixels = Math.Ceiling(dips * dpi / 96d);
        if (pixels > 16_384)
        {
            throw new ArgumentOutOfRangeException(nameof(dips), "Presentation dimension exceeds the bounded pixel limit.");
        }

        return checked((int)pixels);
    }
}
