using CelesteDesktop.Contracts.AssetWorker;

namespace CelesteDesktop.AssetWorker.Client;

public interface IAssetWorkerSession : IAsyncDisposable
{
    Task<int> Completion { get; }

    ValueTask SendAsync(
        AssetWorkerEnvelope envelope,
        CancellationToken cancellationToken);

    ValueTask<AssetWorkerEnvelope> ReceiveAsync(
        CancellationToken cancellationToken);

    ValueTask TerminateAsync(CancellationToken cancellationToken);
}
