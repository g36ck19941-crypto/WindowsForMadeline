using System.ComponentModel;
using System.Runtime.InteropServices;
using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.Rendering.Windows;

public sealed unsafe class DirectCompositionPresenterBackend : IRenderPresenterBackend
{
    private const uint D3D11SdkVersion = 7;
    private const uint D3D11CreateDeviceBgraSupport = 0x20;
    private const int D3DDriverTypeHardware = 1;
    private const int D3DDriverTypeWarp = 5;
    private const uint DxgiFormatB8G8R8A8Unorm = 87;
    private const uint DxgiAlphaModePremultiplied = 1;
    private const uint D3D11UsageDefault = 0;
    private const uint WsExToolWindow = 0x00000080;
    private const uint WsExNoActivate = 0x08000000;
    private const uint WsPopup = 0x80000000;

    private static readonly Guid IidDxgiDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
    private static readonly Guid IidD3D11Resource = new("dc8e63f3-d12b-4952-b47b-5e45026a862d");
    private static readonly Guid IidDxgiSurface = new("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");
    private static readonly Guid IidCompositionDevice = new("c37ea93a-e7aa-450d-b16f-9746cb0407f3");

    private nint _window;
    private nint _d3dDevice;
    private nint _d3dContext;
    private nint _compositionDevice;
    private nint _compositionTarget;
    private nint _visual;
    private nint _surface;
    private int _surfaceWidth;
    private int _surfaceHeight;
    private bool _initialized;
    private bool _uploaded;
    private bool _submitted;
    private bool _disposed;
    private string _driver = "uninitialized";

    public string Name => $"DirectComposition/D3D11-{_driver}";

    public void Initialize(PresentationGeometry geometry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(geometry);
        if (_initialized)
        {
            throw new InvalidOperationException("Native presenter is already initialized.");
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DirectComposition is available only on Windows.");
        }

        try
        {
            _window = NativeMethods.CreateWindowExW(
                WsExToolWindow | WsExNoActivate,
                "STATIC",
                "CelesteDesktopRuntime.HiddenPresenter",
                WsPopup,
                geometry.VirtualLeft,
                geometry.VirtualTop,
                geometry.PixelWidth,
                geometry.PixelHeight,
                0,
                0,
                0,
                0);
            if (_window == 0)
            {
                var error = Marshal.GetLastWin32Error();
                throw new RenderBackendException(
                    "RENDER_WINDOW_CREATE_FAILED",
                    "initialize-window",
                    new Win32Exception(error).Message,
                    error,
                    false);
            }

            CreateD3DDevice();

            nint dxgiDevice = 0;
            try
            {
                var iid = IidDxgiDevice;
                ThrowIfFailed(Marshal.QueryInterface(_d3dDevice, ref iid, out dxgiDevice), "query-dxgi-device", "QueryInterface(IDXGIDevice)");

                var compositionIid = IidCompositionDevice;
                ThrowIfFailed(
                    NativeMethods.DCompositionCreateDevice(dxgiDevice, ref compositionIid, out _compositionDevice),
                    "create-composition-device",
                    "DCompositionCreateDevice");
            }
            finally
            {
                Release(ref dxgiDevice);
            }

            var createTarget = (delegate* unmanaged[Stdcall]<nint, nint, int, nint*, int>)GetMethod(_compositionDevice, 6);
            nint target;
            ThrowIfFailed(createTarget(_compositionDevice, _window, 1, &target), "create-composition-target", "IDCompositionDevice.CreateTargetForHwnd");
            _compositionTarget = target;

            var createVisual = (delegate* unmanaged[Stdcall]<nint, nint*, int>)GetMethod(_compositionDevice, 7);
            nint visual;
            ThrowIfFailed(createVisual(_compositionDevice, &visual), "create-visual", "IDCompositionDevice.CreateVisual");
            _visual = visual;

            var setRoot = (delegate* unmanaged[Stdcall]<nint, nint, int>)GetMethod(_compositionTarget, 3);
            ThrowIfFailed(setRoot(_compositionTarget, _visual), "set-root", "IDCompositionTarget.SetRoot");

            _initialized = true;
        }
        catch
        {
            DisposeNativeResources();
            throw;
        }
    }

    public void Upload(Bgra32Frame frame)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(frame);
        EnsureSurface(frame.Width, frame.Height);

