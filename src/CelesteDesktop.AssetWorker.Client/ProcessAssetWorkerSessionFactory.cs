using System.Diagnostics;
using CelesteDesktop.AssetWorker.Protocol;

namespace CelesteDesktop.AssetWorker.Client;

public sealed class ProcessAssetWorkerSessionFactory : IAssetWorkerSessionFactory
{
    private readonly AssetWorkerLaunchOptions _launchOptions;
    private readonly AssetWorkerProtocolCodec _codec;

    public ProcessAssetWorkerSessionFactory(
        AssetWorkerLaunchOptions launchOptions,
        AssetWorkerProtocolCodec? codec = null)
    {
        _launchOptions = launchOptions ??
            throw new ArgumentNullException(nameof(launchOptions));
        _codec = codec ?? new AssetWorkerProtocolCodec();
    }

    public ValueTask<IAssetWorkerSession> StartAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = _launchOptions.FileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = false
        };

        foreach (var argument in _launchOptions.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        try
        {
            if (!process.Start())
            {
                process.Dispose();
                throw new InvalidOperationException("AssetWorker process did not start.");
            }

            IAssetWorkerSession session = new ProcessAssetWorkerSession(
                process,
                _codec);
            return ValueTask.FromResult(session);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }
}
