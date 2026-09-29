using System.Reflection;
using CelesteDesktop.Animation;
using CelesteDesktop.Contracts.Assets;
using CelesteDesktop.Rendering;

var tests = new (string Name, Action Body)[]
{
    ("first tick resolves first frame", FirstTickResolvesFirstFrame),
    ("delay converts to fixed ticks", DelayConvertsToFixedTicks),
    ("loop wraps deterministically", LoopWraps),
    ("zero delay advances once per tick", ZeroDelayAdvancesPerTick),
    ("nonlooping animation freezes on final frame", NonLoopingFreezes),
    ("goto transitions at completion", GotoTransitions),
    ("goto carries elapsed ticks", GotoCarriesElapsedTicks),
    ("requested state change resets timing", RequestedChangeResets),
    ("unknown requested animation is rejected", UnknownAnimationRejected),
    ("unknown goto is rejected", UnknownGotoRejected),
    ("transition chain is bounded", TransitionChainIsBounded),
    ("ticks must increase", TicksMustIncrease),
    ("empty animation is rejected", EmptyAnimationRejected),
    ("duplicate animation is rejected", DuplicateAnimationRejected),
    ("invalid delay is rejected", InvalidDelayRejected),
    ("frame diagnostics are distinct", FrameDiagnosticsAreDistinct),
    ("snapshot frame remains immutable", SnapshotFrameIsImmutable),
    ("justify origin places frame", JustifyOriginPlacesFrame),
    ("center origin places frame", CenterOriginPlacesFrame),
    ("absolute origin and position place frame", AbsoluteOriginAndPositionPlaceFrame),
    ("horizontal flip reverses pixels", HorizontalFlipReversesPixels),
    ("out of canvas frame is rejected", OutOfCanvasRejected),
    ("invalid canvas is rejected", InvalidCanvasRejected),
    ("snapshot input tick mismatch is rejected", TickMismatchRejected),
    ("offline presenter connects catalog frame to rendering", OfflinePresenterConnectsPipeline),
    ("identical composed frame suppresses pixel change", IdenticalFrameSuppressesChange),
    ("offline failure records structured exception", OfflineFailureRecordsException),
    ("public surface has no file GUI input desktop or simulation dependency", PublicSurfaceIsIsolated)
};

var failed = 0;
foreach (var test in tests)
{
    try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception exception) { failed++; Console.Error.WriteLine($"FAIL {test.Name}\n{exception}"); }
}
Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void FirstTickResolvesFirstFrame()
{
    var playback = Playback(Loop("idle", 0.1, Frame(1), Frame(2)));
    var result = playback.Resolve(Input(10));
    Equal(0, result.FrameIndex); Equal("idle", result.AnimationId); Assert(result.Transitioned, "Initial selection was not marked.");
}

static void DelayConvertsToFixedTicks()
{
    var playback = Playback(Loop("idle", 0.05, Frame(1), Frame(2), Frame(3)));
    Equal(0, playback.Resolve(Input(0)).FrameIndex);
    Equal(0, playback.Resolve(Input(2)).FrameIndex);
    Equal(1, playback.Resolve(Input(3)).FrameIndex);
    Equal(1, playback.Resolve(Input(5)).FrameIndex);
    Equal(2, playback.Resolve(Input(6)).FrameIndex);
}

static void LoopWraps()
{
    var playback = Playback(Loop("idle", 0.05, Frame(1), Frame(2), Frame(3)));
    _ = playback.Resolve(Input(0));
    Equal(0, playback.Resolve(Input(9)).FrameIndex);
}

static void ZeroDelayAdvancesPerTick()
{
    var playback = Playback(Loop("idle", 0, Frame(1), Frame(2)));
    Equal(0, playback.Resolve(Input(0)).FrameIndex);
    Equal(1, playback.Resolve(Input(1)).FrameIndex);
    Equal(0, playback.Resolve(Input(2)).FrameIndex);
}

