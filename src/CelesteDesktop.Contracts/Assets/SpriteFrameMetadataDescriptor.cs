using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class SpriteFrameMetadataDescriptor
{
    public SpriteFrameMetadataDescriptor(
        string animationId,
        IEnumerable<SpriteHairFrameDescriptor>? hairFrames,
        IEnumerable<int>? carryOffsets)
    {
        ArgumentNullException.ThrowIfNull(animationId);

        AnimationId = animationId;
        HairFrames = hairFrames is null
            ? null
            : new ReadOnlyCollection<SpriteHairFrameDescriptor>(hairFrames.ToArray());
        CarryOffsets = carryOffsets is null
            ? null
            : new ReadOnlyCollection<int>(carryOffsets.ToArray());
    }

    public string AnimationId { get; }

    public IReadOnlyList<SpriteHairFrameDescriptor>? HairFrames { get; }

    public IReadOnlyList<int>? CarryOffsets { get; }
}
