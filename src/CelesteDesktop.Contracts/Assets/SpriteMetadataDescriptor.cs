using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class SpriteMetadataDescriptor
{
    public SpriteMetadataDescriptor(IEnumerable<SpriteDefinitionDescriptor> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        Definitions = new ReadOnlyCollection<SpriteDefinitionDescriptor>(definitions.ToArray());
    }

    public IReadOnlyList<SpriteDefinitionDescriptor> Definitions { get; }
}
