using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.Animation;

public static class AnimationFrameCompositor
{
    public static Bgra32Frame Compose(AnimationFrameSnapshot snapshot, AnimationTickInput input, int canvasWidth, int canvasHeight)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(input);
        if (snapshot.Tick != input.Tick) throw new AnimationPipelineException("ANIMATION_TICK_MISMATCH", "compose", "Snapshot/input tick mismatch.");
        if (canvasWidth <= 0 || canvasHeight <= 0 || canvasWidth > 16_384 || canvasHeight > 16_384)
        {
            throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        }

        var source = snapshot.Frame;
        var (originX, originY) = ResolveOrigin(snapshot.Origin, source.Width, source.Height);
        var positionX = Round(snapshot.Position?.X ?? 0);
        var positionY = Round(snapshot.Position?.Y ?? 0);
        var left = input.FlipX
            ? checked(input.AnchorX + positionX - (source.Width - originX))
            : checked(input.AnchorX + positionX - originX);
        var top = checked(input.AnchorY + positionY - originY);
        if (left < 0 || top < 0 || (long)left + source.Width > canvasWidth || (long)top + source.Height > canvasHeight)
        {
            throw new AnimationPipelineException("ANIMATION_FRAME_OUTSIDE_CANVAS", "compose", "Animation frame is outside the bounded canvas.");
        }

        var sourcePixels = source.CopyPixels();
        var output = new byte[checked(canvasWidth * canvasHeight * 4)];
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var sourceX = input.FlipX ? source.Width - 1 - x : x;
                var sourceOffset = checked((y * source.Stride) + (sourceX * 4));
                var outputOffset = checked((((top + y) * canvasWidth) + left + x) * 4);
                sourcePixels.AsSpan(sourceOffset, 4).CopyTo(output.AsSpan(outputOffset, 4));
            }
        }
        return new Bgra32Frame(canvasWidth, canvasHeight, output);
    }

    private static (int X, int Y) ResolveOrigin(SpriteOriginDescriptor origin, int width, int height) => origin.Kind switch
    {
        SpriteOriginKind.Unspecified => (0, 0),
        SpriteOriginKind.Center => (Round(width / 2d), Round(height / 2d)),
        SpriteOriginKind.Justify => (Round(width * origin.X), Round(height * origin.Y)),
        SpriteOriginKind.Absolute => (Round(origin.X), Round(origin.Y)),
        _ => throw new AnimationPipelineException("ANIMATION_ORIGIN_UNSUPPORTED", "compose", "Animation origin kind is unsupported.")
    };

    private static int Round(double value)
    {
        if (!double.IsFinite(value) || value < -65_536 || value > 65_536)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        return checked((int)Math.Round(value, MidpointRounding.AwayFromZero));
    }
}
