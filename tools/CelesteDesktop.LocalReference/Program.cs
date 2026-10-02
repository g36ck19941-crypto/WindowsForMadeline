using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CelesteDesktop.LocalReference;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = LocalReferenceCommandLine.Parse(args);
            var report = new LocalReferenceBuilder(new DotNetIlSpyRunner()).Build(options);
            Console.WriteLine(
                $"{LocalReferenceCodes.Completed} assembly={report.AssemblyFileName} " +
                $"sourceFiles={report.SourceFileCount} cacheKey={report.CacheKey}");
            return 0;
        }
        catch (LocalReferenceException exception)
        {
            Console.Error.WriteLine($"{exception.Code}: {exception.Message}");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"{LocalReferenceCodes.Unexpected}: {exception.GetType().Name}: {exception.Message}");
            return 3;
        }
    }
}

public sealed record LocalReferenceOptions(string InstallRoot, string CacheRoot);

public static class LocalReferenceCommandLine
{
    public static LocalReferenceOptions Parse(IReadOnlyList<string> args)
    {
        string? root = null;
        string? cacheRoot = null;
        for (var index = 0; index < args.Count; index += 2)
        {
            if (index + 1 >= args.Count)
            {
                throw Error(LocalReferenceCodes.ArgumentsInvalid, "Expected --root and --cache-root absolute paths.");
            }

            switch (args[index])
            {
                case "--root" when root is null:
                    root = args[index + 1];
                    break;
                case "--cache-root" when cacheRoot is null:
                    cacheRoot = args[index + 1];
                    break;
                default:
                    throw Error(LocalReferenceCodes.ArgumentsInvalid, "Only --root and --cache-root are supported.");
            }
        }

        if (root is null || cacheRoot is null || !Path.IsPathFullyQualified(root) || !Path.IsPathFullyQualified(cacheRoot))
        {
            throw Error(LocalReferenceCodes.ArgumentsInvalid, "Both paths must be explicit absolute paths.");
        }

        return new LocalReferenceOptions(Path.GetFullPath(root), Path.GetFullPath(cacheRoot));
    }

    private static LocalReferenceException Error(string code, string message) => new(code, message);
}

