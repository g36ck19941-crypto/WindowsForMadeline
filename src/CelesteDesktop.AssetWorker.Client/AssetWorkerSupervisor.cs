using CelesteDesktop.AssetWorker.Protocol;
using CelesteDesktop.Contracts.AssetWorker;

namespace CelesteDesktop.AssetWorker.Client;

public sealed class AssetWorkerSupervisor : IAsyncDisposable
{
    private readonly IAssetWorkerSessionFactory _sessionFactory;
    private readonly AssetWorkerSupervisorOptions _options;
    private readonly AssetWorkerProtocolCodec _codec;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private IAssetWorkerSession? _session;
    private AssetWorkerState _state = AssetWorkerState.Stopped;

    public AssetWorkerSupervisor(
        IAssetWorkerSessionFactory sessionFactory,
        AssetWorkerSupervisorOptions? options = null,
        AssetWorkerProtocolCodec? codec = null)
    {
        _sessionFactory = sessionFactory ??
            throw new ArgumentNullException(nameof(sessionFactory));
        _options = options ?? AssetWorkerSupervisorOptions.Default;
        _options.Validate();
        _codec = codec ?? new AssetWorkerProtocolCodec();
    }

    public AssetWorkerState State => _state;

    public async ValueTask<AssetWorkerOperationResult> StartAsync(
        CancellationToken cancellationToken = default)
    {
        if (!await TryEnterGateAsync(cancellationToken).ConfigureAwait(false))
        {
            return CancelledResult();
        }

        try
        {
            if (_state == AssetWorkerState.Disposed)
            {
                return Failure(AssetWorkerOperationCodes.Disposed);
            }

            if (_state == AssetWorkerState.Ready)
            {
                return Success(AssetWorkerOperationCodes.AlreadyReady);
            }

            await CleanupSessionAsync().ConfigureAwait(false);
            _state = AssetWorkerState.Starting;

            using var operation = CreateOperationToken(
                cancellationToken,
                _options.StartupTimeout);
            try
            {
                _session = await _sessionFactory
                    .StartAsync(operation.Token)
                    .AsTask()
                    .WaitAsync(operation.Token)
                    .ConfigureAwait(false);

                var requestId = Guid.NewGuid();
                await _session.SendAsync(
                        CreateEnvelope(AssetWorkerMessageType.Hello, requestId),
                        operation.Token)
                    .AsTask()
                    .WaitAsync(operation.Token)
                    .ConfigureAwait(false);

                var response = await ReceiveOrExitAsync(
                    _session,
                    operation.Token).ConfigureAwait(false);
                ValidateExpected(
                    response,
                    AssetWorkerMessageType.Ready,
                    requestId);

                _state = AssetWorkerState.Ready;
                return Success(AssetWorkerOperationCodes.Succeeded);
            }
            catch (OperationCanceledException)
            {
                var callerCancelled = cancellationToken.IsCancellationRequested;
                await CleanupSessionAsync().ConfigureAwait(false);
                _state = callerCancelled
                    ? AssetWorkerState.Stopped
                    : AssetWorkerState.Faulted;
                return Failure(
                    callerCancelled
                        ? AssetWorkerOperationCodes.Cancelled
                        : AssetWorkerOperationCodes.StartTimeout,
                    recoveryAction: "session_terminated");
            }
            catch (AssetWorkerProtocolException exception)
            {
                return await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.ProtocolRejected,
                    exception.Code).ConfigureAwait(false);
            }
            catch (UnexpectedWorkerResponseException exception)
            {
                return await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.UnexpectedResponse,
                    exception.DetailCode).ConfigureAwait(false);
            }
            catch (AssetWorkerExitedException exception)
            {
                return await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.Exited,
                    exitCode: exception.ExitCode).ConfigureAwait(false);
            }
            catch (Exception exception) when (
                exception is IOException or
                InvalidOperationException or
                System.ComponentModel.Win32Exception)
            {
                return await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.LaunchFailed,
                    exception.GetType().Name).ConfigureAwait(false);
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async ValueTask<AssetWorkerOperationResult> PingAsync(
        CancellationToken cancellationToken = default)
    {
        if (!await TryEnterGateAsync(cancellationToken).ConfigureAwait(false))
        {
            return CancelledResult();
        }

        try
        {
            if (_state == AssetWorkerState.Disposed)
            {
                return Failure(AssetWorkerOperationCodes.Disposed);
            }

            if (_state != AssetWorkerState.Ready || _session is null)
            {
                return Failure(AssetWorkerOperationCodes.NotReady);
            }

            using var operation = CreateOperationToken(
                cancellationToken,
                _options.RequestTimeout);
            try
            {
                var requestId = Guid.NewGuid();
                await _session.SendAsync(
                        CreateEnvelope(AssetWorkerMessageType.Ping, requestId),
                        operation.Token)
                    .AsTask()
                    .WaitAsync(operation.Token)
                    .ConfigureAwait(false);

                var response = await ReceiveOrExitAsync(
                    _session,
                    operation.Token).ConfigureAwait(false);
                ValidateExpected(
                    response,
                    AssetWorkerMessageType.Pong,
                    requestId);

                return Success(AssetWorkerOperationCodes.Succeeded);
            }
            catch (OperationCanceledException)
            {
                var callerCancelled = cancellationToken.IsCancellationRequested;
                await CleanupSessionAsync().ConfigureAwait(false);
                _state = callerCancelled
                    ? AssetWorkerState.Stopped
                    : AssetWorkerState.Faulted;
                return Failure(
                    callerCancelled
                        ? AssetWorkerOperationCodes.Cancelled
                        : AssetWorkerOperationCodes.RequestTimeout,
                    recoveryAction: "session_terminated");
            }
            catch (AssetWorkerProtocolException exception)
            {
                return await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.ProtocolRejected,
                    exception.Code).ConfigureAwait(false);
            }
            catch (UnexpectedWorkerResponseException exception)
            {
                return await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.UnexpectedResponse,
                    exception.DetailCode).ConfigureAwait(false);
            }
            catch (AssetWorkerExitedException exception)
            {
                return await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.Exited,
                    exitCode: exception.ExitCode).ConfigureAwait(false);
            }
            catch (Exception exception) when (
                exception is IOException or InvalidOperationException)
            {
                return await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.Exited,
                    exception.GetType().Name).ConfigureAwait(false);
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async ValueTask<AssetWorkerOperationResult> StopAsync(
        CancellationToken cancellationToken = default)
    {
        if (!await TryEnterGateAsync(cancellationToken).ConfigureAwait(false))
        {
            return CancelledResult();
        }

        try
        {
            if (_state == AssetWorkerState.Disposed)
            {
                return Failure(AssetWorkerOperationCodes.Disposed);
            }

            if (_session is null)
            {
                _state = AssetWorkerState.Stopped;
                return Success(AssetWorkerOperationCodes.Succeeded);
            }

            using var operation = CreateOperationToken(
                cancellationToken,
                _options.ShutdownTimeout);
            try
            {
                var requestId = Guid.NewGuid();
                await _session.SendAsync(
                        CreateEnvelope(AssetWorkerMessageType.Shutdown, requestId),
                        operation.Token)
                    .AsTask()
                    .WaitAsync(operation.Token)
                    .ConfigureAwait(false);

                var response = await ReceiveOrExitAsync(
                    _session,
                    operation.Token).ConfigureAwait(false);
                ValidateExpected(
                    response,
                    AssetWorkerMessageType.Stopped,
                    requestId);

                await _session.Completion
                    .WaitAsync(operation.Token)
                    .ConfigureAwait(false);
                await CleanupSessionAsync().ConfigureAwait(false);
                _state = AssetWorkerState.Stopped;
                return Success(AssetWorkerOperationCodes.Succeeded);
            }
            catch (OperationCanceledException)
            {
                var callerCancelled = cancellationToken.IsCancellationRequested;
                await CleanupSessionAsync().ConfigureAwait(false);
                _state = AssetWorkerState.Stopped;
                return Failure(
                    callerCancelled
                        ? AssetWorkerOperationCodes.Cancelled
                        : AssetWorkerOperationCodes.ShutdownTimeout,
                    recoveryAction: "session_terminated");
            }
            catch (AssetWorkerProtocolException exception)
            {
                var result = await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.ProtocolRejected,
                    exception.Code).ConfigureAwait(false);
                _state = AssetWorkerState.Stopped;
                return result with { State = _state };
            }
            catch (UnexpectedWorkerResponseException exception)
            {
                var result = await FailAndCleanupAsync(
                    AssetWorkerOperationCodes.UnexpectedResponse,
                    exception.DetailCode).ConfigureAwait(false);
                _state = AssetWorkerState.Stopped;
                return result with { State = _state };
            }
            catch (AssetWorkerExitedException exception)
            {
                await CleanupSessionAsync().ConfigureAwait(false);
                _state = AssetWorkerState.Stopped;
                return Failure(
                    AssetWorkerOperationCodes.Exited,
                    exitCode: exception.ExitCode,
                    recoveryAction: "session_reaped");
            }
            catch (Exception exception) when (
                exception is IOException or InvalidOperationException)
            {
                await CleanupSessionAsync().ConfigureAwait(false);
                _state = AssetWorkerState.Stopped;
                return Failure(
                    AssetWorkerOperationCodes.Exited,
                    detailCode: exception.GetType().Name,
                    recoveryAction: "session_terminated");
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _operationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_state == AssetWorkerState.Disposed)
            {
                return;
            }

            await CleanupSessionAsync().ConfigureAwait(false);
            _state = AssetWorkerState.Disposed;
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private static AssetWorkerEnvelope CreateEnvelope(
        AssetWorkerMessageType messageType,
        Guid requestId) => new(
            AssetWorkerProtocolConstants.Version,
            messageType,
            requestId);

    private void ValidateExpected(
        AssetWorkerEnvelope response,
        AssetWorkerMessageType expectedType,
        Guid expectedRequestId)
    {
        _codec.ValidateEnvelope(response);

        if (response.MessageType == AssetWorkerMessageType.Error)
        {
            throw new AssetWorkerProtocolException(response.ErrorCode!);
        }

        if (response.MessageType != expectedType ||
            response.RequestId != expectedRequestId)
        {
            throw new UnexpectedWorkerResponseException(
                AssetWorkerProtocolCodes.MessageUnexpected);
        }
    }

    private static async ValueTask<AssetWorkerEnvelope> ReceiveOrExitAsync(
        IAssetWorkerSession session,
        CancellationToken cancellationToken)
    {
        using var receiveCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var receiveTask = session
            .ReceiveAsync(receiveCancellation.Token)
            .AsTask();
        var cancellationTask = Task.Delay(
            Timeout.InfiniteTimeSpan,
            cancellationToken);
        var completed = await Task.WhenAny(
            receiveTask,
            session.Completion,
            cancellationTask).ConfigureAwait(false);

        if (completed == receiveTask)
        {
            return await receiveTask.ConfigureAwait(false);
        }

        receiveCancellation.Cancel();
        _ = ObserveReceiveCompletionAsync(receiveTask);
        if (completed == cancellationTask)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        var exitCode = await session.Completion.ConfigureAwait(false);
        throw new AssetWorkerExitedException(exitCode);
    }

    private static async Task ObserveReceiveCompletionAsync(Task receiveTask)
    {
        try
        {
            await receiveTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The operation that won the race already determines the public
            // result. Observe any late receive fault so it cannot surface as
            // an unobserved task exception.
        }
    }

    private async ValueTask<AssetWorkerOperationResult> FailAndCleanupAsync(
        string code,
        string? detailCode = null,
        int? exitCode = null)
    {
        await CleanupSessionAsync().ConfigureAwait(false);
        _state = AssetWorkerState.Faulted;
        return Failure(
            code,
            detailCode,
            exitCode,
            "session_terminated");
    }

    private async ValueTask CleanupSessionAsync()
    {
        var session = _session;
        _session = null;
        if (session is null)
        {
            return;
        }

        using var cleanup = new CancellationTokenSource(_options.ShutdownTimeout);
        try
        {
            await session.TerminateAsync(cleanup.Token)
                .AsTask()
                .WaitAsync(cleanup.Token)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is OperationCanceledException or
            InvalidOperationException or
            IOException)
        {
        }

        try
        {
            using var disposeTimeout = new CancellationTokenSource(
                _options.ShutdownTimeout);
            await session.DisposeAsync()
                .AsTask()
                .WaitAsync(disposeTimeout.Token)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is OperationCanceledException or
            InvalidOperationException or
            IOException)
        {
        }
    }

    private async ValueTask<bool> TryEnterGateAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _operationGate
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static CancellationTokenSource CreateOperationToken(
        CancellationToken cancellationToken,
        TimeSpan timeout)
    {
        var operation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        operation.CancelAfter(timeout);
        return operation;
    }

    private AssetWorkerOperationResult Success(string code) => new(
        true,
        code,
        _state);

    private AssetWorkerOperationResult Failure(
        string code,
        string? detailCode = null,
        int? exitCode = null,
        string? recoveryAction = null) => new(
            false,
            code,
            _state,
            detailCode,
            exitCode,
            recoveryAction);

    private AssetWorkerOperationResult CancelledResult() => new(
        false,
        AssetWorkerOperationCodes.Cancelled,
        _state);

    private sealed class AssetWorkerExitedException : Exception
    {
        public AssetWorkerExitedException(int exitCode)
            : base(AssetWorkerOperationCodes.Exited)
        {
            ExitCode = exitCode;
        }

        public int ExitCode { get; }
    }

    private sealed class UnexpectedWorkerResponseException : Exception
    {
        public UnexpectedWorkerResponseException(string detailCode)
            : base(detailCode)
        {
            DetailCode = detailCode;
        }

        public string DetailCode { get; }
    }
}
