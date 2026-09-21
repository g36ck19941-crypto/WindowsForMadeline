using System.Diagnostics;
using CelesteDesktop.AssetWorker.Protocol;
using CelesteDesktop.Contracts.AssetWorker;

namespace CelesteDesktop.AssetWorker.Client;

internal sealed class ProcessAssetWorkerSession : IAssetWorkerSession
{
    private readonly Process _process;
    private readonly AssetWorkerProtocolCodec _codec;
    private readonly Stream _input;
    private readonly Stream _output;
    private int _disposed;

    public ProcessAssetWorkerSession(
        Process process,
        AssetWorkerProtocolCodec codec)
    {
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        _input = process.StandardInput.BaseStream;
        _output = process.StandardOutput.BaseStream;
        Completion = ObserveCompletionAsync(process);
    }

    public Task<int> Completion { get; }

    public ValueTask SendAsync(
        AssetWorkerEnvelope envelope,
        CancellationToken cancellationToken) =>
        _codec.WriteAsync(_input, envelope, cancellationToken);

    public ValueTask<AssetWorkerEnvelope> ReceiveAsync(
        CancellationToken cancellationToken) =>
        _codec.ReadAsync(_output, cancellationToken);

    public async ValueTask TerminateAsync(CancellationToken cancellationToken)
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }

        await Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            _input.Close();
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            await Completion.ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and cleanup.
        }
        finally
        {
            _output.Dispose();
            _process.Dispose();
        }
    }

    private static async Task<int> ObserveCompletionAsync(Process process)
    {
        await process.WaitForExitAsync().ConfigureAwait(false);
        return process.ExitCode;
    }
}
