using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

try
{
    if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) throw new PlatformNotSupportedException("WINDOWS_X64_PROTOTYPE_ONLY");
    if (args is ["--child", var scenario, var id] && Enum.TryParse<Mode>(scenario, out var mode) && Enum.IsDefined(mode) && Guid.TryParseExact(id, "N", out _))
        return Child(mode, id);
    RequireOwnOnly();
    if (args is ["--verify-startup-log"])
    {
        Console.WriteLine(JsonSerializer.Serialize(new { eventId = "RESTRICTED_STARTUP_LOG_VERIFIED", passed = StartupLog.Verify(), childStarted = false })); return 0;
    }
    if (args is ["--verify-phase-protocol"]) return VerifyPhaseProtocol();
    var nativeControls = args is ["--verify-native-controls"];
    if (args is not ["--verify"] && !nativeControls) throw new ArgumentException("FIXED_SELF_PROBE_ONLY");
    if (Marshal.SizeOf<Native.StartupInfoEx>() != 112 || Marshal.SizeOf<Native.ExtendedLimits>() != 144 || Marshal.SizeOf<Native.Accounting>() != 48)
        throw new InvalidOperationException("NATIVE_STRUCT_LAYOUT_MISMATCH");
    var results = new List<Result>();
    foreach (var testMode in new[] { Mode.BeforeResumeAbort, Mode.Complete, Mode.EarlyExit, Mode.Hang, Mode.CloseJobAfterReady, Mode.ParentFailureAfterReady })
    {
        var result = Observe(testMode, nativeControls);
        var expectedExit = testMode switch { Mode.Complete => 0U, Mode.EarlyExit => 23U, _ => 99U };
        var expectedCleanup = testMode switch { Mode.Complete or Mode.EarlyExit => "normal-exit", Mode.Hang => "timeout",
            Mode.BeforeResumeAbort => "pre-resume-abort", Mode.CloseJobAfterReady => "last-job-handle-closed", _ => "synthetic-parent-failure" };
        if (!result.exited || (testMode != Mode.CloseJobAfterReady && result.exitCode != expectedExit) || !result.preResumeGuiDenied || !result.jobAssignedBeforeResume ||
            !result.resourceSettingsVerified || result.cleanupReason != expectedCleanup ||
            (testMode == Mode.CloseJobAfterReady ? result.activeProcessesAfterCleanup is not null : result.activeProcessesAfterCleanup != 0) ||
            (testMode == Mode.BeforeResumeAbort ? result.resumed || result.childReportObserved : !result.resumed || !result.childReportObserved))
            throw new ProbeEvidenceFailure(result);
        results.Add(result);
        Console.WriteLine(JsonSerializer.Serialize(new { eventId = "RESTRICTED_PROCESS_CASE", timestampUtc = DateTime.UtcNow, result }));
    }
    RequireOwnOnly();
    Console.WriteLine(JsonSerializer.Serialize(new { eventId = "RESTRICTED_PROCESS_VERIFIED", taskId = "CDR-082", passed = results.Count,
        failed = 0, allOwnedChildrenExited = true, guiPolicyQueriedBeforeResume = true, resourceConfigurationQueried = true,
        guiCreationProbeAttempted = false, originalLoaded = false, xnaLoaded = false, steamLoaded = false, realInputUsed = false,
        audioUsed = false, networkUsed = false, guiOpened = false, systemConfigurationChanged = false, aclChanged = false,
        gameDirectoryAccessed = false, fullSandboxEstablished = false, originalCompatibilityEstablished = false,
        commitMemoryLimitBytes = 536870912, cpuHardCapPercent = 20, activeProcessLimit = 1, observationTimeoutMs = 3000,
        memoryCpuStressTested = false, appContainerEstablished = false, childKind = nativeControls ? "own-kernel32-native" : "own-net8" }));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new { eventId = "RESTRICTED_PROCESS_FAILED", timestampUtc = DateTime.UtcNow,
        stage = "own-restricted-prototype", result = (ex as ProbeEvidenceFailure)?.Evidence,
        exceptionSource = args.FirstOrDefault() == "--child" ? "own-child-managed" : ex is ProbeEvidenceFailure ? "parent-evidence-check" : "parent-monitor",
        exception = Describe(ex, 0), nativeChildStackCaptured = false, fullSandboxEstablished = false }));
    return ex is ProbeEvidenceFailure ? 2 : 1;
}

