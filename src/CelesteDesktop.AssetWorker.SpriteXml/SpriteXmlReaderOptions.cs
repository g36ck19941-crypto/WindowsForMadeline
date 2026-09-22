namespace CelesteDesktop.AssetWorker.SpriteXml;

public sealed record SpriteXmlReaderOptions(
    int MaximumInputBytes,
    int MaximumStringCharacters,
    int MaximumDefinitions,
    int MaximumAnimationsPerDefinition,
    int MaximumFramesPerAnimation,
    int MaximumFrameMetadataEntries,
    int MaximumCoordinateMagnitude,
    int MaximumFrameIndex)
{
    public static SpriteXmlReaderOptions Default { get; } = new(
        4 * 1024 * 1024,
        1024,
        4096,
        512,
        4096,
        4096,
        16_384,
        1_000_000);

    internal void Validate()
    {
        if (MaximumInputBytes <= 0 ||
            MaximumStringCharacters <= 0 ||
            MaximumDefinitions <= 0 ||
            MaximumAnimationsPerDefinition <= 0 ||
            MaximumFramesPerAnimation <= 0 ||
            MaximumFrameMetadataEntries <= 0 ||
            MaximumCoordinateMagnitude <= 0 ||
            MaximumFrameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SpriteXmlReaderOptions));
        }
    }
}
