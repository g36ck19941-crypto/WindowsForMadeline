using System.Diagnostics;
using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.Rendering;

public sealed class RenderPresenter : IDisposable
{
    private readonly IRenderPresenterBackendFactory _factory;
    private readonly Action<RenderDiagnosticEvent> _emit;
    private readonly Guid _runId;
    private IRenderPresenterBackend? _backend;
    private PresentationGeometry? _geometry;
    private string? _lastPresentedFingerprint;
    private long _sequence;
    private bool _disposed;

    public RenderPresenter(
        IRenderPresenterBackendFactory factory,
        Action<RenderDiagnosticEvent> emit,
        Guid? runId = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _emit = emit ?? throw new ArgumentNullException(nameof(emit));
        _runId = runId ?? Guid.NewGuid();
    }

    public void Initialize(PresentationGeometry geometry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(geometry);
        if (_backend is not null)
        {
            throw new InvalidOperationException("Presenter is already initialized.");
        }

        _geometry = geometry;
        try
        {
            _backend = _factory.Create();
            Measure("RENDER_BACKEND_READY", "initialize", null, () => _backend.Initialize(geometry));
        }
        catch (Exception exception)
        {
            var backendException = NormalizeException(exception, "initialize");
            EmitFailure(backendException, "none");
            _backend?.Dispose();
            _backend = null;
            throw backendException;
        }
    }

    public RenderPresentationResult Present(Bgra32Frame frame)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(frame);
        if (_backend is null || _geometry is null)
        {
            throw new InvalidOperationException("Presenter must be initialized before presenting a frame.");
        }

        ValidateFrame(frame, _geometry);
        var recovered = false;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return PresentOnce(frame, recovered);
            }
            catch (RenderBackendException exception) when (exception.Recoverable && attempt == 0)
            {
                EmitFailure(exception, "dispose-and-recreate-backend");
                try
                {
                    RecoverBackend();
                    recovered = true;
                }
                catch (Exception recoveryException)
                {
                    var backendException = NormalizeException(recoveryException, "recover");
                    EmitFailure(backendException, "none");
                    return new RenderPresentationResult(
                        false,
                        true,
                        false,
                        frame.ContentSha256,
                        _sequence,
                        backendException.Code);
                }
            }
            catch (RenderBackendException exception)
            {
                EmitFailure(exception, "none");
                return new RenderPresentationResult(
                    false,
                    recovered,
                    false,
                    frame.ContentSha256,
                    _sequence,
                    exception.Code);
            }
        }

        throw new UnreachableException();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _backend?.Dispose();
        _backend = null;
    }

    private RenderPresentationResult PresentOnce(Bgra32Frame frame, bool recovered)
    {
        var backend = _backend!;
        Measure("RENDER_TEXTURE_UPLOADED", "upload", frame, () => backend.Upload(frame));
        Measure("RENDER_SUBMITTED", "submit", frame, backend.Submit);
        Measure("PRESENTER_PRESENTED", "present", frame, backend.WaitForPresented);

        var changed = !string.Equals(_lastPresentedFingerprint, frame.ContentSha256, StringComparison.Ordinal);
        _lastPresentedFingerprint = frame.ContentSha256;
        if (changed)
        {
            Emit("PRESENTED_PIXELS_CHANGED", "present", "succeeded", 0, frame);
        }

        return new RenderPresentationResult(true, recovered, changed, frame.ContentSha256, _sequence, null);
    }

    private void RecoverBackend()
    {
        _backend?.Dispose();
        _backend = _factory.Create();
        Measure("RENDER_DEVICE_RECOVERED", "recover", null, () => _backend.Initialize(_geometry!));
    }

    private void Measure(string eventId, string stage, Bgra32Frame? frame, Action action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            action();
        }
        catch (Exception exception)
        {
            throw NormalizeException(exception, stage);
        }
        stopwatch.Stop();
        Emit(eventId, stage, "succeeded", stopwatch.Elapsed.TotalMilliseconds, frame);
    }

    private static RenderBackendException NormalizeException(Exception exception, string stage) =>
        exception as RenderBackendException ?? new RenderBackendException(
            "RENDER_UNEXPECTED_FAILURE",
            stage,
            exception.Message,
            exception.HResult,
            false,
            exception);

    private void Emit(
        string eventId,
        string stage,
        string outcome,
        double durationMs,
        Bgra32Frame? frame,
        string severity = "Info")
    {
        _emit(new RenderDiagnosticEvent(
            DateTimeOffset.UtcNow,
            _runId,
            ++_sequence,
            eventId,
            "Rendering",
            severity,
            stage,
            outcome,
            durationMs,
            frame?.ContentSha256,
            frame?.Width,
            frame?.Height,
            _backend?.Name));
    }

    private void EmitFailure(RenderBackendException exception, string recoveryAction)
    {
        static string? Inner(Exception? value)
        {
            if (value is null)
            {
                return null;
            }

            return $"{value.GetType().FullName}: {value.Message}\n{value.StackTrace}";
        }

        _emit(new RenderDiagnosticEvent(
            DateTimeOffset.UtcNow,
            _runId,
            ++_sequence,
            exception.Code,
            "Rendering",
            "Error",
            exception.Stage,
            "failed",
            0,
            Backend: _backend?.Name,
            ExceptionType: exception.GetType().FullName,
            Message: exception.Message,
            HResult: exception.HResult,
            Stack: exception.StackTrace,
            Inner: Inner(exception.InnerException),
            Recoverable: exception.Recoverable,
            RecoveryAction: recoveryAction));
    }

    private static void ValidateFrame(Bgra32Frame frame, PresentationGeometry geometry)
    {
        if (frame.Width != geometry.PixelWidth || frame.Height != geometry.PixelHeight)
        {
            throw new ArgumentException(
                $"Frame dimensions {frame.Width}x{frame.Height} do not match presentation pixels {geometry.PixelWidth}x{geometry.PixelHeight}.",
                nameof(frame));
        }

        if (frame.Stride != checked(frame.Width * 4))
        {
            throw new ArgumentException("Presenter requires tightly packed BGRA32 rows.", nameof(frame));
        }

        var pixels = frame.CopyPixels();
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            var alpha = pixels[offset + 3];
            if (pixels[offset] > alpha || pixels[offset + 1] > alpha || pixels[offset + 2] > alpha)
            {
                throw new ArgumentException("Frame contains non-premultiplied BGRA32 pixels.", nameof(frame));
            }
        }
    }
}
