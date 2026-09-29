using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.Rendering;

public interface IRenderPresenterBackend : IDisposable
{
    string Name { get; }

    void Initialize(PresentationGeometry geometry);

    void Upload(Bgra32Frame frame);

    void Submit();

    void WaitForPresented();
}

public interface IRenderPresenterBackendFactory
{
    IRenderPresenterBackend Create();
}
