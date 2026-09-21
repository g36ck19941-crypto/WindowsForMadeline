using System.Collections.ObjectModel;

namespace CelesteDesktop.AssetWorker.Client;

public sealed class AssetWorkerLaunchOptions
{
    private const string ExpectedWorkerAssembly =
        "CelesteDesktop.AssetWorker.Process.dll";

    private AssetWorkerLaunchOptions(
        string fileName,
        IEnumerable<string> arguments)
    {
        FileName = fileName;
        Arguments = new ReadOnlyCollection<string>(
            arguments.ToArray());
    }

    public string FileName { get; }

    public IReadOnlyList<string> Arguments { get; }

    public static AssetWorkerLaunchOptions ForFrameworkDependentWorker(
        string dotnetHost,
        string workerAssemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dotnetHost);
        ArgumentException.ThrowIfNullOrWhiteSpace(workerAssemblyPath);

        var hostName = Path.GetFileName(dotnetHost);
        if (!string.Equals(hostName, "dotnet", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(hostName, "dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Framework-dependent Worker launch requires the dotnet host.",
                nameof(dotnetHost));
        }

        if (!Path.IsPathFullyQualified(workerAssemblyPath) ||
            !string.Equals(
                Path.GetFileName(workerAssemblyPath),
                ExpectedWorkerAssembly,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Worker path must be a fully qualified {ExpectedWorkerAssembly} path.",
                nameof(workerAssemblyPath));
        }

        var attributes = File.GetAttributes(workerAssemblyPath);
        if ((attributes & FileAttributes.ReparsePoint) != 0 ||
            (attributes & FileAttributes.Directory) != 0)
        {
            throw new ArgumentException(
                "Worker assembly must be a regular non-reparse file.",
                nameof(workerAssemblyPath));
        }

        return new AssetWorkerLaunchOptions(
            dotnetHost,
            new[] { Path.GetFullPath(workerAssemblyPath) });
    }
}
