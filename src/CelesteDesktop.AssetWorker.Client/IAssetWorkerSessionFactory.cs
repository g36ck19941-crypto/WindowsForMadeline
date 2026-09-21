namespace CelesteDesktop.AssetWorker.Client;

public interface IAssetWorkerSessionFactory
{
    ValueTask<IAssetWorkerSession> StartAsync(
        CancellationToken cancellationToken);
}