static void NonLoopingFreezes()
{
    var playback = Playback(Anim("dash", 0.05, null, Frame(1), Frame(2)));
    _ = playback.Resolve(Input(0, "dash"));
    Equal(1, playback.Resolve(Input(30, "dash")).FrameIndex);
}

static void GotoTransitions()
{
    var playback = Playback(Anim("dash", 0.05, "idle", Frame(1)), Loop("idle", 0.1, Frame(2)));
    _ = playback.Resolve(Input(0, "dash"));
    var result = playback.Resolve(Input(3, "dash"));
    Equal("idle", result.AnimationId); Assert(result.Transitioned, "Goto transition missing.");
}

static void GotoCarriesElapsedTicks()
{
    var playback = Playback(Anim("dash", 0.05, "idle", Frame(1)), Loop("idle", 0.05, Frame(2), Frame(3)));
    _ = playback.Resolve(Input(0, "dash"));
    var result = playback.Resolve(Input(6, "dash"));
    Equal("idle", result.AnimationId); Equal(1, result.FrameIndex);
}

static void RequestedChangeResets()
{
    var playback = Playback(Loop("idle", 0.05, Frame(1), Frame(2)), Loop("run", 0.05, Frame(3), Frame(4)));
    _ = playback.Resolve(Input(0));
    _ = playback.Resolve(Input(4));
    Equal(0, playback.Resolve(Input(5, "run")).FrameIndex);
}

static void UnknownAnimationRejected() =>
    Throws<AnimationPipelineException>(() => Playback(Loop("idle", 0.1, Frame(1))).Resolve(Input(0, "missing")));

static void UnknownGotoRejected()
{
    var playback = Playback(Anim("dash", 0, "missing", Frame(1)));
    _ = playback.Resolve(Input(0, "dash"));
    Throws<AnimationPipelineException>(() => playback.Resolve(Input(1, "dash")));
}

static void TransitionChainIsBounded()
{
    var animations = Enumerable.Range(0, 33).Select(i => Anim($"a{i}", 0, $"a{(i + 1) % 33}", Frame(i + 1))).ToArray();
    var playback = Playback(animations);
    _ = playback.Resolve(Input(0, "a0"));
    Throws<AnimationPipelineException>(() => playback.Resolve(Input(100, "a0")));
}

static void TicksMustIncrease()
{
    var playback = Playback(Loop("idle", 0.1, Frame(1)));
    _ = playback.Resolve(Input(2));
    Throws<AnimationPipelineException>(() => playback.Resolve(Input(2)));
}

static void EmptyAnimationRejected() => Throws<ArgumentException>(() => Playback(new CatalogAnimationDescriptor("idle", 0.1, true, null, [])));
static void DuplicateAnimationRejected() => Throws<ArgumentException>(() => Playback(Loop("idle", 0.1, Frame(1)), Loop("idle", 0.1, Frame(2))));
static void InvalidDelayRejected() => Throws<ArgumentOutOfRangeException>(() => Playback(Loop("idle", double.NaN, Frame(1))));

static void FrameDiagnosticsAreDistinct()
{
    var events = new List<AnimationDiagnosticEvent>();
    var playback = new AnimationPlayback(Entity(Loop("idle", 0.1, Frame(1))), events.Add);
    _ = playback.Resolve(Input(0));
    Equal("ANIMATION_STATE_SELECTED,ANIMATION_FRAME_RESOLVED", string.Join(',', events.Select(x => x.EventId)));
    Assert(events.All(x => x.FrameFingerprint is null || x.FrameFingerprint.Length == 64), "Diagnostic fingerprint malformed.");
}

static void SnapshotFrameIsImmutable()
{
    var result = Playback(Loop("idle", 0.1, Frame(7))).Resolve(Input(0));
    var pixels = result.Frame.CopyPixels(); pixels[0] = 0;
    Assert(result.Frame.CopyPixels()[0] != 0, "Snapshot frame was mutable.");
}

