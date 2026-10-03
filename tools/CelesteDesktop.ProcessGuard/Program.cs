using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

string? activeScenario = null;
string? activeRunId = null;
try
{
    RequireNoTargetLoaded();
    if (args is ["--child", var modeText, var childRunId] && Enum.TryParse<Scenario>(modeText, out var childMode) &&
        Enum.IsDefined(childMode) && Guid.TryParseExact(childRunId, "N", out _))
        return await Child(childMode, childRunId);
    if (args is not ["--verify"] && args is not ["--demo"]) throw new ArgumentException("PROCESS_GUARD_ARGS");
    var rows = new List<GuardResult>();
    foreach (var mode in Enum.GetValues<Scenario>())
    {
        activeScenario = mode.ToString(); activeRunId = null;
        var result = await Observe(mode);
        activeRunId = result.runId;
        Console.WriteLine(JsonSerializer.Serialize(new { eventId = "PROCESS_GUARD_CASE", timestampUtc = DateTime.UtcNow, result }));
        var expected = mode switch {
            Scenario.Complete => "completed", Scenario.EarlyExit => "unexpected-exit",
            Scenario.MissingComplete => "incomplete", Scenario.Malformed or Scenario.WrongRunId => "protocol-rejected",
            Scenario.FloodStdout or Scenario.FloodStderr => "output-budget-rejected", Scenario.Hang => "timeout",
            _ => throw new InvalidOperationException("UNKNOWN_SCENARIO") };
        if (result.outcome != expected || !result.childExited || result.stdoutCharsKept > 2048 || result.stderrCharsKept > 2048)
            throw new InvalidOperationException("PROCESS_GUARD_CASE_FAILED_" + mode);
        var expectedStage = mode == Scenario.Complete ? "completed" :
            mode is Scenario.MissingComplete or Scenario.Hang ? "context-ready" : "started";
        if (result.lastStage != expectedStage || (mode == Scenario.EarlyExit && result.exitCode != 23) ||
            (mode == Scenario.Hang && !result.terminationRequested) ||
            (mode == Scenario.FloodStdout && !result.stdoutBudgetExceeded) ||
            (mode == Scenario.FloodStderr && !result.stderrBudgetExceeded))
            throw new InvalidOperationException("PROCESS_GUARD_STAGE_FAILED_" + mode);
        rows.Add(result);
    }
    RequireNoTargetLoaded();
    Console.WriteLine(JsonSerializer.Serialize(new { eventId = "PROCESS_GUARD_VERIFIED", taskId = "CDR-082",
        scenarios = rows.Count, passed = rows.Count, failed = 0, allOwnedChildrenExited = rows.All(r => r.childExited),
        originalCodeExecuted = false, xnaLoaded = false, guiOpened = false, realInputUsed = false,
        processSandboxEstablished = false, originalRuntimeMonitorEstablished = false }));
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new { eventId = "PROCESS_GUARD_FAILED", timestampUtc = DateTime.UtcNow,
        stage = "own-synthetic-process-verification", scenario = activeScenario, runId = activeRunId,
        exception = Describe(exception, 0), originalCodeExecuted = false }));
    return 1;
}

static async Task<int> Child(Scenario mode, string runId)
{
    void Emit(string stage, string? id = null) { Console.WriteLine(JsonSerializer.Serialize(new { runId = id ?? runId, stage })); Console.Out.Flush(); }
    Emit("started");
    if (mode == Scenario.EarlyExit) Environment.Exit(23); // Own generated abrupt-exit case, not native/original code.
    if (mode == Scenario.Malformed) { Console.WriteLine("not-json"); return 0; }
    if (mode == Scenario.WrongRunId) { Emit("context-ready", Guid.NewGuid().ToString("N")); return 0; }
    if (mode == Scenario.FloodStdout) { Console.Write(new string('x', 10000)); return 0; }
    if (mode == Scenario.FloodStderr) { Console.Error.Write(new string('x', 10000)); return 0; }
    Emit("context-ready");
    if (mode == Scenario.Hang) await Task.Delay(30000);
    if (mode == Scenario.MissingComplete) return 0;
    RequireNoTargetLoaded(); Emit("completed");
    return 0;
}

