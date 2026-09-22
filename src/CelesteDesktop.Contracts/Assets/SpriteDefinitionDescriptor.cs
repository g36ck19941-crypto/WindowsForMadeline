using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class SpriteDefinitionDescriptor
{
    public SpriteDefinitionDescriptor(
        string id,
        string atlasPathPrefix,
        string? startAnimationId,
        SpriteOriginDescriptor origin,
        SpritePointDescriptor? position,
        IEnumerable<SpriteAnimationDescriptor> animations,
        IEnumerable<SpriteFrameMetadataDescriptor> frameMetadata)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(atlasPathPrefix);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(animations);
        ArgumentNullException.ThrowIfNull(frameMetadata);

        Id = id;
        AtlasPathPrefix = atlasPathPrefix;
        StartAnimationId = startAnimationId;
        Origin = origin;
        Position = position;
        Animations = new ReadOnlyCollection<SpriteAnimationDescriptor>(animations.ToArray());
        FrameMetadata = new ReadOnlyCollection<SpriteFrameMetadataDescriptor>(frameMetadata.ToArray());
    }

    public string Id { get; }

    public string AtlasPathPrefix { get; }

    public string? StartAnimationId { get; }

    public SpriteOriginDescriptor Origin { get; }

    public SpritePointDescriptor? Position { get; }

    public IReadOnlyList<SpriteAnimationDescriptor> Animations { get; }

    public IReadOnlyList<SpriteFrameMetadataDescriptor> FrameMetadata { get; }
}
