using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class SpriteFrameMetadataDescriptor
{
    public SpriteFrameMetadataDescriptor(
        string key,
        string atlasPath,
        IEnumerable<SpriteHairFrameDescriptor>? hairFrames,
        IEnumerable<int>? carryOffsets,
        bool hidesHairForAllFrames = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(atlasPath);

        Key = key;
        AtlasPath = atlasPath;
        HairFrames = hairFrames is null
            ? null
            : new ReadOnlyCollection<SpriteHairFrameDescriptor>(hairFrames.ToArray());
        CarryOffsets = carryOffsets is null
            ? null
            : new ReadOnlyCollection<int>(carryOffsets.ToArray());
        HidesHairForAllFrames = hidesHairForAllFrames;
    }

    public string Key { get; }

    public string AtlasPath { get; }

    public IReadOnlyList<SpriteHairFrameDescriptor>? HairFrames { get; }

    public IReadOnlyList<int>? CarryOffsets { get; }

    public bool HidesHairForAllFrames { get; }
}