static int Child(Mode mode, string id)
{
    var directory = RunDirectory(id);
    WritePhase(directory, id, mode, 0);
    RequireOwnOnly();
    WritePhase(directory, id, mode, 1);
    Native.Check(Native.GetProcessMitigationPolicy(Native.GetCurrentProcess(), 4, out var flags, 4), "CHILD_QUERY_GUI_POLICY");
    WritePhase(directory, id, mode, 2);
    Native.Check(Native.IsProcessInJob(Native.GetCurrentProcess(), 0, out var inJob), "CHILD_QUERY_JOB");
    if ((flags & 1) == 0 || (flags & 2) != 0 || !inJob) throw new InvalidOperationException("CHILD_RESTRICTIONS_MISSING");
    WritePhase(directory, id, mode, 3);
    WriteReport(Path.Combine(directory, "ready.json"), new ChildReport(id, mode.ToString(), "ready", true, true));
    if (mode == Mode.EarlyExit) return 23;
    if (mode != Mode.Complete) Thread.Sleep(30000); // Own generated idle child, no device or target calls.
    RequireOwnOnly();
    WriteReport(Path.Combine(directory, "complete.json"), new ChildReport(id, mode.ToString(), "completed", true, true));
    return 0;
}

static Result Observe(Mode mode, bool nativeControls)
{
    var debugTrace = new DebugTrace(); debugTrace.log.Add("parent-monitor", "configuration-started");
    var id = Guid.NewGuid().ToString("N"); var directory = RunDirectory(id);
    Directory.CreateDirectory(directory);
    using var job = new OwnedHandle(Native.CreateJobObjectW(0, null));
    if (job.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "JOB_CREATE");
    Native.Set(job, 9, new Native.ExtendedLimits { basic = new Native.BasicLimits { flags = Native.JobFlags, activeProcesses = 1 }, jobMemory = 536870912 });
    Native.Set(job, 15, new Native.CpuLimits { flags = 5, rate = 2000 }); // enable + hard cap, units 1/100 percent
    var configured = Native.Query<Native.ExtendedLimits>(job, 9);
    var cpu = Native.Query<Native.CpuLimits>(job, 15);
    if (configured.basic.flags != Native.JobFlags || configured.basic.activeProcesses != 1 || configured.jobMemory != 536870912 || cpu.flags != 5 || cpu.rate != 2000)
        throw new InvalidOperationException("JOB_LIMIT_QUERY_MISMATCH");
    nuint attributeSize = 0;
    if (Native.InitializeProcThreadAttributeList(0, 1, 0, ref attributeSize) || Marshal.GetLastWin32Error() != 122 || attributeSize is 0 or > 65536)
        throw new InvalidOperationException("ATTRIBUTE_SIZE_QUERY_FAILED");
    var attributes = Marshal.AllocHGlobal((int)attributeSize);
    var mitigation = Marshal.AllocHGlobal(8);
    nint environment = 0; var attributesInitialized = false;
    OwnedHandle? process = null; OwnedHandle? thread = null;
    var assigned = false; var resumed = false; var policyChecked = false; var cleanup = "none"; var phase = "policy-configuration"; uint ownedPid = 0;
    try
    {
        Native.Check(Native.InitializeProcThreadAttributeList(attributes, 1, 0, ref attributeSize), "ATTRIBUTE_INIT"); attributesInitialized = true;
        Marshal.WriteInt64(mitigation, (long)Native.GuiPolicy);
        Native.Check(Native.UpdateProcThreadAttribute(attributes, 0, 0x20007, mitigation, 8, 0, 0), "GUI_CREATION_POLICY_SET");
        var dll = typeof(Program).Assembly.Location;
        var host = nativeControls ? Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "cdr-082-restricted-process", "native-child.exe") :
            Environment.ProcessPath ?? throw new InvalidOperationException("OWN_HOST_MISSING");
        if (!nativeControls && !string.Equals(Path.GetFileName(host), "dotnet.exe", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("FIXED_DOTNET_HOST_REQUIRED");
        RejectLinks(host); RejectLinks(dll);
        string? nativeHash = null;
        if (nativeControls)
        {
            var hashFile = host + ".sha256"; RejectLinks(hashFile);
            if (!File.Exists(hashFile) || new FileInfo(hashFile).Length > 128) throw new InvalidDataException("OWN_NATIVE_HASH_MISSING");
            nativeHash = File.ReadAllText(hashFile).Trim();
            if (!Regex.IsMatch(nativeHash, "^[0-9A-F]{64}$") || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(host))) != nativeHash)
                throw new InvalidDataException("OWN_NATIVE_HASH_MISMATCH");
        }
        var variables = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            ["TEMP"] = directory, ["TMP"] = directory,
            ["DOTNET_EnableDiagnostics"] = "0", ["COMPlus_EnableDiagnostics"] = "0",
            ["CORECLR_ENABLE_PROFILING"] = "0", ["COR_ENABLE_PROFILING"] = "0", ["DOTNET_NOLOGO"] = "1" };
        environment = Marshal.StringToHGlobalUni(string.Join('\0', variables.Select(pair => pair.Key + "=" + pair.Value)) + "\0\0");
        var startup = new Native.StartupInfoEx { startup = new Native.StartupInfo { cb = (uint)Marshal.SizeOf<Native.StartupInfoEx>() }, attributes = attributes };
        var command = new StringBuilder(Quote(host) + (nativeControls ? "" : " " + Quote(dll)) + " --child " + mode + " " + id);
        Native.Check(Native.CreateProcessW(host, command, 0, 0, false, 0x08000000 | 0x00080000 | 0x00000400 | 0x4 | 0x2,
            environment, Directory.GetCurrentDirectory(), ref startup, out var child), "SELF_CREATE_SUSPENDED");
        process = new OwnedHandle(child.process); thread = new OwnedHandle(child.thread); ownedPid = child.pid;
        if (nativeHash is not null && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(host))) != nativeHash)
            throw new InvalidDataException("OWN_NATIVE_CHANGED_DURING_CREATION");
        phase = "suspended";
        debugTrace.log.Add("parent-monitor", "created-suspended");
        Native.Check(Native.AssignProcessToJobObject(job, process), "JOB_ASSIGN_BEFORE_RESUME"); assigned = true;
        Native.Check(Native.IsProcessInJob(process.DangerousGetHandle(), job.DangerousGetHandle(), out var ownJob), "QUERY_OWN_JOB");
        if (!ownJob || Native.Query<Native.Accounting>(job, 1).activeProcesses != 1) throw new InvalidOperationException("JOB_ASSIGNMENT_UNVERIFIED");
        Native.Check(Native.GetProcessMitigationPolicy(process.DangerousGetHandle(), 4, out var flags, 4), "PRE_RESUME_GUI_QUERY");
        if ((flags & 1) == 0 || (flags & 2) != 0) throw new InvalidOperationException("GUI_DENIAL_NOT_ENFORCED");
        policyChecked = true; phase = "pre-resume-verified";
        debugTrace.log.Add("parent-monitor", "restrictions-verified");
        if (mode == Mode.BeforeResumeAbort) { Native.Check(Native.TerminateJobObject(job, 99), "ABORT_SUSPENDED"); cleanup = "pre-resume-abort"; }
        else
        {
            if (Native.ResumeThread(thread) != 1) throw new Win32Exception(Marshal.GetLastWin32Error(), "RESUME_FAILED");
            resumed = true; phase = "resumed";
            debugTrace.log.Add("parent-monitor", "resumed");
            var deadline = Stopwatch.StartNew();
            while (Native.Wait(process, 0) == Native.WaitTimeout)
            {
                PumpDebug(process, ownedPid, debugTrace, 20);
                if (ReadReport(directory, "ready.json", id, mode, "ready"))
                {
                    if (phase != "child-ready") debugTrace.log.Add("child-report", "ready-observed");
                    phase = "child-ready";
                    if (mode == Mode.CloseJobAfterReady) { job.Dispose(); cleanup = "last-job-handle-closed"; break; }
                    if (mode == Mode.ParentFailureAfterReady) throw new SyntheticParentFailure();
                }
                if (deadline.ElapsedMilliseconds >= 3000) { Native.Check(Native.TerminateJobObject(job, 99), "TIMEOUT_JOB_TERMINATE"); cleanup = "timeout"; break; }
            }
        }
        WaitExited(process, ownedPid, debugTrace);
        debugTrace.log.Add("parent-monitor", "exit-confirmed");
        Native.Check(Native.GetExitCodeProcess(process, out var exitCode), "EXIT_QUERY");
        var ready = ReadReport(directory, "ready.json", id, mode, "ready");
        var completed = ReadReport(directory, "complete.json", id, mode, "completed");
        if (mode == Mode.Complete && !completed) cleanup = "startup-or-protocol-failed";
        if (mode != Mode.Complete && completed) throw new InvalidOperationException("UNEXPECTED_COMPLETION");
        uint? active = job.IsClosed ? null : WaitJobEmpty(job); // Closed handle cannot be queried; do not invent zero accounting.
        if (ready) phase = "child-ready";
        if (completed) phase = "completed";
        if (completed) debugTrace.log.Add("child-report", "complete-observed");
        debugTrace.log.Add("parent-monitor", "cleanup-confirmed", cleanup == "none" ? "normal-exit" : cleanup);
        var ownPhase = nativeControls ? null : ReadPhase(directory, id, mode);
        if (ownPhase is not null) debugTrace.log.Add("child-report", "phase-file-observed-after-exit", ownPhase);
        return new Result(id, mode.ToString(), child.pid, phase, exitCode, debugTrace.log.ElapsedMs, policyChecked, assigned, true, resumed,
            ready, true, active, cleanup == "none" ? "normal-exit" : cleanup, debugTrace.exceptionCode, debugTrace.parameter0, debugTrace.exceptionImage, debugTrace.guardTargetImage,
            ownPhase, LoadedImages(debugTrace), debugTrace.log.Snapshot(), debugTrace.log.dropped,
            debugTrace.exceptionCode is null ? null : "owned-child-debug-event", debugTrace.imageLoadEvents);
    }
    catch (SyntheticParentFailure)
    {
        if (process is null || !assigned) throw;
        Native.Check(Native.TerminateJobObject(job, 99), "PARENT_FAILURE_CLEANUP"); WaitExited(process, ownedPid, debugTrace);
        Native.Check(Native.GetExitCodeProcess(process, out var exitCode), "FAILURE_EXIT_QUERY");
        debugTrace.log.Add("parent-monitor", "cleanup-confirmed", "synthetic-parent-failure");
        return new Result(id, mode.ToString(), ownedPid, phase, exitCode, debugTrace.log.ElapsedMs, policyChecked, assigned, true, resumed,
            ReadReport(directory, "ready.json", id, mode, "ready"), true, WaitJobEmpty(job), "synthetic-parent-failure", debugTrace.exceptionCode, debugTrace.parameter0, debugTrace.exceptionImage, debugTrace.guardTargetImage,
            nativeControls ? null : ReadPhase(directory, id, mode), LoadedImages(debugTrace), debugTrace.log.Snapshot(), debugTrace.log.dropped,
            debugTrace.exceptionCode is null ? null : "owned-child-debug-event", debugTrace.imageLoadEvents);
    }
    finally
    {
        try
        {
            // Handles only from this CreateProcess call; no PID attachment, name scan or global settings.
            if (process is not null && Native.Wait(process, 0) == Native.WaitTimeout)
            {
                if (assigned && !job.IsClosed) Native.Check(Native.TerminateJobObject(job, 99), "FINALLY_JOB_CLEANUP");
                else Native.Check(Native.TerminateProcess(process, 99), "FINALLY_OWN_PROCESS_CLEANUP");
                WaitExited(process, ownedPid, debugTrace);
            }
        }
        finally
        {
            thread?.Dispose(); process?.Dispose();
            if (attributesInitialized) Native.DeleteProcThreadAttributeList(attributes);
            Marshal.FreeHGlobal(attributes); Marshal.FreeHGlobal(mitigation);
            if (environment != 0) Marshal.FreeHGlobal(environment);
        }
    }
}