static void JustifyOriginPlacesFrame()
{
    var snapshot = Snapshot(new SpriteOriginDescriptor(SpriteOriginKind.Justify, 0.5, 1), Frame(5, 2, 2));
    var composed = AnimationFrameCompositor.Compose(snapshot, Input(0, anchorX: 4, anchorY: 5), 8, 8);
    Pixel(5, composed, 3, 3);
}

static void CenterOriginPlacesFrame()
{
    var snapshot = Snapshot(new SpriteOriginDescriptor(SpriteOriginKind.Center, 0, 0), Frame(5, 2, 2));
    var composed = AnimationFrameCompositor.Compose(snapshot, Input(0, anchorX: 4, anchorY: 4), 8, 8);
    Pixel(5, composed, 3, 3);
}

static void AbsoluteOriginAndPositionPlaceFrame()
{
    var snapshot = Snapshot(new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 1, 1), Frame(5, 2, 2), new SpritePointDescriptor(2, -1));
    var composed = AnimationFrameCompositor.Compose(snapshot, Input(0, anchorX: 3, anchorY: 4), 8, 8);
    Pixel(5, composed, 4, 2);
}

static void HorizontalFlipReversesPixels()
{
    var pixels = new byte[] { 1, 0, 0, 255, 2, 0, 0, 255 };
    var frame = new CatalogFrameDescriptor("f", new Bgra32Frame(2, 1, pixels));
    var snapshot = Snapshot(new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0), frame);
    var composed = AnimationFrameCompositor.Compose(snapshot, Input(0, anchorX: 2, anchorY: 2, flipX: true), 6, 6);
    Pixel(2, composed, 0, 2); Pixel(1, composed, 1, 2);
}

static void OutOfCanvasRejected() => Throws<AnimationPipelineException>(() =>
    AnimationFrameCompositor.Compose(Snapshot(new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0), Frame(1, 2, 2)), Input(0, anchorX: 7, anchorY: 7), 8, 8));

static void InvalidCanvasRejected() => Throws<ArgumentOutOfRangeException>(() =>
    AnimationFrameCompositor.Compose(Snapshot(new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0), Frame(1)), Input(0), 0, 8));

static void TickMismatchRejected() => Throws<AnimationPipelineException>(() =>
    AnimationFrameCompositor.Compose(Snapshot(new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0), Frame(1)), Input(1), 8, 8));

static void OfflinePresenterConnectsPipeline()
{
    var backend = new RecordingBackend();
    var renderEvents = new List<RenderDiagnosticEvent>();
    var animationEvents = new List<AnimationDiagnosticEvent>();
    using var presenter = new RenderPresenter(new RecordingFactory(backend), renderEvents.Add, Guid.Empty);
    presenter.Initialize(new PresentationGeometry(0, 0, 8, 8, 96, 96));
    var offline = new OfflineAnimationPresenter(Entity(Loop("idle", 0, Frame(5, 2, 2), Frame(6, 2, 2))), presenter, 8, 8, animationEvents.Add);
    var result = offline.Present(Input(0, anchorX: 3, anchorY: 3));
    Assert(result.Presentation.Succeeded, "Presentation failed.");
    Equal(result.ComposedFrame.ContentSha256, backend.LastFingerprint);
    Assert(animationEvents.Any(x => x.EventId == "ANIMATION_FRAME_RESOLVED"), "Frame resolution event missing.");
    Assert(renderEvents.Any(x => x.EventId == "PRESENTER_PRESENTED"), "Presentation event missing.");
}