public static class LocalReferencePathGuard
{
    public static void Validate(string installRoot, string cacheRoot)
    {
        var install = WithSeparator(Path.GetFullPath(installRoot));
        var cache = WithSeparator(Path.GetFullPath(cacheRoot));
        if (cache.StartsWith(install, StringComparison.OrdinalIgnoreCase) ||
            install.StartsWith(cache, StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalReferenceException(
                LocalReferenceCodes.CacheOverlapsInstall,
                "The local cache and selected installation must not contain one another.");
        }

        if (!string.Equals(
                Path.GetFileName(Path.TrimEndingDirectorySeparator(cacheRoot)),
                "local-cache",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalReferenceException(
                LocalReferenceCodes.CacheRootInvalid,
                "The output root must be the repository local-cache directory.");
        }
    }

    public static void EnsureDescendant(string cacheRoot, string candidate)
    {
        var cache = WithSeparator(Path.GetFullPath(cacheRoot));
        var child = Path.GetFullPath(candidate);
        if (!child.StartsWith(cache, StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalReferenceException(LocalReferenceCodes.CacheEscape, "A cache path escaped local-cache.");
        }
    }

    private static string WithSeparator(string path) => Path.TrimEndingDirectorySeparator(path) + Path.DirectorySeparatorChar;
}

public interface ILocalDecompilerRunner
{
    DecompilerResult Run(DecompilerRequest request);
}

public sealed record DecompilerRequest(string AssemblyPath, string ReferenceRoot, string OutputDirectory);
public sealed record DecompilerResult(int ExitCode);

public sealed class DotNetIlSpyRunner : ILocalDecompilerRunner
{
    public DecompilerResult Run(DecompilerRequest request)
    {
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("tool");
        start.ArgumentList.Add("run");
        start.ArgumentList.Add("ilspycmd");
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("--disable-updatecheck");
        start.ArgumentList.Add("-p");
        start.ArgumentList.Add("-o");
        start.ArgumentList.Add(request.OutputDirectory);
        start.ArgumentList.Add("-r");
        start.ArgumentList.Add(request.ReferenceRoot);
        start.ArgumentList.Add(request.AssemblyPath);

        using var process = Process.Start(start) ?? throw new LocalReferenceException(
            LocalReferenceCodes.DecompilerUnavailable,
            "The pinned local ILSpy tool could not be started. Run dotnet tool restore first.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(output, error);
        return new DecompilerResult(process.ExitCode);
    }
}

public sealed class LocalReferenceBuilder(ILocalDecompilerRunner decompiler)
{
    public LocalReferenceReport Build(LocalReferenceOptions options)
    {
        LocalReferencePathGuard.Validate(options.InstallRoot, options.CacheRoot);
        if (!Directory.Exists(options.InstallRoot))
        {
            throw new LocalReferenceException(LocalReferenceCodes.InstallMissing, "The selected installation directory does not exist.");
        }

        var assemblyPath = SelectAssembly(options.InstallRoot);
        var info = new FileInfo(assemblyPath);
        var sha256 = HashFile(assemblyPath);
        var cacheKey = sha256[..16].ToLowerInvariant();
        var referenceRoot = Path.Combine(options.CacheRoot, "celeste-reference", cacheKey);
        var sourceRoot = Path.Combine(referenceRoot, "source");
        var manifestPath = Path.Combine(referenceRoot, "manifest.json");
        LocalReferencePathGuard.EnsureDescendant(options.CacheRoot, sourceRoot);

        if (File.Exists(manifestPath) && Directory.Exists(sourceRoot))
        {
            return ReadExisting(manifestPath, sha256);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(referenceRoot)!);
        var stagingRoot = referenceRoot + ".staging-" + Guid.NewGuid().ToString("N");
        var stagingSource = Path.Combine(stagingRoot, "source");
        LocalReferencePathGuard.EnsureDescendant(options.CacheRoot, stagingRoot);
        Directory.CreateDirectory(stagingSource);

        try
        {
            var result = decompiler.Run(new DecompilerRequest(assemblyPath, options.InstallRoot, stagingSource));
            if (result.ExitCode != 0)
            {
                throw new LocalReferenceException(
                    LocalReferenceCodes.DecompilerFailed,
                    $"The local decompiler exited with code {result.ExitCode}; its raw output was not persisted.");
            }

            var sourceCount = Directory.EnumerateFiles(stagingSource, "*.cs", SearchOption.AllDirectories).Count();
            if (sourceCount == 0)
            {
                throw new LocalReferenceException(LocalReferenceCodes.NoSourceProduced, "No C# reference files were produced.");
            }

            var report = new LocalReferenceReport(
                1,
                "CDR-070",
                info.Name,
                info.Length,
                sha256,
                cacheKey,
                "ilspycmd",
                "11.1.0.9782",
                sourceCount,
                false,
                0,
                false,
                false);
            File.WriteAllText(
                Path.Combine(stagingRoot, "manifest.json"),
                JsonSerializer.Serialize(report, JsonOptions));
            File.WriteAllText(
                Path.Combine(stagingRoot, ".commercial-reference-do-not-commit"),
                "Local analysis material. Never commit, publish, package, or redistribute.\n");

            if (Directory.Exists(referenceRoot))
            {
                throw new LocalReferenceException(LocalReferenceCodes.CacheCollision, "The immutable cache key already exists without a valid manifest.");
            }
            Directory.Move(stagingRoot, referenceRoot);
            return report;
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, recursive: true);
            }
        }
    }

    public static string SelectAssembly(string installRoot)
    {
        foreach (var name in new[] { "Celeste.dll", "Celeste.exe" })
        {
            var candidate = Path.Combine(installRoot, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new LocalReferenceException(LocalReferenceCodes.AssemblyMissing, "Neither Celeste.dll nor Celeste.exe exists in the selected directory.");
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static LocalReferenceReport ReadExisting(string manifestPath, string expectedHash)
    {
        var report = JsonSerializer.Deserialize<LocalReferenceReport>(File.ReadAllText(manifestPath), JsonOptions)
            ?? throw new LocalReferenceException(LocalReferenceCodes.ManifestInvalid, "The existing local manifest is empty.");
        if (!string.Equals(report.AssemblySha256, expectedHash, StringComparison.OrdinalIgnoreCase) ||
            report.SourceFileCount <= 0 || report.CommercialReferenceTracked || report.InstallationWrites != 0)
        {
            throw new LocalReferenceException(LocalReferenceCodes.ManifestInvalid, "The existing local manifest failed its safety checks.");
        }
        return report;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed record LocalReferenceReport(
    int SchemaVersion,
    string TaskId,
    string AssemblyFileName,
    long AssemblyLength,
    string AssemblySha256,
    string CacheKey,
    string Decompiler,
    string DecompilerVersion,
    int SourceFileCount,
    bool GameLaunched,
    int InstallationWrites,
    bool CommercialReferenceTracked,
    bool ExactParityEstablished);

public sealed class LocalReferenceException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class LocalReferenceCodes
{
    public const string ArgumentsInvalid = "LOCAL_REFERENCE_ARGUMENTS_INVALID";
    public const string CacheOverlapsInstall = "LOCAL_REFERENCE_CACHE_OVERLAPS_INSTALL";
    public const string CacheRootInvalid = "LOCAL_REFERENCE_CACHE_ROOT_INVALID";
    public const string CacheEscape = "LOCAL_REFERENCE_CACHE_ESCAPE";
    public const string InstallMissing = "LOCAL_REFERENCE_INSTALL_MISSING";
    public const string AssemblyMissing = "LOCAL_REFERENCE_ASSEMBLY_MISSING";
    public const string DecompilerUnavailable = "LOCAL_REFERENCE_DECOMPILER_UNAVAILABLE";
    public const string DecompilerFailed = "LOCAL_REFERENCE_DECOMPILER_FAILED";
    public const string NoSourceProduced = "LOCAL_REFERENCE_NO_SOURCE_PRODUCED";
    public const string CacheCollision = "LOCAL_REFERENCE_CACHE_COLLISION";
    public const string ManifestInvalid = "LOCAL_REFERENCE_MANIFEST_INVALID";
    public const string Unexpected = "LOCAL_REFERENCE_UNEXPECTED";
    public const string Completed = "LOCAL_REFERENCE_COMPLETED";
}