static uint WaitJobEmpty(OwnedHandle job)
{
    for (var attempt = 0; attempt < 100; attempt++)
    {
        var state = Native.Query<Native.Accounting>(job, 1);
        if (state.activeProcesses == 0) return 0;
        Thread.Sleep(20);
    }
    throw new InvalidOperationException("JOB_NOT_EMPTY_AFTER_EXIT");
}
static void PumpDebug(OwnedHandle process, uint ownedPid, DebugTrace trace, uint milliseconds)
{
    if (!Native.WaitForDebugEvent(out var debugEvent, milliseconds))
    {
        if (Marshal.GetLastWin32Error() == 121) return;
        throw new Win32Exception(Marshal.GetLastWin32Error(), "OWN_DEBUG_WAIT");
    }
    if (debugEvent.pid != ownedPid) throw new InvalidOperationException("UNOWNED_DEBUG_EVENT");
    var status = 0x10002U;
    try
    {
        if (debugEvent.code == 1)
        {
            if (debugEvent.exceptionCode != 0x80000003)
            {
                trace.exceptionCode = debugEvent.exceptionCode.ToString("X8");
                trace.log.Add("owned-child-debug-event", "native-exception", trace.exceptionCode);
                trace.parameter0 = debugEvent.parameterCount > 0 ? debugEvent.parameter0.ToString("X") : null;
                trace.exceptionImage = ImageForAddress(process, trace, debugEvent.exceptionAddress);
                trace.guardTargetImage = debugEvent.parameterCount > 1 && debugEvent.parameter0 == 10 ? ImageForAddress(process, trace, (nint)debugEvent.parameter1) : null;
                status = 0x80010001;
            }
        }
        if (debugEvent.code is 3 or 6)
        {
            trace.imageLoadEvents++;
            if (debugEvent.fileHandle == 0) trace.log.Add("owned-child-debug-event", "image-name-unavailable", "no-file-handle");
            else
            {
                try
                {
                    var name = new StringBuilder(2048);
                    var length = Native.GetFinalPathNameByHandleW(debugEvent.fileHandle, name, (uint)name.Capacity, 0);
                    if (length > 0 && length < name.Capacity)
                    {
                        var filename = Path.GetFileName(name.ToString());
                        if (Regex.IsMatch(filename, "^[A-Za-z0-9_.-]{1,128}$"))
                        {
                            if (trace.images.Count >= 128) throw new InvalidDataException("OWN_IMAGE_BUDGET");
                            trace.images[debugEvent.code == 3 ? debugEvent.processImageBase : debugEvent.dllImageBase] = filename;
                            trace.log.Add("owned-child-debug-event", "image-loaded", filename);
                        }
                        else trace.log.Add("owned-child-debug-event", "image-name-unavailable", "name-filtered");
                    }
                    else trace.log.Add("owned-child-debug-event", "image-name-unavailable", length == 0 ? "win32-" + Marshal.GetLastWin32Error() : "name-budget");
                }
                finally { Native.Check(Native.CloseHandle(debugEvent.fileHandle), "DEBUG_FILE_HANDLE_CLOSE"); }
            }
        }
    }
    finally
    {
        // Even an evidence-budget error must release this debug stop so owned cleanup can finish.
        Native.Check(Native.ContinueDebugEvent(debugEvent.pid, debugEvent.tid, status), "OWN_DEBUG_CONTINUE");
    }
}
static void WaitExited(OwnedHandle process, uint ownedPid, DebugTrace trace)
{
    var cleanupClock = Stopwatch.StartNew();
    while (Native.Wait(process, 0) == Native.WaitTimeout)
    {
        if (cleanupClock.ElapsedMilliseconds >= 5000) throw new InvalidOperationException("OWN_CHILD_CLEANUP_UNCONFIRMED");
        PumpDebug(process, ownedPid, trace, 20);
    }
}
static string? ImageForAddress(OwnedHandle process, DebugTrace trace, nint address)
{
    // Query allocation metadata in this owned child only; never read bytes or collect a memory dump.
    if (address == 0 || Native.VirtualQueryEx(process, address, out var info, (nuint)Marshal.SizeOf<Native.MemoryInfo>()) == 0) return null;
    return trace.images.TryGetValue(info.allocationBase, out var image) ? image : "image-name-not-resolved";
}
static bool ReadReport(string directory, string name, string id, Mode mode, string stage)
{
    var file = Path.Combine(directory, name); RejectLinks(file);
    if (!File.Exists(file)) return false;
    if (new FileInfo(file).Length > 2048) throw new InvalidDataException("CHILD_REPORT_BUDGET");
    var report = JsonSerializer.Deserialize<ChildReport>(File.ReadAllText(file));
    if (report is null || report.runId != id || report.scenario != mode.ToString() || report.stage != stage || !report.guiDenied || !report.inJob)
        throw new InvalidDataException("CHILD_REPORT_PROTOCOL");
    return true;
}
static string[] LoadedImages(DebugTrace trace) => trace.images.Values.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
static string PhaseName(int index) => index switch { 0 => "own-entry-file-written", 1 => "own-assembly-check-completed", 2 => "gui-policy-query-returned", 3 => "job-query-verified", _ => throw new InvalidDataException("OWN_PHASE_INDEX") };
static void WritePhase(string directory, string id, Mode mode, int index)
{
    // Fixed private stage records only. No JSON/reflection/native calls needed for the first marker.
    var file = Path.Combine(directory, "phase-" + index + ".txt"); RejectLinks(file);
    File.WriteAllText(file, id + "\n" + mode + "\n" + PhaseName(index));
}
static string? ReadPhase(string directory, string id, Mode mode)
{
    string? last = null; var missing = false;
    for (var index = 0; index < 4; index++)
    {
        var file = Path.Combine(directory, "phase-" + index + ".txt"); RejectLinks(file);
        if (!File.Exists(file)) { missing = true; continue; }
        if (missing || new FileInfo(file).Length > 512 || File.ReadAllText(file) != id + "\n" + mode + "\n" + PhaseName(index))
            throw new InvalidDataException("OWN_PHASE_PROTOCOL");
        last = PhaseName(index);
    }
    return last;
}
static int VerifyPhaseProtocol()
{
    string Fixture() { var path = RunDirectory(Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; }
    var id = Guid.NewGuid().ToString("N"); var passed = 0;
    var absent = Fixture();
    if (ReadPhase(absent, id, Mode.Complete) is not null) throw new InvalidDataException("ABSENT_PHASE_CALLED_ENTRY"); passed++;
    var complete = Fixture(); for (var i = 0; i < 4; i++) WritePhase(complete, id, Mode.Complete, i);
    if (ReadPhase(complete, id, Mode.Complete) != PhaseName(3)) throw new InvalidDataException("PHASE_CHAIN_FAILED"); passed++;
    var entry = Fixture(); WritePhase(entry, id, Mode.Complete, 0);
    if (ReadPhase(entry, id, Mode.Complete) != PhaseName(0)) throw new InvalidDataException("ENTRY_PHASE_FAILED"); passed++;
    foreach (var invalid in new[] { "gap", "id", "mode", "stage", "budget" })
    {
        var path = Fixture();
        var content = id + "\nComplete\n" + PhaseName(0);
        if (invalid == "id") content = Guid.NewGuid().ToString("N") + "\nComplete\n" + PhaseName(0);
        if (invalid == "mode") content = id + "\nHang\n" + PhaseName(0);
        if (invalid == "stage") content = id + "\nComplete\ncompleted";
        if (invalid == "budget") content = new string('x', 513);
        File.WriteAllText(Path.Combine(path, invalid == "gap" ? "phase-1.txt" : "phase-0.txt"), content);
        var rejected = false;
        try { _ = ReadPhase(path, id, Mode.Complete); } catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new InvalidDataException("PHASE_NEGATIVE_NOT_REJECTED"); passed++;
    }
    Console.WriteLine(JsonSerializer.Serialize(new { eventId = "RESTRICTED_PHASE_PROTOCOL_VERIFIED", passed, childStarted = false }));
    return 0;
}
static void WriteReport<T>(string path, T report)
{
    var temporary = path + ".tmp"; RejectLinks(path); RejectLinks(temporary);
    File.WriteAllText(temporary, JsonSerializer.Serialize(report)); File.Move(temporary, path, false);
}
static string RunDirectory(string id)
{
    var root = Directory.GetCurrentDirectory();
    if (!File.Exists(Path.Combine(root, "CelesteDesktopRuntime.sln"))) throw new InvalidOperationException("REPO_ROOT_REQUIRED");
    var path = Path.Combine(root, "artifacts", "cdr-082-restricted-process", "runs", id); RejectLinks(path); return path;
}
static string Quote(string path)
{
    if (path.Contains('"') || path.EndsWith('\\')) throw new ArgumentException("SELF_PATH_INVALID");
    return '"' + path + '"';
}
static void RejectLinks(string path)
{
    for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("REPARSE_PATH_REJECTED");
}
static void RequireOwnOnly()
{
    if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name is "Celeste" or "OriginalCoreCompileProbe" or "Steamworks.NET" or "FNA" ||
        a.GetName().Name?.StartsWith("Microsoft.Xna.Framework", StringComparison.Ordinal) == true))
        throw new InvalidOperationException("TARGET_ASSEMBLY_PRESENT");
}
static object Describe(Exception ex, int depth)
{
    string Redact(string? value) => Regex.Replace(value ?? "", @"[A-Za-z]:[\\/][^\r\n""']+", "[local-path]");
    return new { type = ex.GetType().Name, message = Redact(ex.Message), hresult = ex.HResult,
        nativeError = ex is Win32Exception win32 ? (int?)win32.NativeErrorCode : null, stack = Redact(ex.StackTrace),
        inner = depth >= 3 || ex.InnerException is null ? null : Describe(ex.InnerException, depth + 1) };
}
enum Mode { Complete, EarlyExit, Hang, BeforeResumeAbort, CloseJobAfterReady, ParentFailureAfterReady }
sealed class SyntheticParentFailure : Exception { }
sealed class DebugTrace { public string? exceptionCode, parameter0, exceptionImage, guardTargetImage; public Dictionary<nint, string> images = new(); public StartupLog log = new(); public int imageLoadEvents; }
sealed class ProbeEvidenceFailure(Result evidence) : Exception("SELF_PROBE_DID_NOT_MEET_EXPECTED_OUTCOME") { public Result Evidence { get; } = evidence; }
sealed record ChildReport(string runId, string scenario, string stage, bool guiDenied, bool inJob);
sealed record Result(string runId, string scenario, uint ownedPid, string lastStage, uint exitCode, long durationMs,
    bool preResumeGuiDenied, bool jobAssignedBeforeResume, bool resourceSettingsVerified, bool resumed,
    bool childReportObserved, bool exited, uint? activeProcessesAfterCleanup, string cleanupReason, string? nativeExceptionCode, string? nativeExceptionParameter0, string? exceptionImage, string? guardTargetImage,
    string? lastOwnPhase, string[] loadedImageNames, StartupEvent[] startupTimeline, int timelineEventsDropped, string? nativeExceptionSource, int imageLoadEventCount);
