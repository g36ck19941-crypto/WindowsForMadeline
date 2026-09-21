using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class AtlasPageDescriptor
{
    public AtlasPageDescriptor(
        int index,
        string dataPath,
        IEnumerable<AtlasEntryDescriptor> entries)
    {
        ArgumentNullException.ThrowIfNull(dataPath);
        ArgumentNullException.ThrowIfNull(entries);

        Index = index;
        DataPath = dataPath;
        Entries = new ReadOnlyCollection<AtlasEntryDescriptor>(entries.ToArray());
    }

    public int Index { get; }

    public string DataPath { get; }

    public IReadOnlyList<AtlasEntryDescriptor> Entries { get; }
}
