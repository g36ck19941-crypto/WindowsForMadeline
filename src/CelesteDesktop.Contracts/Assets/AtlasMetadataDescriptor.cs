using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class AtlasMetadataDescriptor
{
    public AtlasMetadataDescriptor(
        int formatVersion,
        string packer,
        int packerVersion,
        IEnumerable<AtlasPageDescriptor> pages)
    {
        ArgumentNullException.ThrowIfNull(packer);
        ArgumentNullException.ThrowIfNull(pages);

        FormatVersion = formatVersion;
        Packer = packer;
        PackerVersion = packerVersion;
        Pages = new ReadOnlyCollection<AtlasPageDescriptor>(pages.ToArray());
    }

    public int FormatVersion { get; }

    public string Packer { get; }

    public int PackerVersion { get; }

    public IReadOnlyList<AtlasPageDescriptor> Pages { get; }
}
