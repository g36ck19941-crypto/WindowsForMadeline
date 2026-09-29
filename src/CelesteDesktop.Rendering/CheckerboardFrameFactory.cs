using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.Rendering;

public readonly record struct Bgra32Color(byte B, byte G, byte R, byte A)
{
    public static Bgra32Color FromStraightAlpha(byte b, byte g, byte r, byte a)
    {
        static byte Premultiply(byte value, byte alpha) =>
            (byte)((value * alpha + 127) / 255);

        return new Bgra32Color(
            Premultiply(b, a),
            Premultiply(g, a),
            Premultiply(r, a),
            a);
    }
}

public static class CheckerboardFrameFactory
{
    public static Bgra32Frame Create(
        int width,
        int height,
        int cellSize,
        Bgra32Color first,
        Bgra32Color second)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Frame dimensions must be positive.");
        }

        if (cellSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
        }

        var stride = checked(width * 4);
        var pixels = new byte[checked(stride * height)];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = ((x / cellSize) + (y / cellSize)) % 2 == 0 ? first : second;
                var offset = checked(y * stride + x * 4);
                pixels[offset] = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = color.A;
            }
        }

        return new Bgra32Frame(width, height, pixels);
    }
}
