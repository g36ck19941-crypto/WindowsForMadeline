using CelesteDesktop.Animation;
using CelesteDesktop.Contracts.Assets;
using CelesteDesktop.Rendering;

namespace CelesteDesktop.App;

public interface IAppPresentationStage : IDisposable
{
    void Start();
    OfflineAnimationPresentationResult Present(AnimationTickInput input);
    void Stop();
}

public sealed class OfflineAnimationAppPresentationStage : IAppPresentationStage
{
    private readonly EntityAssetCatalog _entity;
    private readonly IRenderPresenterBackendFactory _backendFactory;
    private readonly PresentationGeometry _geometry;
    private readonly int _canvasWidth;
    private readonly int _canvasHeight;
    private readonly Action<RenderDiagnosticEvent> _renderDiagnostics;
    private readonly Action<AnimationDiagnosticEvent>? _animationDiagnostics;
    private RenderPresenter? _renderPresenter;
    private OfflineAnimationPresenter? _animationPresenter;
    private bool _disposed;

    public OfflineAnimationAppPresentationStage(
        EntityAssetCatalog entity,
        IRenderPresenterBackendFactory backendFactory,
        PresentationGeometry geometry,
        int canvasWidth,
        int canvasHeight,
        Action<RenderDiagnosticEvent> renderDiagnostics,
        Action<AnimationDiagnosticEvent>? animationDiagnostics = null)
    {
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        _backendFactory = backendFactory ?? throw new ArgumentNullException(nameof(backendFactory));
        _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        if (canvasWidth <= 0 || canvasHeight <= 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        if (_geometry.PixelWidth != canvasWidth || _geometry.PixelHeight != canvasHeight)
            throw new ArgumentException("Presentation geometry and animation canvas must have identical pixel dimensions.");
        _canvasWidth = canvasWidth;
        _canvasHeight = canvasHeight;
        _renderDiagnostics = renderDiagnostics ?? throw new ArgumentNullException(nameof(renderDiagnostics));
        _animationDiagnostics = animationDiagnostics;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_renderPresenter is not null) throw new InvalidOperationException("Presentation stage is already started.");
        var presenter = new RenderPresenter(_backendFactory, _renderDiagnostics);
        try
        {
            presenter.Initialize(_geometry);
            _renderPresenter = presenter;
            _animationPresenter = new OfflineAnimationPresenter(_entity, presenter, _canvasWidth, _canvasHeight, _animationDiagnostics);
        }
        catch
        {
            presenter.Dispose();
            throw;
        }
    }

    public OfflineAnimationPresentationResult Present(AnimationTickInput input)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_animationPresenter is null) throw new InvalidOperationException("Presentation stage is not started.");
        return _animationPresenter.Present(input);
    }

    public void Stop()
    {
        if (_disposed) return;
        _animationPresenter = null;
        _renderPresenter?.Dispose();
        _renderPresenter = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }
}
