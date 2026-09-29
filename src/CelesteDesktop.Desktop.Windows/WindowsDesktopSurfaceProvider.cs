using System.Runtime.InteropServices;

namespace CelesteDesktop.Desktop.Windows;

public sealed class WindowsDesktopSurfaceProvider : IDesktopSurfaceProvider
{
    private const uint DwmwaExtendedFrameBounds = 9;
    private const uint DwmwaCloaked = 14;
    private readonly Dictionary<nint, Guid> _sessionTokens = [];

    public IReadOnlyList<DesktopSurfaceCandidate> Capture()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The Windows desktop provider requires Windows.");
        }

        var result = new List<DesktopSurfaceCandidate>();
        var seenWindows = new HashSet<nint>();
        Exception? callbackFailure = null;
        var success = EnumWindows((window, _) =>
        {
            try
            {
                if (!IsWindowVisible(window))
                {
                    return true;
                }

                var cloaked = 0;
                _ = DwmGetWindowAttribute(window, DwmwaCloaked, out cloaked, sizeof(int));
                if (!TryGetBounds(window, out var bounds))
                {
                    return true;
                }

                if (!_sessionTokens.TryGetValue(window, out var token))
                {
                    token = Guid.NewGuid();
                    _sessionTokens.Add(window, token);
                }
                seenWindows.Add(window);

                var dpi = GetDpiForWindow(window);
                result.Add(new DesktopSurfaceCandidate(token, bounds, dpi == 0 ? 96u : dpi, true, cloaked != 0));
                return true;
            }
            catch (Exception exception)
            {
                callbackFailure = exception;
                return false;
            }
        }, 0);

        if (callbackFailure is not null)
        {
            throw new InvalidOperationException("DESKTOP_CAPTURE_CALLBACK_FAILED", callbackFailure);
        }
        if (!success)
        {
            throw new InvalidOperationException($"DESKTOP_ENUMERATION_FAILED HRESULT=0x{Marshal.GetHRForLastWin32Error():X8}");
        }

        foreach (var window in _sessionTokens.Keys.Where(window => !seenWindows.Contains(window)).ToArray())
        {
            _sessionTokens.Remove(window);
        }

        return result.AsReadOnly();
    }

    private static bool TryGetBounds(nint window, out DesktopRect bounds)
    {
        var result = DwmGetWindowAttribute(window, DwmwaExtendedFrameBounds, out NativeRect rect, Marshal.SizeOf<NativeRect>());
        if (result < 0 && !GetWindowRect(window, out rect))
        {
            bounds = default;
            return false;
        }

        var width = (long)rect.Right - rect.Left;
        var height = (long)rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0 || width > 65_536 || height > 65_536)
        {
            bounds = default;
            return false;
        }

        bounds = new DesktopRect(rect.Left, rect.Top, checked((int)width), checked((int)height));
        return true;
    }

    private delegate bool EnumWindowsCallback(nint window, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, uint attribute, out int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, uint attribute, out NativeRect value, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
