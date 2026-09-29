using CelesteDesktop.Contracts.Assets;
using CelesteDesktop.Rendering;

namespace CelesteDesktop.Animation;

public sealed record OfflineAnimationPresentationResult(
    AnimationFrameSnapshot Animation,
    Bgra32Frame ComposedFrame,
    RenderPresentationResult Presentation);
