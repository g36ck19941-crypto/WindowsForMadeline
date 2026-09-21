using CelesteDesktop.AssetWorker.Protocol;
using CelesteDesktop.Contracts.AssetWorker;
using System.Text.Json;

return await AssetWorkerProgram.RunAsync().ConfigureAwait(false);

internal static class AssetWorkerProgram
{
    private const int SuccessExitCode = 0;
    private const int ProtocolFailureExitCode = 64;
    private static readonly Guid RunId = Guid.NewGuid();

    public static async Task<int> RunAsync()
    {
        var codec = new AssetWorkerProtocolCodec();
        await using var input = Console.OpenStandardInput();
        await using var output = Console.OpenStandardOutput();

        try
        {
            var hello = await codec.ReadAsync(
                input,
                CancellationToken.None).ConfigureAwait(false);
            if (hello.MessageType != AssetWorkerMessageType.Hello)
            {
                await SendErrorAsync(
                    codec,
                    output,
                    hello.RequestId,
                    AssetWorkerProtocolCodes.MessageUnexpected).ConfigureAwait(false);
                return ProtocolFailureExitCode;
            }

            await codec.WriteAsync(
                output,
                CreateEnvelope(AssetWorkerMessageType.Ready, hello.RequestId),
                CancellationToken.None).ConfigureAwait(false);

            while (true)
            {
                var request = await codec.ReadAsync(
                    input,
                    CancellationToken.None).ConfigureAwait(false);
                switch (request.MessageType)
                {
                    case AssetWorkerMessageType.Ping:
                        await codec.WriteAsync(
                            output,
                            CreateEnvelope(
                                AssetWorkerMessageType.Pong,
                                request.RequestId),
                            CancellationToken.None).ConfigureAwait(false);
                        break;

                    case AssetWorkerMessageType.Shutdown:
                        await codec.WriteAsync(
                            output,
                            CreateEnvelope(
                                AssetWorkerMessageType.Stopped,
                                request.RequestId),
                            CancellationToken.None).ConfigureAwait(false);
                        return SuccessExitCode;

                    default:
                        await SendErrorAsync(
                            codec,
                            output,
                            request.RequestId,
                            AssetWorkerProtocolCodes.MessageUnexpected).ConfigureAwait(false);
                        break;
                }
            }
        }
        catch (AssetWorkerProtocolException exception)
        {
            await WriteFailureAsync(
                "protocol-loop",
                exception.Code,
                exception.GetType().Name).ConfigureAwait(false);
            return ProtocolFailureExitCode;
        }
        catch (Exception exception) when (
            exception is IOException or OperationCanceledException)
        {
            await WriteFailureAsync(
                "stdio-loop",
                "WORKER_IO_FAILURE",
                exception.GetType().Name).ConfigureAwait(false);
            return ProtocolFailureExitCode;
        }
    }

    private static AssetWorkerEnvelope CreateEnvelope(
        AssetWorkerMessageType messageType,
        Guid requestId) => new(
            AssetWorkerProtocolConstants.Version,
            messageType,
            requestId);

    private static ValueTask SendErrorAsync(
        AssetWorkerProtocolCodec codec,
        Stream output,
        Guid requestId,
        string errorCode) => codec.WriteAsync(
            output,
            new AssetWorkerEnvelope(
                AssetWorkerProtocolConstants.Version,
                AssetWorkerMessageType.Error,
                requestId,
                errorCode),
            CancellationToken.None);

    private static Task WriteFailureAsync(
        string stage,
        string code,
        string exceptionType)
    {
        var record = JsonSerializer.Serialize(new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            runId = RunId,
            eventId = "ASSET_WORKER_PROCESS_FAILED",
            subsystem = "AssetWorker",
            severity = "Error",
            stage,
            outcome = "failed",
            code,
            exceptionType,
            recoverable = false
        });
        return Console.Error.WriteLineAsync(record);
    }
}
