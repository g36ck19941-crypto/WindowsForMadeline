namespace CelesteDesktop.AssetWorker.Catalog;

public sealed record AssetCatalogBuilderOptions(
    int MaximumSourceFiles,
    int MaximumEntities,
    int MaximumAnimations,
    int MaximumFrames,
    int MaximumDecodedPages,
    int MaximumPixelBytes)
{
    public static AssetCatalogBuilderOptions Default { get; } = new(
        MaximumSourceFiles: 128,
        MaximumEntities: 64,
        MaximumAnimations: 4096,
        MaximumFrames: 32768,
        MaximumDecodedPages: 64,
        MaximumPixelBytes: 256 * 1024 * 1024);

    internal void Validate()
    {
        if (MaximumSourceFiles <= 0 || MaximumEntities <= 0 || MaximumAnimations <= 0 ||
            MaximumFrames <= 0 || MaximumDecodedPages <= 0 ||
            MaximumPixelBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(AssetCatalogBuilderOptions));
        }
    }
}