static void IdenticalFrameSuppressesChange()
{
    var backend = new RecordingBackend();
    using var presenter = new RenderPresenter(new RecordingFactory(backend), _ => { }, Guid.Empty);
    presenter.Initialize(new PresentationGeometry(0, 0, 8, 8, 96, 96));
    var offline = new OfflineAnimationPresenter(Entity(Loop("idle", 0.1, Frame(5, 2, 2))), presenter, 8, 8);
    Assert(offline.Present(Input(0, anchorX: 3, anchorY: 3)).Presentation.PixelsChanged, "First frame must change.");
    Assert(!offline.Present(Input(1, anchorX: 3, anchorY: 3)).Presentation.PixelsChanged, "Identical frame was marked changed.");
}

static void OfflineFailureRecordsException()
{
    var events = new List<AnimationDiagnosticEvent>();
    using var presenter = new RenderPresenter(new RecordingFactory(new RecordingBackend()), _ => { }, Guid.Empty);
    presenter.Initialize(new PresentationGeometry(0, 0, 8, 8, 96, 96));
    var offline = new OfflineAnimationPresenter(Entity(Loop("idle", 0.1, Frame(5, 2, 2))), presenter, 8, 8, events.Add);
    Throws<AnimationPipelineException>(() => offline.Present(Input(0, anchorX: 8, anchorY: 8)));
    var failure = events.Single(x => x.Outcome == "failed");
    Equal("ANIMATION_FRAME_OUTSIDE_CANVAS", failure.EventId);
    Equal("compose", failure.Stage);
    Assert(!string.IsNullOrWhiteSpace(failure.ExceptionType) && !string.IsNullOrWhiteSpace(failure.Stack), "Failure exception detail missing.");
    Equal("none", failure.RecoveryAction);
}

static void PublicSurfaceIsIsolated()
{
    var text = string.Join(' ', typeof(AnimationPlayback).Assembly.GetExportedTypes().SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "System.IO", "Win32", "Window", "Keyboard", "Mouse", "InputDevice", "CelesteDesktop.Desktop", "CelesteDesktop.Simulation" })
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden dependency leaked: {forbidden}");
}

static AnimationPlayback Playback(params CatalogAnimationDescriptor[] animations) => new(Entity(animations));
static EntityAssetCatalog Entity(params CatalogAnimationDescriptor[] animations) => new("player", "idle", new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 0, 0), null, animations);
static CatalogAnimationDescriptor Loop(string id, double delay, params CatalogFrameDescriptor[] frames) => new(id, delay, true, null, frames);
static CatalogAnimationDescriptor Anim(string id, double delay, string? gotoId, params CatalogFrameDescriptor[] frames) => new(id, delay, false, gotoId, frames);
static CatalogFrameDescriptor Frame(int blue, int width = 1, int height = 1) => new($"frame-{blue}", new Bgra32Frame(width, height, Enumerable.Range(0, width * height).SelectMany(_ => new byte[] { checked((byte)blue), 0, 0, 255 }).ToArray()));
static AnimationTickInput Input(long tick, string id = "idle", int anchorX = 0, int anchorY = 0, bool flipX = false) => new(tick, id, anchorX, anchorY, flipX);
static AnimationFrameSnapshot Snapshot(SpriteOriginDescriptor origin, CatalogFrameDescriptor frame, SpritePointDescriptor? position = null) => new(0, "player", "idle", 0, frame.AtlasEntryId, frame.Frame, origin, position, false);

static void Pixel(int blue, Bgra32Frame frame, int x, int y) => Equal((byte)blue, frame.CopyPixels()[(y * frame.Stride) + (x * 4)]);
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

internal sealed class RecordingFactory(RecordingBackend backend) : IRenderPresenterBackendFactory { public IRenderPresenterBackend Create() => backend; }
internal sealed class RecordingBackend : IRenderPresenterBackend
{
    public string Name => "Recording";
    public string? LastFingerprint { get; private set; }
    public void Initialize(PresentationGeometry geometry) { }
    public void Upload(Bgra32Frame frame) => LastFingerprint = frame.ContentSha256;
    public void Submit() { }
    public void WaitForPresented() { }
    public void Dispose() { }
}
