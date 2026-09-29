using CelesteDesktop.Contracts.Assets;
using CelesteDesktop.Rendering;

namespace CelesteDesktop.Animation;

public sealed class OfflineAnimationPresenter
{
    private readonly AnimationPlayback _playback;
    private readonly RenderPresenter _presenter;
    private readonly int _canvasWidth;
    private readonly int _canvasHeight;
    private readonly AnimationDiagnosticEmitter _diagnostics;

    public OfflineAnimationPresenter(
        EntityAssetCatalog entity,
        RenderPresenter presenter,
        int canvasWidth,
        int canvasHeight,
        Action<AnimationDiagnosticEvent>? emit = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _diagnostics = new AnimationDiagnosticEmitter(emit, entity.EntityId);
        _playback = new AnimationPlayback(entity, _diagnostics);
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        if (canvasWidth <= 0 || canvasHeight <= 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        _canvasWidth = canvasWidth;
        _canvasHeight = canvasHeight;
    }

    public OfflineAnimationPresentationResult Present(AnimationTickInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        try
        {
            var snapshot = _playback.Resolve(input);
            var composed = AnimationFrameCompositor.Compose(snapshot, input, _canvasWidth, _canvasHeight);
            _diagnostics.Success("ANIMATION_FRAME_COMPOSED", "compose", input.Tick, snapshot.AnimationId, snapshot.FrameIndex, composed.ContentSha256);
            var presentation = _presenter.Present(composed);
            return new OfflineAnimationPresentationResult(snapshot, composed, presentation);
        }
        catch (AnimationPipelineException exception)
        {
            _diagnostics.Failure(exception, input.Tick, input.RequestedAnimationId);
            throw;
        }
    }
}
