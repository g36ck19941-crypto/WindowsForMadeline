namespace CelesteDesktop.Contracts.Assets;

public sealed class CatalogFrameDescriptor
{
    public CatalogFrameDescriptor(string atlasEntryId, Bgra32Frame frame)
    {
        ArgumentNullException.ThrowIfNull(atlasEntryId);
        ArgumentNullException.ThrowIfNull(frame);

        AtlasEntryId = atlasEntryId;
        Frame = frame;
    }

    public string AtlasEntryId { get; }

    public Bgra32Frame Frame { get; }
}