static async Task<GuardResult> Observe(Scenario mode)
{
    var runId = Guid.NewGuid().ToString("N");
    var ownDll = typeof(Program).Assembly.Location;
    var host = Environment.ProcessPath ?? throw new InvalidOperationException("OWN_HOST_MISSING");
    var isDotnet = string.Equals(Path.GetFileNameWithoutExtension(host), "dotnet", StringComparison.OrdinalIgnoreCase);
    if (!isDotnet && !string.Equals(Path.GetFullPath(host), Path.ChangeExtension(ownDll, ".exe"), StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("OWN_HOST_NOT_APPROVED");
    var start = new ProcessStartInfo(host) { UseShellExecute = false, CreateNoWindow = true,
        RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
        WorkingDirectory = Path.GetDirectoryName(ownDll)! };
    foreach (var key in new[] { "DOTNET_STARTUP_HOOKS", "DOTNET_ADDITIONAL_DEPS", "DOTNET_SHARED_STORE",
        "CORECLR_PROFILER", "CORECLR_PROFILER_PATH", "CORECLR_PROFILER_PATH_32", "CORECLR_PROFILER_PATH_64",
        "COR_PROFILER", "COR_PROFILER_PATH" }) start.Environment.Remove(key);
    start.Environment["CORECLR_ENABLE_PROFILING"] = "0";
    start.Environment["COR_ENABLE_PROFILING"] = "0";
    start.Environment["DOTNET_EnableDiagnostics"] = "0";
    if (isDotnet) start.ArgumentList.Add(ownDll);
    start.ArgumentList.Add("--child"); start.ArgumentList.Add(mode.ToString()); start.ArgumentList.Add(runId);
    using var child = new Process { StartInfo = start };
    var clock = Stopwatch.StartNew();
    if (!child.Start()) throw new InvalidOperationException("OWN_CHILD_START_FAILED");
    var pid = child.Id; child.StandardInput.Close();
    var stdoutTask = Drain(child.StandardOutput); var stderrTask = Drain(child.StandardError);
    var terminationRequested = false;
    try
    {
        var exit = child.WaitForExitAsync();
        var timedOut = await Task.WhenAny(exit, Task.Delay(3000)) != exit;
        if (timedOut && !child.HasExited) { terminationRequested = true; child.Kill(); }
        await exit.WaitAsync(TimeSpan.FromSeconds(5));
        var stdout = await stdoutTask.WaitAsync(TimeSpan.FromSeconds(5));
        var stderr = await stderrTask.WaitAsync(TimeSpan.FromSeconds(5));
        var protocol = ParseProtocol(stdout.Text, runId);
        var outcome = timedOut ? "timeout" : stdout.Exceeded || stderr.Exceeded ? "output-budget-rejected" :
            child.ExitCode != 0 ? "unexpected-exit" : !protocol.Valid ? "protocol-rejected" :
            protocol.LastStage != "completed" ? "incomplete" : "completed";
        return new GuardResult(runId, mode.ToString(), pid, outcome, child.ExitCode, clock.ElapsedMilliseconds,
            protocol.LastStage, protocol.EventCount, terminationRequested, child.HasExited,
            stdout.Text.Length, stderr.Text.Length, stdout.Exceeded, stderr.Exceeded);
    }
    finally
    {
        // This Process instance is created here; never attach to or terminate another PID.
        if (!child.HasExited) { child.Kill(); await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
    }
}

static async Task<PipeResult> Drain(StreamReader reader)
{
    var kept = new StringBuilder(); var buffer = new char[256]; var exceeded = false;
    int count;
    while ((count = await reader.ReadAsync(buffer.AsMemory())) != 0)
    {
        var available = 2048 - kept.Length;
        kept.Append(buffer, 0, Math.Min(count, available));
        if (count > available) exceeded = true; // Continue draining to avoid a full-pipe deadlock.
    }
    return new PipeResult(kept.ToString(), exceeded);
}

static ProtocolResult ParseProtocol(string text, string runId)
{
    var expected = new[] { "started", "context-ready", "completed" }; var count = 0;
    try
    {
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 1024 || count >= expected.Length) return new(false, count == 0 ? "none" : expected[count - 1], count);
            using var json = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 4 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("runId", out var id) || id.GetString() != runId ||
                !root.TryGetProperty("stage", out var stage) || stage.GetString() != expected[count])
                return new(false, count == 0 ? "none" : expected[count - 1], count);
            count++;
        }
        return new(true, count == 0 ? "none" : expected[count - 1], count);
    }
    catch (Exception ex) when (ex is JsonException or InvalidOperationException)
    { return new(false, count == 0 ? "none" : expected[count - 1], count); }
}

static void RequireNoTargetLoaded()
{
    if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name is "Celeste" or "OriginalCoreCompileProbe" ||
        a.GetName().Name?.StartsWith("Microsoft.Xna.Framework", StringComparison.Ordinal) == true))
        throw new InvalidOperationException("TARGET_ASSEMBLY_UNEXPECTEDLY_LOADED");
}
static object? Describe(Exception exception, int depth)
{
    if (depth >= 4) return new { truncated = true };
    string Redact(string? text) => Regex.Replace(text ?? "", @"[A-Za-z]:[\\/][^\r\n""']+", "[local-path]");
    return new { type = exception.GetType().Name, message = Redact(exception.Message), hresult = exception.HResult,
        stack = Redact(exception.StackTrace), inner = exception.InnerException is null ? null : Describe(exception.InnerException, depth + 1) };
}

enum Scenario { Complete, EarlyExit, MissingComplete, Malformed, WrongRunId, FloodStdout, FloodStderr, Hang }
sealed record PipeResult(string Text, bool Exceeded);
sealed record ProtocolResult(bool Valid, string LastStage, int EventCount);
sealed record GuardResult(string runId, string scenario, int ownedPid, string outcome, int exitCode, long durationMs,
    string lastStage, int eventCount, bool terminationRequested, bool childExited, int stdoutCharsKept,
    int stderrCharsKept, bool stdoutBudgetExceeded, bool stderrBudgetExceeded);
