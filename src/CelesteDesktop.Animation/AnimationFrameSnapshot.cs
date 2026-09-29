using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.Animation;

public sealed record AnimationFrameSnapshot(
    long Tick,
    string EntityId,
    string AnimationId,
    int FrameIndex,
    string AtlasEntryId,
    Bgra32Frame Frame,
    SpriteOriginDescriptor Origin,
    SpritePointDescriptor? Position,
    bool Transitioned);
