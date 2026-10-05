using System.Diagnostics;
using System.Text.RegularExpressions;

// Observations, not inferred execution: child-file times are receipt times on the parent clock.
sealed class StartupLog
{
    readonly Stopwatch clock = Stopwatch.StartNew();
    readonly List<StartupEvent> events = new();
    public int dropped { get; private set; }
    public long ElapsedMs => clock.ElapsedMilliseconds;
    public void Add(string source, string name, string? detail = null) => AddAt(source, name, detail, clock.ElapsedMilliseconds);
    internal void AddAt(string source, string name, string? detail, long elapsedMs)
    {
        if (source is not ("parent-monitor" or "child-report" or "owned-child-debug-event") ||
            !Regex.IsMatch(name, "^[a-z-]{1,64}$") || (detail is not null && !Regex.IsMatch(detail, "^[A-Za-z0-9_.-]{1,128}$")) ||
            elapsedMs < 0 || (events.Count > 0 && elapsedMs < events[^1].observedElapsedMs))
            throw new InvalidDataException("STARTUP_EVENT_PROTOCOL");
        if (events.Count == 256) { dropped++; return; }
        events.Add(new StartupEvent(events.Count + 1, elapsedMs, source, name, detail));
    }
    public StartupEvent[] Snapshot() => events.ToArray();
    public static int Verify()
    {
        var passed = 0;
        void Check(bool condition) { if (!condition) throw new InvalidDataException("STARTUP_LOG_CHECK"); passed++; }
        var log = new StartupLog();
        log.AddAt("parent-monitor", "resumed", null, 0);
        log.AddAt("owned-child-debug-event", "image-loaded", "z.dll", 1);
        log.AddAt("owned-child-debug-event", "image-loaded", "a.dll", 2);
        log.AddAt("child-report", "ready-observed", null, 3);
        var snapshot = log.Snapshot();
        Check(snapshot.Select(e => e.sequence).SequenceEqual(new[] { 1, 2, 3, 4 }));
        Check(snapshot[1].detail == "z.dll" && snapshot[2].detail == "a.dll");
        Check(snapshot[0].source != snapshot[3].source && snapshot[3].source == "child-report");
        foreach (var bad in new[] { "source", "time", "path", "name" })
        {
            var rejected = false;
            try { log.AddAt(bad == "source" ? "original-runtime" : "parent-monitor", bad == "name" ? "arbitrary text" : "phase",
                bad == "path" ? "C:\\private\\x.dll" : null, bad == "time" ? 2 : 4); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected);
        }
        var bounded = new StartupLog();
        for (var i = 0; i < 260; i++) bounded.AddAt("parent-monitor", "phase", null, i);
        Check(bounded.Snapshot().Length == 256 && bounded.dropped == 4);
        return passed;
    }
}
sealed record StartupEvent(int sequence, long observedElapsedMs, string source, string name, string? detail);
