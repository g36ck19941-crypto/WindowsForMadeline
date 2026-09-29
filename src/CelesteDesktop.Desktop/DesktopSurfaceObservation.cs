namespace CelesteDesktop.Desktop;

public sealed record DesktopSurfaceObservation(
    string AnonymousId,
    DesktopRect Bounds,
    uint Dpi,
    double VelocityX,
    double VelocityY);
