using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class SpriteAnimationDescriptor
{
    public SpriteAnimationDescriptor(
        string id,
        string atlasPath,
        double delaySeconds,
        bool isLooping,
        string? gotoExpression,
        bool usesAllFrames,
        IEnumerable<int> frames)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(atlasPath);
        ArgumentNullException.ThrowIfNull(frames);

        Id = id;
        AtlasPath = atlasPath;
        DelaySeconds = delaySeconds;
        IsLooping = isLooping;
        GotoExpression = gotoExpression;
        UsesAllFrames = usesAllFrames;
        Frames = new ReadOnlyCollection<int>(frames.ToArray());
    }

    public string Id { get; }

    public string AtlasPath { get; }

    public double DelaySeconds { get; }

    public bool IsLooping { get; }

    public string? GotoExpression { get; }

    public bool UsesAllFrames { get; }

    public IReadOnlyList<int> Frames { get; }
}
