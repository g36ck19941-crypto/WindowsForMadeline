using CelesteDesktop.Contracts.Assets;
using CelesteDesktop.Rendering;
using CelesteDesktop.Rendering.Windows;

var tests = new (string Name, Action Body)[]
{
    ("checkerboard alternates exact cells", CheckerboardAlternates),
    ("straight alpha is premultiplied", StraightAlphaIsPremultiplied),
    ("generated frame is immutable", GeneratedFrameIsImmutable),
    ("96 DPI maps one DIP to one pixel", Dpi96IsExact),
    ("fractional DPI rounds outward", FractionalDpiRoundsOutward),
    ("negative virtual origin is preserved", NegativeVirtualOriginIsPreserved),
    ("invalid geometry is rejected", InvalidGeometryIsRejected),
    ("initialization emits backend ready", InitializationEmitsReady),
    ("presentation stages are ordered", PresentationStagesAreOrdered),
    ("identical frame suppresses change event", IdenticalFrameSuppressesChange),
    ("different frame emits change event", DifferentFrameEmitsChange),
    ("frame size mismatch is rejected", FrameSizeMismatchIsRejected),
    ("non-premultiplied alpha is rejected", NonPremultipliedAlphaIsRejected),
    ("device loss is recovered once", DeviceLossIsRecoveredOnce),
    ("device loss records full exception detail", DeviceLossRecordsDetails),
    ("second device loss is bounded failure", SecondDeviceLossIsBounded),
    ("nonrecoverable failure is not retried", NonrecoverableFailureIsNotRetried),
    ("diagnostic sequences are monotonic", DiagnosticSequencesAreMonotonic),
    ("presenter disposal releases backend", PresenterDisposalReleasesBackend),
    ("hidden DirectComposition commit presents generated pixels", HiddenDirectCompositionCommit)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void CheckerboardAlternates()
{
    var first = new Bgra32Color(1, 2, 3, 255);
    var second = new Bgra32Color(4, 5, 6, 255);
    var frame = CheckerboardFrameFactory.Create(4, 4, 2, first, second);
    var pixels = frame.CopyPixels();
    Pixel(first, pixels, frame.Stride, 0, 0);
    Pixel(first, pixels, frame.Stride, 1, 1);
    Pixel(second, pixels, frame.Stride, 2, 0);
    Pixel(second, pixels, frame.Stride, 0, 2);
    Pixel(first, pixels, frame.Stride, 2, 2);
}

static void StraightAlphaIsPremultiplied()
{
    var color = Bgra32Color.FromStraightAlpha(200, 100, 50, 128);
    Equal((byte)100, color.B);
    Equal((byte)50, color.G);
    Equal((byte)25, color.R);
    Equal((byte)128, color.A);
}

static void GeneratedFrameIsImmutable()
{
    var frame = Frame(2, 2, 255);
    var first = frame.CopyPixels();
    first[0] = 0;
    var second = frame.CopyPixels();
    Assert(second[0] != 0, "Mutating a returned copy changed the frame.");
}

static void Dpi96IsExact()
{
    var geometry = new PresentationGeometry(0, 0, 64, 48, 96, 96);
    Equal(64, geometry.PixelWidth);
    Equal(48, geometry.PixelHeight);
}

static void FractionalDpiRoundsOutward()
{
    var geometry = new PresentationGeometry(0, 0, 10.1, 9.1, 120, 144);
    Equal(13, geometry.PixelWidth);
    Equal(14, geometry.PixelHeight);
}

static void NegativeVirtualOriginIsPreserved()
{
    var geometry = new PresentationGeometry(-1920, -240, 32, 24, 96, 96);
    Equal(-1920, geometry.VirtualLeft);
    Equal(-240, geometry.VirtualTop);
}

static void InvalidGeometryIsRejected()
{
    Throws<ArgumentOutOfRangeException>(() => new PresentationGeometry(0, 0, 0, 10, 96, 96));
    Throws<ArgumentOutOfRangeException>(() => new PresentationGeometry(0, 0, 10, 10, double.NaN, 96));
    Throws<ArgumentOutOfRangeException>(() => new PresentationGeometry(0, 0, 20_000, 10, 96, 96));
}

static void InitializationEmitsReady()
{
    var fixture = Fixture();
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    Equal("RENDER_BACKEND_READY", fixture.Events.Single().EventId);
}

static void PresentationStagesAreOrdered()
{
    var fixture = Fixture();
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    var result = presenter.Present(Frame());
    Assert(result.Succeeded, "Presentation failed.");
    Equal(
        "RENDER_BACKEND_READY,RENDER_TEXTURE_UPLOADED,RENDER_SUBMITTED,PRESENTER_PRESENTED,PRESENTED_PIXELS_CHANGED",
        string.Join(',', fixture.Events.Select(item => item.EventId)));
}

static void IdenticalFrameSuppressesChange()
{
    var fixture = Fixture();
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    var frame = Frame();
    Assert(presenter.Present(frame).PixelsChanged, "First presentation must count as changed.");
    Assert(!presenter.Present(frame).PixelsChanged, "Identical frame was reported as changed.");
    Equal(1, fixture.Events.Count(item => item.EventId == "PRESENTED_PIXELS_CHANGED"));
}

static void DifferentFrameEmitsChange()
{
    var fixture = Fixture();
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    _ = presenter.Present(Frame(alpha: 255));
    var result = presenter.Present(Frame(alpha: 128));
    Assert(result.PixelsChanged, "Changed pixels were not reported.");
    Equal(2, fixture.Events.Count(item => item.EventId == "PRESENTED_PIXELS_CHANGED"));
}

static void FrameSizeMismatchIsRejected()
{
    var fixture = Fixture();
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    Throws<ArgumentException>(() => presenter.Present(Frame(7, 8)));
}

static void NonPremultipliedAlphaIsRejected()
{
    var fixture = Fixture();
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    var pixels = new byte[8 * 8 * 4];
    for (var offset = 0; offset < pixels.Length; offset += 4)
    {
        pixels[offset] = 200;
        pixels[offset + 3] = 100;
    }
    Throws<ArgumentException>(() => presenter.Present(new Bgra32Frame(8, 8, pixels)));
}

static void DeviceLossIsRecoveredOnce()
{
    var first = new ScriptedBackend("upload", recoverable: true);
    var second = new ScriptedBackend();
    var fixture = Fixture(first, second);
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    var result = presenter.Present(Frame());
    Assert(result.Succeeded && result.Recovered, "Recoverable device loss did not recover.");
    Assert(first.Disposed, "Lost backend was not disposed.");
    Equal(1, fixture.Events.Count(item => item.EventId == "RENDER_DEVICE_LOST"));
    Equal(1, fixture.Events.Count(item => item.EventId == "RENDER_DEVICE_RECOVERED"));
}

static void DeviceLossRecordsDetails()
{
    var fixture = Fixture(new ScriptedBackend("submit", recoverable: true), new ScriptedBackend());
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    _ = presenter.Present(Frame());
    var failure = fixture.Events.Single(item => item.EventId == "RENDER_DEVICE_LOST");
    Equal("submit", failure.Stage);
    Equal("Error", failure.Severity);
    Assert(failure.ExceptionType?.Contains(nameof(RenderBackendException), StringComparison.Ordinal) == true, "Exception type missing.");
    Assert(!string.IsNullOrWhiteSpace(failure.Message), "Exception message missing.");
    Assert(failure.HResult == unchecked((int)0x887A0005), "HRESULT missing.");
    Assert(!string.IsNullOrWhiteSpace(failure.Stack), "Stack trace missing.");
    Assert(failure.Inner?.Contains("synthetic inner", StringComparison.Ordinal) == true, "Inner exception missing.");
    Equal("dispose-and-recreate-backend", failure.RecoveryAction);
}

static void SecondDeviceLossIsBounded()
{
    var fixture = Fixture(
        new ScriptedBackend("upload", recoverable: true),
        new ScriptedBackend("upload", recoverable: true));
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    var result = presenter.Present(Frame());
    Assert(!result.Succeeded && result.Recovered, "Second device loss was not bounded.");
    Equal(2, fixture.Events.Count(item => item.EventId == "RENDER_DEVICE_LOST"));
}

static void NonrecoverableFailureIsNotRetried()
{
    var first = new ScriptedBackend("submit", recoverable: false);
    var unused = new ScriptedBackend();
    var fixture = Fixture(first, unused);
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    var result = presenter.Present(Frame());
    Assert(!result.Succeeded && !result.Recovered, "Nonrecoverable failure was misclassified.");
    Equal(0, unused.Calls.Count);
}

static void DiagnosticSequencesAreMonotonic()
{
    var fixture = Fixture();
    using var presenter = fixture.Presenter;
    presenter.Initialize(Geometry());
    _ = presenter.Present(Frame());
    Equal(
        string.Join(',', Enumerable.Range(1, fixture.Events.Count)),
        string.Join(',', fixture.Events.Select(item => item.Sequence)));
    var json = RenderDiagnosticJson.SerializeLine(fixture.Events[^1]);
    Assert(json.Contains("\"eventId\":\"PRESENTED_PIXELS_CHANGED\"", StringComparison.Ordinal), "JSON event ID is not canonical camelCase.");
    Assert(json.Contains("\"subsystem\":\"Rendering\"", StringComparison.Ordinal), "JSON subsystem is missing.");
    Assert(!json.Contains("pixelPayload", StringComparison.OrdinalIgnoreCase), "Diagnostic JSON exposed a pixel payload.");
}

static void PresenterDisposalReleasesBackend()
{
    var backend = new ScriptedBackend();
    var fixture = Fixture(backend);
    fixture.Presenter.Initialize(Geometry());
    fixture.Presenter.Dispose();
    Assert(backend.Disposed, "Presenter did not dispose its backend.");
}

static void HiddenDirectCompositionCommit()
{
    if (!OperatingSystem.IsWindows())
    {
        return;
    }

    var events = new List<RenderDiagnosticEvent>();
    using var presenter = new RenderPresenter(new DirectCompositionPresenterBackendFactory(), events.Add, Guid.Empty);
    presenter.Initialize(new PresentationGeometry(-32_000, -32_000, 16, 16, 96, 96));
    var first = presenter.Present(Frame(16, 16, 255));
    var second = presenter.Present(Frame(16, 16, 128));
    Assert(first.Succeeded && second.Succeeded, "Native hidden presentation failed.");
    Assert(first.PixelsChanged && second.PixelsChanged, "Native pixel changes were not recorded.");
    Equal(2, events.Count(item => item.EventId == "PRESENTER_PRESENTED"));
    Assert(events.All(item => item.EventId != "HUMAN_VISIBILITY_CONFIRMED"), "Hidden test claimed human visibility.");
}

static TestFixture Fixture(params ScriptedBackend[] backends)
{
    if (backends.Length == 0)
    {
        backends = [new ScriptedBackend()];
    }
    var events = new List<RenderDiagnosticEvent>();
    var factory = new ScriptedBackendFactory(backends);
    return new TestFixture(new RenderPresenter(factory, events.Add, Guid.Empty), events);
}

static PresentationGeometry Geometry() => new(0, 0, 8, 8, 96, 96);

static Bgra32Frame Frame(int width = 8, int height = 8, byte alpha = 255) =>
    CheckerboardFrameFactory.Create(
        width,
        height,
        2,
        Bgra32Color.FromStraightAlpha(220, 80, 40, alpha),
        Bgra32Color.FromStraightAlpha(30, 160, 230, alpha));

static void Pixel(Bgra32Color expected, byte[] pixels, int stride, int x, int y)
{
    var offset = y * stride + x * 4;
    Equal(expected.B, pixels[offset]);
    Equal(expected.G, pixels[offset + 1]);
    Equal(expected.R, pixels[offset + 2]);
    Equal(expected.A, pixels[offset + 3]);
}

static void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed record TestFixture(RenderPresenter Presenter, List<RenderDiagnosticEvent> Events);

internal sealed class ScriptedBackendFactory(IEnumerable<ScriptedBackend> backends) : IRenderPresenterBackendFactory
{
    private readonly Queue<ScriptedBackend> _backends = new(backends);

    public IRenderPresenterBackend Create()
    {
        if (_backends.Count == 0)
        {
            throw new InvalidOperationException("No scripted backend remains.");
        }
        return _backends.Dequeue();
    }
}

internal sealed class ScriptedBackend(string? failingStage = null, bool recoverable = false) : IRenderPresenterBackend
{
    public string Name => "Scripted";
    public List<string> Calls { get; } = [];
    public bool Disposed { get; private set; }

    public void Initialize(PresentationGeometry geometry)
    {
        Calls.Add("initialize");
        Fail("initialize");
    }

    public void Upload(Bgra32Frame frame)
    {
        Calls.Add("upload");
        Fail("upload");
    }

    public void Submit()
    {
        Calls.Add("submit");
        Fail("submit");
    }

    public void WaitForPresented()
    {
        Calls.Add("present");
        Fail("present");
    }

    public void Dispose()
    {
        Calls.Add("dispose");
        Disposed = true;
    }

    private void Fail(string stage)
    {
        if (!string.Equals(stage, failingStage, StringComparison.Ordinal))
        {
            return;
        }

        throw new RenderBackendException(
            recoverable ? "RENDER_DEVICE_LOST" : "RENDER_NATIVE_CALL_FAILED",
            stage,
            "synthetic backend failure",
            unchecked((int)0x887A0005),
            recoverable,
            new InvalidOperationException("synthetic inner"));
    }
}