        var pixels = frame.CopyPixels();
        nint updateObject = 0;
        nint destinationResource = 0;
        nint sourceTexture = 0;
        var drawBegan = false;
        try
        {
            var beginDraw = (delegate* unmanaged[Stdcall]<nint, void*, Guid*, nint*, NativePoint*, int>)GetMethod(_surface, 3);
            var surfaceIid = IidDxgiSurface;
            NativePoint updateOffset;
            ThrowIfFailed(beginDraw(_surface, null, &surfaceIid, &updateObject, &updateOffset), "begin-draw", "IDCompositionSurface.BeginDraw");

            drawBegan = true;
            var resourceIid = IidD3D11Resource;
            ThrowIfFailed(Marshal.QueryInterface(updateObject, ref resourceIid, out destinationResource), "query-destination-resource", "QueryInterface(ID3D11Resource)");

            var description = new D3D11Texture2DDescription
            {
                Width = checked((uint)frame.Width),
                Height = checked((uint)frame.Height),
                MipLevels = 1,
                ArraySize = 1,
                Format = DxgiFormatB8G8R8A8Unorm,
                SampleDescription = new DxgiSampleDescription { Count = 1, Quality = 0 },
                Usage = D3D11UsageDefault,
                BindFlags = 0,
                CpuAccessFlags = 0,
                MiscFlags = 0
            };

            fixed (byte* pixelPointer = pixels)
            {
                var initialData = new D3D11SubresourceData
                {
                    SystemMemory = (nint)pixelPointer,
                    SystemMemoryPitch = checked((uint)frame.Stride),
                    SystemMemorySlicePitch = checked((uint)pixels.Length)
                };
                var createTexture = (delegate* unmanaged[Stdcall]<nint, D3D11Texture2DDescription*, D3D11SubresourceData*, nint*, int>)GetMethod(_d3dDevice, 5);
                ThrowIfFailed(createTexture(_d3dDevice, &description, &initialData, &sourceTexture), "create-upload-texture", "ID3D11Device.CreateTexture2D");
            }

            var copy = (delegate* unmanaged[Stdcall]<nint, nint, uint, uint, uint, uint, nint, uint, void*, void>)GetMethod(_d3dContext, 46);
            copy(
                _d3dContext,
                destinationResource,
                0,
                checked((uint)updateOffset.X),
                checked((uint)updateOffset.Y),
                0,
                sourceTexture,
                0,
                null);

            var endDraw = (delegate* unmanaged[Stdcall]<nint, int>)GetMethod(_surface, 4);
            var endDrawResult = endDraw(_surface);
            drawBegan = false;
            ThrowIfFailed(endDrawResult, "end-draw", "IDCompositionSurface.EndDraw");
            _uploaded = true;
            _submitted = false;
        }
        finally
        {
            if (drawBegan)
            {
                var endDraw = (delegate* unmanaged[Stdcall]<nint, int>)GetMethod(_surface, 4);
                _ = endDraw(_surface);
            }

            Release(ref sourceTexture);
            Release(ref destinationResource);
            Release(ref updateObject);
        }
    }

    public void Submit()
    {
        EnsureInitialized();
        if (!_uploaded)
        {
            throw new InvalidOperationException("A frame must be uploaded before submission.");
        }

        var commit = (delegate* unmanaged[Stdcall]<nint, int>)GetMethod(_compositionDevice, 3);
        ThrowIfFailed(commit(_compositionDevice), "submit", "IDCompositionDevice.Commit");
        _submitted = true;
    }

    public void WaitForPresented()
    {
        EnsureInitialized();
        if (!_submitted)
        {
            throw new InvalidOperationException("A frame must be submitted before waiting for presentation.");
        }

        var wait = (delegate* unmanaged[Stdcall]<nint, int>)GetMethod(_compositionDevice, 4);
        ThrowIfFailed(wait(_compositionDevice), "present", "IDCompositionDevice.WaitForCommitCompletion");
        _submitted = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeNativeResources();
    }

    private void CreateD3DDevice()
    {
        var result = NativeMethods.D3D11CreateDevice(
            0,
            D3DDriverTypeHardware,
            0,
            D3D11CreateDeviceBgraSupport,
            0,
            0,
            D3D11SdkVersion,
            out _d3dDevice,
            out _,
            out _d3dContext);
        if (result >= 0)
        {
            _driver = "Hardware";
            return;
        }

        Release(ref _d3dContext);
        Release(ref _d3dDevice);
        result = NativeMethods.D3D11CreateDevice(
            0,
            D3DDriverTypeWarp,
            0,
            D3D11CreateDeviceBgraSupport,
            0,
            0,
            D3D11SdkVersion,
            out _d3dDevice,
            out _,
            out _d3dContext);
        ThrowIfFailed(result, "create-d3d-device", "D3D11CreateDevice(Hardware/WARP)");
        _driver = "WARP";
    }

    private void EnsureSurface(int width, int height)
    {
        if (_surface != 0 && _surfaceWidth == width && _surfaceHeight == height)
        {
            return;
        }

        Release(ref _surface);
        var createSurface = (delegate* unmanaged[Stdcall]<nint, uint, uint, uint, uint, nint*, int>)GetMethod(_compositionDevice, 8);
        nint surface;
        ThrowIfFailed(
            createSurface(
                _compositionDevice,
                checked((uint)width),
                checked((uint)height),
                DxgiFormatB8G8R8A8Unorm,
                DxgiAlphaModePremultiplied,
                &surface),
            "create-surface",
            "IDCompositionDevice.CreateSurface");
        _surface = surface;
        _surfaceWidth = width;
        _surfaceHeight = height;

        var setContent = (delegate* unmanaged[Stdcall]<nint, nint, int>)GetMethod(_visual, 15);
        ThrowIfFailed(setContent(_visual, _surface), "set-content", "IDCompositionVisual.SetContent");
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_initialized)
        {
            throw new InvalidOperationException("Native presenter is not initialized.");
        }
    }

    private static nint GetMethod(nint instance, int slot)
    {
        if (instance == 0)
        {
            throw new InvalidOperationException("COM instance is null.");
        }

        var vtable = *(nint**)instance;
        return vtable[slot];
    }

    private static void ThrowIfFailed(int hresult, string stage, string api)
    {
        if (hresult >= 0)
        {
            return;
        }

        var recoverable = IsDeviceLoss(hresult);
        throw new RenderBackendException(
            recoverable ? "RENDER_DEVICE_LOST" : "RENDER_NATIVE_CALL_FAILED",
            stage,
            $"{api} failed with HRESULT 0x{unchecked((uint)hresult):X8}.",
            hresult,
            recoverable,
            Marshal.GetExceptionForHR(hresult));
    }

    private static bool IsDeviceLoss(int hresult) => unchecked((uint)hresult) is
        0x887A0005 or
        0x887A0006 or
        0x887A0007 or
        0x887A0020;

    private void DisposeNativeResources()
    {
        Release(ref _surface);
        Release(ref _visual);
        Release(ref _compositionTarget);
        Release(ref _compositionDevice);
        Release(ref _d3dContext);
        Release(ref _d3dDevice);
        if (_window != 0)
        {
            _ = NativeMethods.DestroyWindow(_window);
            _window = 0;
        }

        _initialized = false;
        _uploaded = false;
        _submitted = false;
    }

    private static void Release(ref nint instance)
    {
        if (instance == 0)
        {
            return;
        }

        _ = Marshal.Release(instance);
        instance = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DxgiSampleDescription
    {
        public uint Count;
        public uint Quality;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11Texture2DDescription
    {
        public uint Width;
        public uint Height;
        public uint MipLevels;
        public uint ArraySize;
        public uint Format;
        public DxgiSampleDescription SampleDescription;
        public uint Usage;
        public uint BindFlags;
        public uint CpuAccessFlags;
        public uint MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11SubresourceData
    {
        public nint SystemMemory;
        public uint SystemMemoryPitch;
        public uint SystemMemorySlicePitch;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    private static partial class NativeMethods
    {
        [DllImport("d3d11.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern int D3D11CreateDevice(
            nint adapter,
            int driverType,
            nint software,
            uint flags,
            nint featureLevels,
            uint featureLevelCount,
            uint sdkVersion,
            out nint device,
            out uint featureLevel,
            out nint immediateContext);

        [DllImport("dcomp.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern int DCompositionCreateDevice(
            nint renderingDevice,
            ref Guid iid,
            out nint dcompositionDevice);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern nint CreateWindowExW(
            uint extendedStyle,
            string className,
            string windowName,
            uint style,
            int x,
            int y,
            int width,
            int height,
            nint parent,
            nint menu,
            nint instance,
            nint parameter);

        [DllImport("user32.dll", ExactSpelling = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyWindow(nint window);
    }
}
