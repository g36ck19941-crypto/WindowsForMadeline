namespace CelesteDesktop.AssetWorker.Meta;

public sealed record AtlasMetadataReaderOptions(
    int MaximumInputBytes,
    int MaximumStringBytes,
    int MaximumPages,
    int MaximumEntries,
    int MaximumEntriesPerPage,
    int MaximumAtlasExtent)
{
    public static AtlasMetadataReaderOptions Default { get; } = new(
        16 * 1024 * 1024,
        1024,
        64,
        100_000,
        short.MaxValue,
        16_384);

    internal void Validate()
    {
        if (MaximumInputBytes <= 0 ||
            MaximumStringBytes <= 0 ||
            MaximumPages <= 0 ||
            MaximumPages > short.MaxValue ||
            MaximumEntries <= 0 ||
            MaximumEntriesPerPage <= 0 ||
            MaximumEntriesPerPage > short.MaxValue ||
            MaximumAtlasExtent <= 0 ||
            MaximumAtlasExtent > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AtlasMetadataReaderOptions));
        }
    }
}
