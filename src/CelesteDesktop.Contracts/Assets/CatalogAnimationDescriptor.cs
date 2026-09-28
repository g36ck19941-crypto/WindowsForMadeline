using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class CatalogAnimationDescriptor
{
    public CatalogAnimationDescriptor(
        string id,
        double delaySeconds,
        bool isLooping,
        string? gotoExpression,
        IEnumerable<CatalogFrameDescriptor> frames)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(frames);

        Id = id;
        DelaySeconds = delaySeconds;
        IsLooping = isLooping;
        GotoExpression = gotoExpression;
        Frames = new ReadOnlyCollection<CatalogFrameDescriptor>(frames.ToArray());
    }

    public string Id { get; }

    public double DelaySeconds { get; }

    public bool IsLooping { get; }

    public string? GotoExpression { get; }

    public IReadOnlyList<CatalogFrameDescriptor> Frames { get; }
}
