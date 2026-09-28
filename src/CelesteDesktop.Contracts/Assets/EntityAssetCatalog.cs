using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class EntityAssetCatalog
{
    public EntityAssetCatalog(
        string entityId,
        string? startAnimationId,
        SpriteOriginDescriptor origin,
        SpritePointDescriptor? position,
        IEnumerable<CatalogAnimationDescriptor> animations)
    {
        ArgumentNullException.ThrowIfNull(entityId);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(animations);

        EntityId = entityId;
        StartAnimationId = startAnimationId;
        Origin = origin;
        Position = position;
        Animations = new ReadOnlyCollection<CatalogAnimationDescriptor>(animations.ToArray());
    }

    public string EntityId { get; }

    public string? StartAnimationId { get; }

    public SpriteOriginDescriptor Origin { get; }

    public SpritePointDescriptor? Position { get; }

    public IReadOnlyList<CatalogAnimationDescriptor> Animations { get; }
}
