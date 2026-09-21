using System.Threading.Channels;
using CelesteDesktop.AssetWorker.Client;
using CelesteDesktop.Contracts.AssetWorker;

namespace CelesteDesktop.AssetWorker.Tests;

internal sealed class ScriptedAssetWorkerSession : IAssetWorkerSession
{
    private readonly Channel<object> _received = Channel.CreateUnbounded<object>();
    private readonly TaskCompletionSource<int> _completion = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public Func<AssetWorkerEnvelope, ScriptedAssetWorkerSession, ValueTask>? OnSend { get; set; }

    public Task<int> Completion => _completion.Task;

    public bool Terminated { get; private set; }

    public bool Disposed { get; private set; }

    public bool HangOnSend { get; set; }

    public bool IgnoreReceiveCancellation { get; set; }

    public bool HangOnTerminate { get; set; }

    public bool HangOnDispose { get; set; }

    public ValueTask SendAsync(
        AssetWorkerEnvelope envelope,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (HangOnSend)
        {
            return new ValueTask(Task.Delay(Timeout.InfiniteTimeSpan));
        }

        return OnSend is null
            ? ValueTask.CompletedTask
            : OnSend(envelope, this);
    }

    public async ValueTask<AssetWorkerEnvelope> ReceiveAsync(
        CancellationToken cancellationToken)
    {
        var item = await _received.Reader
            .ReadAsync(
                IgnoreReceiveCancellation
                    ? CancellationToken.None
                    : cancellationToken)
            .ConfigureAwait(false);
        if (item is Exception exception)
        {
            throw exception;
        }

        return (AssetWorkerEnvelope)item;
    }

    public ValueTask TerminateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Terminated = true;
        if (HangOnTerminate)
        {
            return new ValueTask(Task.Delay(Timeout.InfiniteTimeSpan));
        }

        _completion.TrySetResult(-1);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        if (HangOnDispose)
        {
            return new ValueTask(Task.Delay(Timeout.InfiniteTimeSpan));
        }

        _completion.TrySetResult(-1);
        return ValueTask.CompletedTask;
    }

    public void Enqueue(AssetWorkerEnvelope envelope) =>
        _received.Writer.TryWrite(envelope);

    public void Enqueue(Exception exception) =>
        _received.Writer.TryWrite(exception);

    public void Exit(int exitCode) => _completion.TrySetResult(exitCode);
}
