using CelesteDesktop.AssetWorker.Client;

namespace CelesteDesktop.AssetWorker.Tests;

internal sealed class QueuedAssetWorkerSessionFactory : IAssetWorkerSessionFactory
{
    private readonly Queue<IAssetWorkerSession> _sessions;

    public QueuedAssetWorkerSessionFactory(params IAssetWorkerSession[] sessions)
    {
        _sessions = new Queue<IAssetWorkerSession>(sessions);
    }

    public int Starts { get; private set; }

    public ValueTask<IAssetWorkerSession> StartAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Starts++;
        if (_sessions.Count == 0)
        {
            throw new InvalidOperationException("No scripted session remains.");
        }

        return ValueTask.FromResult(_sessions.Dequeue());
    }
}
