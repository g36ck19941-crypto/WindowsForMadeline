using System.Buffers.Binary;
using System.Text;
using CelesteDesktop.AssetWorker.Client;
using CelesteDesktop.AssetWorker.Protocol;
using CelesteDesktop.AssetWorker.Tests;
using CelesteDesktop.Contracts.AssetWorker;

var tests = new (string Name, Func<Task> Body)[]
{
    ("protocol frame round-trips", ProtocolFrameRoundTrips),
    ("truncated header is rejected", TruncatedHeaderIsRejected),
    ("truncated payload is rejected", TruncatedPayloadIsRejected),
    ("oversized frame is rejected before allocation", OversizedFrameIsRejected),
    ("invalid JSON is rejected", InvalidJsonIsRejected),
    ("unsupported version is rejected", UnsupportedVersionIsRejected),
    ("invalid error envelope is rejected", InvalidErrorEnvelopeIsRejected),
    ("arbitrary worker executable is rejected", ArbitraryWorkerExecutableIsRejected),
    ("non-dotnet host is rejected", NonDotNetHostIsRejected),
    ("supervisor reaches ready", SupervisorReachesReady),
    ("ping succeeds", PingSucceeds),
    ("ping before start is bounded failure", PingBeforeStartFails),
    ("startup timeout terminates session", StartupTimeoutTerminatesSession),
    ("send ignoring cancellation is bounded", SendIgnoringCancellationIsBounded),
    ("receive ignoring cancellation is bounded", ReceiveIgnoringCancellationIsBounded),
    ("caller cancellation terminates session", CallerCancellationTerminatesSession),
    ("worker exit during start is contained", ExitDuringStartIsContained),
    ("malformed handshake is contained", MalformedHandshakeIsContained),
    ("unexpected handshake is contained", UnexpectedHandshakeIsContained),
    ("ping timeout terminates session", PingTimeoutTerminatesSession),
    ("worker crash during ping is contained", CrashDuringPingIsContained),
    ("shutdown timeout forces termination", ShutdownTimeoutForcesTermination),
    ("terminate ignoring cancellation is bounded", TerminateIgnoringCancellationIsBounded),
    ("hanging dispose is bounded", HangingDisposeIsBounded),
    ("new session recovers after crash", NewSessionRecoversAfterCrash),
    ("disposed supervisor rejects restart", DisposedSupervisorRejectsRestart),
    ("real worker process completes lifecycle", RealWorkerProcessCompletesLifecycle)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        await test.Body().ConfigureAwait(false);
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

static async Task ProtocolFrameRoundTrips()
{
    var codec = new AssetWorkerProtocolCodec();
    var expected = Envelope(AssetWorkerMessageType.Ping, Guid.NewGuid());
    await using var stream = new MemoryStream(codec.Encode(expected));

    var actual = await codec.ReadAsync(stream, CancellationToken.None);

    Assert(actual == expected, "Round-tripped envelope differs.");
}

static async Task TruncatedHeaderIsRejected()
{
    var codec = new AssetWorkerProtocolCodec();
    await using var stream = new MemoryStream(new byte[] { 1, 0 });

    await AssertProtocolFailure(
        () => codec.ReadAsync(stream, CancellationToken.None).AsTask(),
        AssetWorkerProtocolCodes.FrameTruncated);
}

static async Task TruncatedPayloadIsRejected()
{
    var bytes = new byte[6];
    BinaryPrimitives.WriteInt32LittleEndian(bytes, 10);
    bytes[4] = (byte)'{';
    bytes[5] = (byte)'}';
    var codec = new AssetWorkerProtocolCodec();
    await using var stream = new MemoryStream(bytes);

    await AssertProtocolFailure(
        () => codec.ReadAsync(stream, CancellationToken.None).AsTask(),
        AssetWorkerProtocolCodes.FrameTruncated);
}

static async Task OversizedFrameIsRejected()
{
    var bytes = new byte[AssetWorkerProtocolConstants.LengthPrefixBytes];
    BinaryPrimitives.WriteInt32LittleEndian(
        bytes,
        AssetWorkerProtocolConstants.MaximumPayloadBytes + 1);
    var codec = new AssetWorkerProtocolCodec();
    await using var stream = new MemoryStream(bytes);

    await AssertProtocolFailure(
        () => codec.ReadAsync(stream, CancellationToken.None).AsTask(),
        AssetWorkerProtocolCodes.FrameTooLarge);
}

static async Task InvalidJsonIsRejected()
{
    var codec = new AssetWorkerProtocolCodec();
    await using var stream = new MemoryStream(Frame("not-json"));

    await AssertProtocolFailure(
        () => codec.ReadAsync(stream, CancellationToken.None).AsTask(),
        AssetWorkerProtocolCodes.JsonInvalid);
}

static async Task UnsupportedVersionIsRejected()
{
    var requestId = Guid.NewGuid();
    var json = $$"""
        {"ProtocolVersion":99,"MessageType":3,"RequestId":"{{requestId}}","ErrorCode":null}
        """;
    var codec = new AssetWorkerProtocolCodec();
    await using var stream = new MemoryStream(Frame(json));

    await AssertProtocolFailure(
        () => codec.ReadAsync(stream, CancellationToken.None).AsTask(),
        AssetWorkerProtocolCodes.VersionUnsupported);
}

static Task InvalidErrorEnvelopeIsRejected()
{
    var codec = new AssetWorkerProtocolCodec();
    try
    {
        codec.Encode(new AssetWorkerEnvelope(
            AssetWorkerProtocolConstants.Version,
            AssetWorkerMessageType.Error,
            Guid.NewGuid()));
    }
    catch (AssetWorkerProtocolException exception)
    {
        Assert(
            exception.Code == AssetWorkerProtocolCodes.ErrorCodeInvalid,
            "Unexpected protocol code.");
        return Task.CompletedTask;
    }

    throw new InvalidOperationException("Expected invalid Error envelope rejection.");
}

static Task ArbitraryWorkerExecutableIsRejected()
{
    var invalidPath = Path.Combine(
        Path.GetPathRoot(Path.GetFullPath(Environment.CurrentDirectory))!,
        "not-the-worker.exe");
    try
    {
        AssetWorkerLaunchOptions.ForFrameworkDependentWorker(
            "dotnet",
            invalidPath);
    }
    catch (ArgumentException)
    {
        return Task.CompletedTask;
    }

    throw new InvalidOperationException("Arbitrary worker path was accepted.");
}

static Task NonDotNetHostIsRejected()
{
    var workerPath = WorkerAssemblyPath();
    try
    {
        AssetWorkerLaunchOptions.ForFrameworkDependentWorker(
            "Celeste.exe",
            workerPath);
    }
    catch (ArgumentException)
    {
        return Task.CompletedTask;
    }

    throw new InvalidOperationException("Non-dotnet process host was accepted.");
}

static async Task SupervisorReachesReady()
{
    var session = ReadySession();
    await using var supervisor = Supervisor(session);

    var result = await supervisor.StartAsync();

    Assert(result.Succeeded, result.Code);
    Assert(supervisor.State == AssetWorkerState.Ready, "Supervisor did not reach Ready.");
}

static async Task PingSucceeds()
{
    var session = ReadySession();
    await using var supervisor = Supervisor(session);
    Assert((await supervisor.StartAsync()).Succeeded, "Start failed.");

    var result = await supervisor.PingAsync();

    Assert(result.Succeeded, result.Code);
    Assert(supervisor.State == AssetWorkerState.Ready, "Ping changed Ready state.");
}

static async Task PingBeforeStartFails()
{
    await using var supervisor = Supervisor(ReadySession());

    var result = await supervisor.PingAsync();

    Assert(!result.Succeeded, "Ping before start must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.NotReady, result.Code);
}

static async Task StartupTimeoutTerminatesSession()
{
    var session = new ScriptedAssetWorkerSession();
    await using var supervisor = Supervisor(session, FastOptions());

    var result = await supervisor.StartAsync();

    Assert(!result.Succeeded, "Hanging startup must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.StartTimeout, result.Code);
    Assert(session.Terminated, "Timed-out session was not terminated.");
}

static async Task SendIgnoringCancellationIsBounded()
{
    var session = new ScriptedAssetWorkerSession
    {
        HangOnSend = true
    };
    await using var supervisor = Supervisor(session, FastOptions());
    var startedAt = DateTime.UtcNow;

    var result = await supervisor.StartAsync();
    var elapsed = DateTime.UtcNow - startedAt;

    Assert(!result.Succeeded, "Hanging send must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.StartTimeout, result.Code);
    Assert(session.Terminated, "Hanging send session was not terminated.");
    Assert(elapsed < TimeSpan.FromSeconds(1), "Hanging send was not bounded.");
}

static async Task ReceiveIgnoringCancellationIsBounded()
{
    var session = new ScriptedAssetWorkerSession
    {
        IgnoreReceiveCancellation = true
    };
    await using var supervisor = Supervisor(session, FastOptions());
    var startedAt = DateTime.UtcNow;

    var result = await supervisor.StartAsync();
    var elapsed = DateTime.UtcNow - startedAt;

    Assert(!result.Succeeded, "Hanging receive must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.StartTimeout, result.Code);
    Assert(session.Terminated, "Hanging receive session was not terminated.");
    Assert(elapsed < TimeSpan.FromSeconds(1), "Hanging receive was not bounded.");
}

static async Task CallerCancellationTerminatesSession()
{
    var session = new ScriptedAssetWorkerSession();
    await using var supervisor = Supervisor(
        session,
        new AssetWorkerSupervisorOptions(
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100)));
    using var cancellation = new CancellationTokenSource(
        TimeSpan.FromMilliseconds(30));

    var result = await supervisor.StartAsync(cancellation.Token);

    Assert(!result.Succeeded, "Cancelled startup must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.Cancelled, result.Code);
    Assert(session.Terminated, "Cancelled session was not terminated.");
}

static async Task ExitDuringStartIsContained()
{
    var session = new ScriptedAssetWorkerSession
    {
        OnSend = static (_, current) =>
        {
            current.Exit(23);
            return ValueTask.CompletedTask;
        }
    };
    await using var supervisor = Supervisor(session);

    var result = await supervisor.StartAsync();

    Assert(!result.Succeeded, "Exited worker must fail startup.");
    Assert(result.Code == AssetWorkerOperationCodes.Exited, result.Code);
    Assert(result.ExitCode == 23, "Exit code was not preserved.");
}

static async Task MalformedHandshakeIsContained()
{
    var session = new ScriptedAssetWorkerSession
    {
        OnSend = static (request, current) =>
        {
            current.Enqueue(new AssetWorkerEnvelope(
                99,
                AssetWorkerMessageType.Ready,
                request.RequestId));
            return ValueTask.CompletedTask;
        }
    };
    await using var supervisor = Supervisor(session);

    var result = await supervisor.StartAsync();

    Assert(!result.Succeeded, "Malformed handshake must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.ProtocolRejected, result.Code);
    Assert(
        result.DetailCode == AssetWorkerProtocolCodes.VersionUnsupported,
        result.DetailCode ?? "missing detail");
    Assert(session.Terminated, "Malformed session was not terminated.");
}

static async Task UnexpectedHandshakeIsContained()
{
    var session = new ScriptedAssetWorkerSession
    {
        OnSend = static (request, current) =>
        {
            current.Enqueue(Envelope(
                AssetWorkerMessageType.Pong,
                request.RequestId));
            return ValueTask.CompletedTask;
        }
    };
    await using var supervisor = Supervisor(session);

    var result = await supervisor.StartAsync();

    Assert(!result.Succeeded, "Unexpected handshake must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.UnexpectedResponse, result.Code);
    Assert(session.Terminated, "Unexpected session was not terminated.");
}

static async Task PingTimeoutTerminatesSession()
{
    var session = ReadySession(respondToPing: false);
    await using var supervisor = Supervisor(session, FastOptions());
    Assert((await supervisor.StartAsync()).Succeeded, "Start failed.");

    var result = await supervisor.PingAsync();

    Assert(!result.Succeeded, "Hanging ping must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.RequestTimeout, result.Code);
    Assert(session.Terminated, "Timed-out ping session was not terminated.");
}

static async Task ShutdownTimeoutForcesTermination()
{
    var session = ReadySession(respondToShutdown: false);
    await using var supervisor = Supervisor(session, FastOptions());
    Assert((await supervisor.StartAsync()).Succeeded, "Start failed.");

    var result = await supervisor.StopAsync();

    Assert(!result.Succeeded, "Hanging shutdown must report failure.");
    Assert(result.Code == AssetWorkerOperationCodes.ShutdownTimeout, result.Code);
    Assert(session.Terminated, "Hanging worker was not terminated.");
    Assert(supervisor.State == AssetWorkerState.Stopped, "Supervisor did not stop.");
}

static async Task TerminateIgnoringCancellationIsBounded()
{
    var session = new ScriptedAssetWorkerSession
    {
        HangOnTerminate = true
    };
    await using var supervisor = Supervisor(session, FastOptions());
    var startedAt = DateTime.UtcNow;

    var result = await supervisor.StartAsync();
    var elapsed = DateTime.UtcNow - startedAt;

    Assert(!result.Succeeded, "Hanging startup must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.StartTimeout, result.Code);
    Assert(session.Terminated, "Termination was not attempted.");
    Assert(elapsed < TimeSpan.FromSeconds(1), "Hanging terminate was not bounded.");
}

static async Task CrashDuringPingIsContained()
{
    var session = new ScriptedAssetWorkerSession
    {
        OnSend = static (request, current) =>
        {
            if (request.MessageType == AssetWorkerMessageType.Hello)
            {
                current.Enqueue(Envelope(
                    AssetWorkerMessageType.Ready,
                    request.RequestId));
            }
            else if (request.MessageType == AssetWorkerMessageType.Ping)
            {
                current.Exit(42);
            }

            return ValueTask.CompletedTask;
        }
    };
    await using var supervisor = Supervisor(session, FastOptions());
    Assert((await supervisor.StartAsync()).Succeeded, "Start failed.");

    var result = await supervisor.PingAsync();

    Assert(!result.Succeeded, "Crashed ping must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.Exited, result.Code);
    Assert(result.ExitCode == 42, "Crash exit code was not preserved.");
    Assert(supervisor.State == AssetWorkerState.Faulted, "Crash was not marked Faulted.");
}

static async Task NewSessionRecoversAfterCrash()
{
    var crashed = new ScriptedAssetWorkerSession
    {
        OnSend = static (_, current) =>
        {
            current.Exit(31);
            return ValueTask.CompletedTask;
        }
    };
    var recovered = ReadySession();
    var factory = new QueuedAssetWorkerSessionFactory(crashed, recovered);
    await using var supervisor = new AssetWorkerSupervisor(factory, FastOptions());

    var first = await supervisor.StartAsync();
    var second = await supervisor.StartAsync();

    Assert(!first.Succeeded, "First crashed session should fail.");
    Assert(second.Succeeded, second.Code);
    Assert(factory.Starts == 2, "Recovery did not create a fresh session.");
    Assert(supervisor.State == AssetWorkerState.Ready, "Recovery did not reach Ready.");
}

static async Task HangingDisposeIsBounded()
{
    var session = new ScriptedAssetWorkerSession
    {
        HangOnDispose = true
    };
    await using var supervisor = Supervisor(session, FastOptions());
    var startedAt = DateTime.UtcNow;

    var result = await supervisor.StartAsync();
    var elapsed = DateTime.UtcNow - startedAt;

    Assert(!result.Succeeded, "Hanging startup must fail.");
    Assert(result.Code == AssetWorkerOperationCodes.StartTimeout, result.Code);
    Assert(elapsed < TimeSpan.FromSeconds(1), "Cleanup dispose was not bounded.");
}

static async Task DisposedSupervisorRejectsRestart()
{
    var supervisor = Supervisor(ReadySession());
    await supervisor.DisposeAsync();

    var result = await supervisor.StartAsync();

    Assert(!result.Succeeded, "Disposed supervisor must reject start.");
    Assert(result.Code == AssetWorkerOperationCodes.Disposed, result.Code);
}

static async Task RealWorkerProcessCompletesLifecycle()
{
    var workerPath = WorkerAssemblyPath();
    Assert(File.Exists(workerPath), $"Worker output missing: {workerPath}");

    var factory = new ProcessAssetWorkerSessionFactory(
        AssetWorkerLaunchOptions.ForFrameworkDependentWorker(
            "dotnet",
            workerPath));
    await using var supervisor = new AssetWorkerSupervisor(
        factory,
        new AssetWorkerSupervisorOptions(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(2)));

    var start = await supervisor.StartAsync();
    var ping = await supervisor.PingAsync();
    var stop = await supervisor.StopAsync();

    Assert(start.Succeeded, $"Real start failed: {start.Code}/{start.DetailCode}");
    Assert(ping.Succeeded, $"Real ping failed: {ping.Code}/{ping.DetailCode}");
    Assert(stop.Succeeded, $"Real stop failed: {stop.Code}/{stop.DetailCode}");
    Assert(supervisor.State == AssetWorkerState.Stopped, "Real worker did not stop.");
}

static string WorkerAssemblyPath() => Path.Combine(
    Environment.CurrentDirectory,
    "src",
    "CelesteDesktop.AssetWorker.Process",
    "bin",
    "Release",
    "net8.0",
    "CelesteDesktop.AssetWorker.Process.dll");

static AssetWorkerSupervisor Supervisor(
    ScriptedAssetWorkerSession session,
    AssetWorkerSupervisorOptions? options = null) => new(
        new QueuedAssetWorkerSessionFactory(session),
        options ?? FastOptions());

static AssetWorkerSupervisorOptions FastOptions() => new(
    TimeSpan.FromMilliseconds(150),
    TimeSpan.FromMilliseconds(100),
    TimeSpan.FromMilliseconds(100));

static ScriptedAssetWorkerSession ReadySession(
    bool respondToPing = true,
    bool respondToShutdown = true)
{
    return new ScriptedAssetWorkerSession
    {
        OnSend = (request, current) =>
        {
            switch (request.MessageType)
            {
                case AssetWorkerMessageType.Hello:
                    current.Enqueue(Envelope(
                        AssetWorkerMessageType.Ready,
                        request.RequestId));
                    break;

                case AssetWorkerMessageType.Ping when respondToPing:
                    current.Enqueue(Envelope(
                        AssetWorkerMessageType.Pong,
                        request.RequestId));
                    break;

                case AssetWorkerMessageType.Shutdown when respondToShutdown:
                    current.Enqueue(Envelope(
                        AssetWorkerMessageType.Stopped,
                        request.RequestId));
                    current.Exit(0);
                    break;
            }

            return ValueTask.CompletedTask;
        }
    };
}

static AssetWorkerEnvelope Envelope(
    AssetWorkerMessageType messageType,
    Guid requestId) => new(
        AssetWorkerProtocolConstants.Version,
        messageType,
        requestId);

static byte[] Frame(string text)
{
    var payload = Encoding.UTF8.GetBytes(text);
    var frame = new byte[
        AssetWorkerProtocolConstants.LengthPrefixBytes + payload.Length];
    BinaryPrimitives.WriteInt32LittleEndian(frame, payload.Length);
    payload.CopyTo(frame.AsSpan(AssetWorkerProtocolConstants.LengthPrefixBytes));
    return frame;
}

static async Task AssertProtocolFailure(
    Func<Task> operation,
    string expectedCode)
{
    try
    {
        await operation().ConfigureAwait(false);
    }
    catch (AssetWorkerProtocolException exception)
    {
        Assert(exception.Code == expectedCode, $"Expected {expectedCode}, got {exception.Code}.");
        return;
    }

    throw new InvalidOperationException($"Expected protocol failure {expectedCode}.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
