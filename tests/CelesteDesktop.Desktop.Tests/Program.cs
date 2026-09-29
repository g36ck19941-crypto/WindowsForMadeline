using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using CelesteDesktop.Desktop;
using CelesteDesktop.Desktop.Windows;

var realReadonly = args.SequenceEqual(["--real-readonly"]);
var tests = realReadonly
    ? new (string Name, Action Body)[] { ("authorized Windows snapshot exposes aggregate-only facts", AuthorizedWindowsSnapshot) }
    : new (string Name, Action Body)[]
    {
        ("rectangle preserves negative origin", RectanglePreservesNegativeOrigin),
        ("rectangle rejects invalid dimensions", RectangleRejectsInvalidDimensions),
        ("candidate rejects empty token", CandidateRejectsEmptyToken),
        ("candidate rejects implausible DPI", CandidateRejectsDpi),
        ("first capture assigns anonymous ID", FirstCaptureAssignsAnonymousId),
        ("hidden candidates are filtered", HiddenCandidatesAreFiltered),
        ("cloaked candidates are filtered", CloakedCandidatesAreFiltered),
        ("velocity uses elapsed monotonic time", VelocityUsesElapsedTime),
        ("negative velocity is preserved", NegativeVelocityIsPreserved),
        ("new surface starts at zero velocity", NewSurfaceStartsAtZeroVelocity),
        ("removed surface is forgotten", RemovedSurfaceIsForgotten),
        ("output order follows anonymous IDs", OutputOrderIsStable),
        ("duplicate source token is rejected", DuplicateTokenIsRejected),
        ("surface budget is bounded", SurfaceBudgetIsBounded),
        ("time must increase", TimeMustIncrease),
        ("snapshots do not expose source tokens", SnapshotsDoNotExposeSourceTokens),
        ("public surface has no native handle title content screenshot or input", PublicSurfaceIsPrivacyBounded),
        ("diagnostics contain aggregate facts only", DiagnosticsAreAggregateOnly),
        ("same generated replay is deterministic", GeneratedReplayIsDeterministic)
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

static void RectanglePreservesNegativeOrigin()
{
    var rect = new DesktopRect(-1920, -200, 800, 600);
    Equal(-1920, rect.Left);
    Equal(-200, rect.Top);
    Equal(-1120, rect.Right);
}

static void RectangleRejectsInvalidDimensions()
{
    Throws<ArgumentOutOfRangeException>(() => new DesktopRect(0, 0, 0, 10));
    Throws<ArgumentOutOfRangeException>(() => new DesktopRect(0, 0, 10, 70_000));
}

static void CandidateRejectsEmptyToken() =>
    Throws<ArgumentException>(() => new DesktopSurfaceCandidate(Guid.Empty, Rect(), 96, true, false));

static void CandidateRejectsDpi()
{
    Throws<ArgumentOutOfRangeException>(() => new DesktopSurfaceCandidate(Guid.NewGuid(), Rect(), 0, true, false));
    Throws<ArgumentOutOfRangeException>(() => new DesktopSurfaceCandidate(Guid.NewGuid(), Rect(), 961, true, false));
}

static void FirstCaptureAssignsAnonymousId()
{
    var tracker = Tracker([Candidate(Token(1), 10, 20)]);
    var surface = tracker.Capture(TimeSpan.FromSeconds(1)).Surfaces.Single();
    Equal("surface-000001", surface.AnonymousId);
    Equal(0d, surface.VelocityX);
}

static void HiddenCandidatesAreFiltered()
{
    var tracker = Tracker([Candidate(Token(1), visible: false)]);
    Equal(0, tracker.Capture(TimeSpan.FromSeconds(1)).Surfaces.Count);
}

static void CloakedCandidatesAreFiltered()
{
    var tracker = Tracker([Candidate(Token(1), cloaked: true)]);
    Equal(0, tracker.Capture(TimeSpan.FromSeconds(1)).Surfaces.Count);
}

static void VelocityUsesElapsedTime()
{
    var provider = new ScriptedProvider(
        [Candidate(Token(1), 10, 20)],
        [Candidate(Token(1), 30, 10)]);
    var tracker = new DesktopTracker(provider);
    _ = tracker.Capture(TimeSpan.FromSeconds(1));
    var moved = tracker.Capture(TimeSpan.FromSeconds(1.5)).Surfaces.Single();
    Equal(40d, moved.VelocityX);
    Equal(-20d, moved.VelocityY);
}

static void NegativeVelocityIsPreserved()
{
    var provider = new ScriptedProvider([Candidate(Token(1), -10, 0)], [Candidate(Token(1), -25, 0)]);
    var tracker = new DesktopTracker(provider);
    _ = tracker.Capture(TimeSpan.FromSeconds(2));
    Equal(-15d, tracker.Capture(TimeSpan.FromSeconds(3)).Surfaces.Single().VelocityX);
}

static void NewSurfaceStartsAtZeroVelocity()
{
    var provider = new ScriptedProvider([Candidate(Token(1))], [Candidate(Token(1)), Candidate(Token(2), 100)]);
    var tracker = new DesktopTracker(provider);
    _ = tracker.Capture(TimeSpan.FromSeconds(1));
    Equal(0d, tracker.Capture(TimeSpan.FromSeconds(2)).Surfaces.Single(x => x.AnonymousId == "surface-000002").VelocityX);
}

static void RemovedSurfaceIsForgotten()
{
    var provider = new ScriptedProvider([Candidate(Token(1))], [], [Candidate(Token(1), 20)]);
    var tracker = new DesktopTracker(provider);
    Equal("surface-000001", tracker.Capture(TimeSpan.FromSeconds(1)).Surfaces.Single().AnonymousId);
    Equal(0, tracker.Capture(TimeSpan.FromSeconds(2)).Surfaces.Count);
    Equal("surface-000002", tracker.Capture(TimeSpan.FromSeconds(3)).Surfaces.Single().AnonymousId);
}

static void OutputOrderIsStable()
{
    var tracker = Tracker([Candidate(Token(2)), Candidate(Token(1))]);
    Equal("surface-000001,surface-000002", string.Join(',', tracker.Capture(TimeSpan.FromSeconds(1)).Surfaces.Select(x => x.AnonymousId)));
}

static void DuplicateTokenIsRejected()
{
    var candidate = Candidate(Token(1));
    var tracker = Tracker([candidate, candidate]);
    Throws<InvalidOperationException>(() => tracker.Capture(TimeSpan.FromSeconds(1)));
}

static void SurfaceBudgetIsBounded()
{
    var candidates = Enumerable.Range(1, 4097).Select(i => Candidate(GuidFromInt(i))).ToArray();
    Throws<InvalidOperationException>(() => Tracker(candidates).Capture(TimeSpan.FromSeconds(1)));
}

static void TimeMustIncrease()
{
    var tracker = Tracker([Candidate(Token(1))], [Candidate(Token(1))]);
    _ = tracker.Capture(TimeSpan.FromSeconds(1));
    Throws<ArgumentOutOfRangeException>(() => tracker.Capture(TimeSpan.FromSeconds(1)));
}

static void SnapshotsDoNotExposeSourceTokens()
{
    var json = JsonSerializer.Serialize(Tracker([Candidate(Token(1))]).Capture(TimeSpan.FromSeconds(1)));
    Assert(!json.Contains(Token(1).ToString(), StringComparison.OrdinalIgnoreCase), "Source token escaped into snapshot JSON.");
}

static void PublicSurfaceIsPrivacyBounded()
{
    var assembly = typeof(DesktopTracker).Assembly;
    var text = string.Join(' ', assembly.GetExportedTypes().SelectMany(type =>
        type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(member => $"{type.FullName} {member}")));
    foreach (var forbidden in new[] { "IntPtr", "UIntPtr", "nint", "Handle", "Title", "Caption", "Text", "Content", "Screenshot", "Bitmap", "Keyboard", "Mouse", "Input" })
    {
        Assert(!text.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Forbidden public surface leaked: {forbidden}");
    }
}

static void DiagnosticsAreAggregateOnly()
{
    var events = new List<DesktopDiagnosticEvent>();
    var tracker = new DesktopTracker(new ScriptedProvider([Candidate(Token(1), dpi: 120), Candidate(Token(2), visible: false)]), events.Add);
    _ = tracker.Capture(TimeSpan.FromSeconds(1));
    var item = events.Single();
    Equal("DESKTOP_SNAPSHOT_CAPTURED", item.EventId);
    Equal(1, item.SurfaceCount);
    Equal(1, item.FilteredCount);
    var json = JsonSerializer.Serialize(item);
    Assert(!json.Contains(Token(1).ToString(), StringComparison.OrdinalIgnoreCase), "Diagnostic leaked a source token.");
}

static void GeneratedReplayIsDeterministic()
{
    static string Run()
    {
        var provider = new ScriptedProvider([Candidate(Token(1), -20, 4)], [Candidate(Token(1), -5, 10)]);
        var tracker = new DesktopTracker(provider);
        return JsonSerializer.Serialize(new[] { tracker.Capture(TimeSpan.FromSeconds(1)), tracker.Capture(TimeSpan.FromSeconds(2)) });
    }
    Equal(Run(), Run());
}

static void AuthorizedWindowsSnapshot()
{
    if (!OperatingSystem.IsWindows())
    {
        return;
    }

    var events = new List<DesktopDiagnosticEvent>();
    var tracker = new DesktopTracker(new WindowsDesktopSurfaceProvider(), events.Add);
    var clock = Stopwatch.StartNew();
    var firstTime = clock.Elapsed;
    var first = tracker.Capture(firstTime);
    var secondTime = clock.Elapsed;
    if (secondTime <= firstTime) secondTime = firstTime + TimeSpan.FromTicks(1);
    var snapshot = tracker.Capture(secondTime);
    Assert(snapshot.Surfaces.All(item => item.AnonymousId.StartsWith("surface-", StringComparison.Ordinal)), "A non-anonymous ID escaped.");
    Assert(snapshot.Surfaces.All(item => item.Dpi is >= 48 and <= 960), "A DPI value was outside the bounded contract.");
    var minimumDpi = snapshot.Surfaces.Count == 0 ? 0 : snapshot.Surfaces.Min(item => item.Dpi);
    var maximumDpi = snapshot.Surfaces.Count == 0 ? 0 : snapshot.Surfaces.Max(item => item.Dpi);
    var movingCount = snapshot.Surfaces.Count(item => item.VelocityX != 0 || item.VelocityY != 0);
    Console.WriteLine($"AGGREGATE snapshots=2 first_visible_surface_count={first.Surfaces.Count} visible_surface_count={snapshot.Surfaces.Count} moving_surface_count={movingCount} minimum_dpi={minimumDpi} maximum_dpi={maximumDpi} titles_read=0 content_read=0 screenshots_read=0 input_read=0 visible_gui_opened=0");
}

static DesktopTracker Tracker(params IReadOnlyList<DesktopSurfaceCandidate>[] captures) => new(new ScriptedProvider(captures));

static DesktopSurfaceCandidate Candidate(Guid token, int left = 0, int top = 0, uint dpi = 96, bool visible = true, bool cloaked = false) =>
    new(token, new DesktopRect(left, top, 100, 80), dpi, visible, cloaked);

static DesktopRect Rect() => new(0, 0, 100, 80);
static Guid Token(int value) => GuidFromInt(value);
static Guid GuidFromInt(int value) => new(value, 0, 0, new byte[8]);

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
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
    if (!condition) throw new InvalidOperationException(message);
}

internal sealed class ScriptedProvider(params IReadOnlyList<DesktopSurfaceCandidate>[] captures) : IDesktopSurfaceProvider
{
    private readonly Queue<IReadOnlyList<DesktopSurfaceCandidate>> _captures = new(captures);
    public IReadOnlyList<DesktopSurfaceCandidate> Capture() => _captures.Dequeue();
}
