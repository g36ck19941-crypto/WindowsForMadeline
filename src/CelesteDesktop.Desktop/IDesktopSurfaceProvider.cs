namespace CelesteDesktop.Desktop;

public interface IDesktopSurfaceProvider
{
    IReadOnlyList<DesktopSurfaceCandidate> Capture();
}
