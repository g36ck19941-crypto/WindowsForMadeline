using System.Security.Cryptography;

namespace CelesteDesktop.Contracts.Assets;

public sealed class Bgra32Frame
{
    private readonly byte[] _pixels;

    public Bgra32Frame(int width, int height, ReadOnlySpan<byte> pixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var stride = checked(width * 4);
        var expectedLength = checked(stride * height);
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException(
                "Pixel data must contain exactly width * height * 4 bytes.",
                nameof(pixels));
        }

        Width = width;
        Height = height;
        Stride = stride;
        _pixels = pixels.ToArray();
        ContentSha256 = Convert.ToHexString(SHA256.HashData(_pixels))
            .ToLowerInvariant();
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public int PixelByteCount => _pixels.Length;

    public string ContentSha256 { get; }

    public byte[] CopyPixels() => (byte[])_pixels.Clone();

    public void CopyPixelsTo(Span<byte> destination)
    {
        if (destination.Length < _pixels.Length)
        {
            throw new ArgumentException("Destination is too small.", nameof(destination));
        }

        _pixels.CopyTo(destination);
    }
}
