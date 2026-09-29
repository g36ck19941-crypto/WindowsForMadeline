namespace CelesteDesktop.Desktop;

public sealed record DesktopSurfaceCandidate
{
    public DesktopSurfaceCandidate(Guid sessionToken, DesktopRect bounds, uint dpi, bool isVisible, bool isCloaked)
    {
        if (sessionToken == Guid.Empty)
        {
            throw new ArgumentException("A non-empty session token is required.", nameof(sessionToken));
        }
        if (dpi is < 48 or > 960)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), "DPI must be within the bounded desktop range.");
        }

        SessionToken = sessionToken;
        Bounds = bounds;
        Dpi = dpi;
        IsVisible = isVisible;
        IsCloaked = isCloaked;
    }

    public Guid SessionToken { get; }
    public DesktopRect Bounds { get; }
    public uint Dpi { get; }
    public bool IsVisible { get; }
    public bool IsCloaked { get; }
}
