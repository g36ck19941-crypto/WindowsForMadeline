namespace CelesteDesktop.AssetWorker.Data;

public sealed record AtlasDataDecoderOptions(
    int MaximumInputBytes,
    int MaximumDimension,
    int MaximumOutputBytes)
{
    public static AtlasDataDecoderOptions Default { get; } = new(
        64 * 1024 * 1024,
        16_384,
        256 * 1024 * 1024);

    internal void Validate()
    {
        if (MaximumInputBytes <= 0 ||
            MaximumDimension <= 0 ||
            MaximumOutputBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(AtlasDataDecoderOptions));
        }
    }
}
