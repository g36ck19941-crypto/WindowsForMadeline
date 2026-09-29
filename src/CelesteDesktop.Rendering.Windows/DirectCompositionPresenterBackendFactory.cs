namespace CelesteDesktop.Rendering.Windows;

public sealed class DirectCompositionPresenterBackendFactory : IRenderPresenterBackendFactory
{
    public IRenderPresenterBackend Create() => new DirectCompositionPresenterBackend();
}
