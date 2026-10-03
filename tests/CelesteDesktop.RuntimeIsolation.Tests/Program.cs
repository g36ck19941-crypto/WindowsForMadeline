using System.Text.Json;
using CelesteDesktop.RuntimeIsolation;

var checks = 0;
var runId = Guid.NewGuid().ToString();
void Check(bool condition) { if (!condition) throw new InvalidOperationException("ISOLATION_TEST_FAILED"); checks++; }
void Reject<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { checks++; return; }
    throw new InvalidOperationException("ISOLATION_NEGATIVE_TEST_FAILED");
}

try
{
    if (args.Length != 0 && args is not ["--demo"]) throw new ArgumentException("ISOLATION_ARGS_INVALID");
    var source = new[] { new InjectedInput(0, 0, Buttons.None), new InjectedInput(1, -1, Buttons.Jump),
        new InjectedInput(1, 0, Buttons.Jump | Buttons.Grab), new InjectedInput(-1, 1, Buttons.Dash),
        new InjectedInput(0, 0, Buttons.None) };
    using var session = new IsolationSession(source);
    using var replay = new IsolationSession(source);
    Check(!session.OriginalBound && session.BindingStatus == "ORIGINAL_BRIDGE_NOT_BOUND");
    var contexts = new List<StepContext>();
    for (var i = 0; i < source.Length; i++)
    {
        var context = session.Advance(); contexts.Add(context);
        Check(context == replay.Advance());
        Check(context.Tick == i && context.ElapsedSeconds == i / 60d && context.DeltaSeconds == 1f / 60f);
        Check(context.Input == source[i]);
    }
    Check(contexts[1].Pressed == Buttons.Jump && contexts[1].Released == Buttons.None);
    Check(contexts[2].Pressed == Buttons.Grab && contexts[2].Released == Buttons.None);
    Check(contexts[3].Pressed == Buttons.Dash && contexts[3].Released == (Buttons.Jump | Buttons.Grab));
    Check(contexts[4].Pressed == Buttons.None && contexts[4].Released == Buttons.Dash);
    Check(session.RemainingFrames == 0);
    Reject<InvalidOperationException>(() => session.Advance());
    Check(session.RemainingFrames == 0);
    foreach (var service in Enum.GetValues<ExternalService>())
    {
        var result = session.RequestService(service);
        Check(!result.Allowed && result.Code == "ISOLATION_SERVICE_DENIED" && result.Service == service);
    }
    Reject<ArgumentOutOfRangeException>(() => session.RequestService((ExternalService)999));
    Reject<ArgumentNullException>(() => new IsolationSession(null!));
    Reject<ArgumentOutOfRangeException>(() => new IsolationSession(Array.Empty<InjectedInput>()));
    Reject<ArgumentOutOfRangeException>(() => new IsolationSession(new InjectedInput[IsolationSession.MaximumFrames + 1]));
    Reject<ArgumentException>(() => new IsolationSession(new[] { new InjectedInput(2, 0, Buttons.None) }));
    Reject<ArgumentException>(() => new IsolationSession(new[] { new InjectedInput(0, -2, Buttons.None) }));
    Reject<ArgumentException>(() => new IsolationSession(new[] { new InjectedInput(0, 0, (Buttons)8) }));
    var mutable = new[] { new InjectedInput(1, 0, Buttons.Grab) };
    using var copied = new IsolationSession(mutable);
    mutable[0] = new InjectedInput(-1, 0, Buttons.None);
    Check(copied.Advance().Input == new InjectedInput(1, 0, Buttons.Grab));
    using var maximum = new IsolationSession(new InjectedInput[IsolationSession.MaximumFrames]);
    for (var i = 0; i < IsolationSession.MaximumFrames; i++)
    {
        var step = maximum.Advance();
        if (step.Tick != i || step.ElapsedSeconds != i / 60d) throw new InvalidOperationException("ISOLATION_TIME_DRIFT");
    }
    Check(maximum.RemainingFrames == 0);
    session.Dispose(); session.Dispose();
    Reject<ObjectDisposedException>(() => session.Advance());
    Reject<ObjectDisposedException>(() => session.RequestService(ExternalService.Audio));
    Reject<ObjectDisposedException>(() => { _ = session.RemainingFrames; });
    Check(typeof(IsolationSession).Assembly.GetReferencedAssemblies().All(a => a.Name?.StartsWith("System", StringComparison.Ordinal) == true));
    Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name is "Celeste" or "Steamworks.NET" or "OriginalCoreCompileProbe"));
    if (args is ["--demo"])
    {
        foreach (var step in contexts)
            Console.WriteLine(JsonSerializer.Serialize(new { timestampUtc = DateTime.UtcNow, runId,
                eventId = "ISOLATION_CONTEXT_ADVANCED", subsystem = "RuntimeIsolation", severity = "Info",
                stage = "injected-context", outcome = "succeeded", durationMs = 0, tick = step.Tick,
                deltaSeconds = step.DeltaSeconds, input = step.Input, pressed = step.Pressed, released = step.Released,
                originalBound = false, recoveredCodeExecuted = false }));
        foreach (var service in Enum.GetValues<ExternalService>())
            Console.WriteLine(JsonSerializer.Serialize(new { timestampUtc = DateTime.UtcNow, runId,
                eventId = "ISOLATION_SERVICE_DENIED", subsystem = "RuntimeIsolation", severity = "Warning",
                stage = "service-policy", outcome = "denied", durationMs = 0, service = service.ToString(), allowed = false }));
    }
    Console.WriteLine($"ISOLATION_VERIFIED passed={checks} failed=0 originalBound=false recoveredCodeExecuted=false");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new { timestampUtc = DateTime.UtcNow, runId,
        eventId = "ISOLATION_VERIFICATION_FAILED", subsystem = "RuntimeIsolation", stage = "synthetic-verification",
        severity = "Error", outcome = "failed", durationMs = 0, exceptionType = exception.GetType().Name,
        message = exception.Message, hresult = exception.HResult, stack = exception.StackTrace,
        inner = exception.InnerException?.GetType().Name, recoverable = false }));
    return 1;
}
