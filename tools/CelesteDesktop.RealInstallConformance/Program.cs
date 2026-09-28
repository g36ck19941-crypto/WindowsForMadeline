using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CelesteDesktop.AssetWorker.Catalog;
using CelesteDesktop.AssetWorker.Data;
using CelesteDesktop.AssetWorker.Meta;
using CelesteDesktop.AssetWorker.SpriteXml;
using CelesteDesktop.Contracts.Assets;
using CelesteDesktop.Install;
using CelesteDesktop.Install.FileSystem;

try
{
    var arguments = CommandLine.Parse(args);
    ConformancePathGuard.EnsureOutputOutsideInstall(arguments.InstallRoot, arguments.OutputPath);
    var report = new RealInstallConformanceRunner().Run(arguments.InstallRoot);

    var outputDirectory = Path.GetDirectoryName(arguments.OutputPath)
        ?? throw new ConformanceException(ConformanceCodes.OutputInvalid);
    Directory.CreateDirectory(outputDirectory);
    File.WriteAllText(
        arguments.OutputPath,
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    Console.WriteLine("CONFORMANCE CDR-016 read-only selected-install verification");
    Console.WriteLine(
        $"RESULT passed={report.Passed.ToString().ToLowerInvariant()} " +
        $"source_files={report.SourceFileCount} atlas_pages={report.AtlasPageCount} " +
        $"atlas_entries={report.AtlasEntryCount} sprite_definitions={report.SpriteDefinitionCount} " +
        $"animations={report.AnimationCount} frames={report.ResolvedFrameCount} " +
        $"decoder_stable={report.DecoderFingerprintStable.ToString().ToLowerInvariant()} " +
        $"catalog_stable={report.CatalogFingerprintStable.ToString().ToLowerInvariant()} " +
        "installation_writes=0 commercial_bytes_persisted=0");
    Console.WriteLine($"REPORT {arguments.OutputPath}");
    return report.Passed ? 0 : 1;
}
catch (ConformanceException exception)
{
    Console.Error.WriteLine($"CONFORMANCE_FAILED code={exception.Code}");
    return 2;
}
catch (SpriteXmlException exception)
{
    Console.Error.WriteLine($"CONFORMANCE_FAILED code={exception.Code} stage=sprite-xml");
    return 2;
}
catch (AtlasMetadataException exception)
{
    Console.Error.WriteLine($"CONFORMANCE_FAILED code={exception.Code} stage=atlas-meta");
    return 2;
}
catch (AtlasDataException exception)
{
    Console.Error.WriteLine($"CONFORMANCE_FAILED code={exception.Code} stage=atlas-data");
    return 2;
}
catch (AssetCatalogException exception)
{
    var asset = exception.AssetId is null ? string.Empty : $" asset={exception.AssetId}";
    var detail = exception.Detail is null ? string.Empty : $" {exception.Detail}";
    Console.Error.WriteLine($"CONFORMANCE_FAILED code={exception.Code} stage=catalog{asset}{detail}");
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"CONFORMANCE_FAILED code={ConformanceCodes.Unexpected} type={exception.GetType().FullName}");
    return 2;
}

public static class ConformanceCodes
{
    public const string ArgumentsInvalid = "CONFORMANCE_ARGUMENTS_INVALID";
    public const string InstallInvalid = "CONFORMANCE_INSTALL_INVALID";
    public const string OutputInvalid = "CONFORMANCE_OUTPUT_INVALID";
    public const string OutputInsideInstall = "CONFORMANCE_OUTPUT_INSIDE_INSTALL";
    public const string SourceInvalid = "CONFORMANCE_SOURCE_INVALID";
    public const string SourceChanged = "CONFORMANCE_SOURCE_CHANGED";
    public const string DecoderUnstable = "CONFORMANCE_DECODER_UNSTABLE";
    public const string CatalogUnstable = "CONFORMANCE_CATALOG_UNSTABLE";
    public const string Unexpected = "CONFORMANCE_UNEXPECTED";
}

public sealed class ConformanceException : Exception
{
    public ConformanceException(string code)
        : base(code)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed record ConformanceArguments(string InstallRoot, string OutputPath);

public static class CommandLine
{
    public static ConformanceArguments Parse(string[] arguments)
    {
        if (arguments.Length != 4)
        {
            throw new ConformanceException(ConformanceCodes.ArgumentsInvalid);
        }

        string? root = null;
        string? output = null;
        for (var index = 0; index < arguments.Length; index += 2)
        {
            switch (arguments[index])
            {
                case "--root":
                    root = arguments[index + 1];
                    break;
                case "--output":
                    output = arguments[index + 1];
                    break;
                default:
                    throw new ConformanceException(ConformanceCodes.ArgumentsInvalid);
            }
        }

        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(output) ||
            !Path.IsPathFullyQualified(root) || !Path.IsPathFullyQualified(output))
        {
            throw new ConformanceException(ConformanceCodes.ArgumentsInvalid);
        }

        return new ConformanceArguments(Path.GetFullPath(root), Path.GetFullPath(output));
    }
}

public static class ConformancePathGuard
{
    public static void EnsureOutputOutsideInstall(string installRoot, string outputPath)
    {
        if (!Path.IsPathFullyQualified(installRoot) || !Path.IsPathFullyQualified(outputPath))
        {
            throw new ConformanceException(ConformanceCodes.OutputInvalid);
        }

        var canonicalRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installRoot));
        var canonicalOutput = Path.GetFullPath(outputPath);
        var prefix = canonicalRoot + Path.DirectorySeparatorChar;
        if (canonicalOutput.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(canonicalOutput, canonicalRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConformanceException(ConformanceCodes.OutputInsideInstall);
        }
    }
}

public sealed record ConformanceSourceSummary(
    string LogicalPath,
    long ByteLength,
    string Sha256);

public sealed record RealInstallConformanceReport(
    int SchemaVersion,
    string TaskId,
    string ProfileId,
    bool Passed,
    bool ReadOnly,
    bool GameLaunched,
    bool GuiLaunched,
    bool InstallationWritesObserved,
    int PersistedCommercialBytes,
    bool SelectedRootPersisted,
    int SourceFileCount,
    string SourceFingerprintSha256,
    IReadOnlyList<ConformanceSourceSummary> Sources,
    int AtlasPageCount,
    int AtlasEntryCount,
    int SpriteDefinitionCount,
    int AnimationCount,
    int ResolvedFrameCount,
    int DecodedPageCount,
    bool DecoderFingerprintStable,
    string DecoderFingerprintSha256,
    bool CatalogFingerprintStable,
    string CatalogFingerprintSha256);

public sealed class RealInstallConformanceRunner
{
    public static IReadOnlyList<string> RequiredSpriteIds { get; } =
        Array.AsReadOnly(new[] { "player", "theo_crystal", "glider", "bumper", "pufferFish" });

    public RealInstallConformanceReport Run(string selectedRoot)
    {
        var validation = new InstallVerifier(new SystemInstallFileSystem())
            .Validate(selectedRoot, CelesteInstallProfiles.WindowsFoundation);
        if (!validation.IsValid || validation.CanonicalRoot is null)
        {
            throw new ConformanceException(ConformanceCodes.InstallInvalid);
        }

        var root = validation.CanonicalRoot;
        var atlasDirectory = ResolveReadOnlyDirectory(root, "Content/Graphics/Atlases");
        var metadataPath = ResolveReadOnlyFile(root, "Content/Graphics/Atlases/Gameplay.meta");
        var spriteXmlPath = ResolveReadOnlyFile(root, "Content/Graphics/Sprites.xml");

        AtlasMetadataDescriptor atlas;
        using (var stream = OpenRead(metadataPath))
        {
            atlas = new AtlasMetadataReader().Read(stream);
        }

        var pageFiles = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var page in atlas.Pages)
        {
            var pagePath = ResolvePageFile(atlasDirectory, page.DataPath);
            if (!pageFiles.TryAdd(page.DataPath, pagePath))
            {
                throw new ConformanceException(ConformanceCodes.SourceInvalid);
            }
        }

        var sourcePaths = new List<(string LogicalPath, string AbsolutePath)>
        {
            ("Content/Graphics/Atlases/Gameplay.meta", metadataPath),
            ("Content/Graphics/Sprites.xml", spriteXmlPath)
        };
        sourcePaths.AddRange(pageFiles.Select(pair =>
            (Path.GetRelativePath(root, pair.Value).Replace('\\', '/'), pair.Value)));

        var before = sourcePaths
            .Select(pair => Capture(pair.LogicalPath, pair.AbsolutePath))
            .OrderBy(snapshot => snapshot.LogicalPath, StringComparer.Ordinal)
            .ToArray();

        SpriteMetadataDescriptor sprites;
        using (var stream = OpenRead(spriteXmlPath))
        {
            sprites = new SpriteXmlReader(RequiredSpriteIds).Read(stream);
        }

        var sourceFingerprint = new AssetSourceFingerprint(
            CelesteInstallProfiles.WindowsFoundation.Id,
            before.Select(snapshot => new AssetFileFingerprint(
                snapshot.LogicalPath,
                snapshot.ByteLength,
                snapshot.Sha256)));

        var firstCatalog = new AssetCatalogBuilder().Build(
            atlas,
            sprites,
            sourceFingerprint,
            RequiredSpriteIds,
            new ReadOnlyPageSource(pageFiles));
        var secondCatalog = new AssetCatalogBuilder().Build(
            atlas,
            sprites,
            sourceFingerprint,
            RequiredSpriteIds,
            new ReadOnlyPageSource(pageFiles));
        var catalogStable = string.Equals(
            firstCatalog.CatalogSha256,
            secondCatalog.CatalogSha256,
            StringComparison.Ordinal);
        if (!catalogStable)
        {
            throw new ConformanceException(ConformanceCodes.CatalogUnstable);
        }

        var decoderHashes = new List<string>();
        foreach (var page in atlas.Pages.OrderBy(page => page.Index))
        {
            var path = pageFiles[page.DataPath];
            string first;
            string second;
            using (var stream = OpenRead(path))
            {
                first = new AtlasDataDecoder().Decode(stream).ContentSha256;
            }
            using (var stream = OpenRead(path))
            {
                second = new AtlasDataDecoder().Decode(stream).ContentSha256;
            }
            if (!string.Equals(first, second, StringComparison.Ordinal))
            {
                throw new ConformanceException(ConformanceCodes.DecoderUnstable);
            }
            decoderHashes.Add(first);
        }

        var after = sourcePaths
            .Select(pair => Capture(pair.LogicalPath, pair.AbsolutePath))
            .OrderBy(snapshot => snapshot.LogicalPath, StringComparer.Ordinal)
            .ToArray();
        if (!before.SequenceEqual(after))
        {
            throw new ConformanceException(ConformanceCodes.SourceChanged);
        }

        var sourceSummaries = before
            .Select(snapshot => new ConformanceSourceSummary(
                snapshot.LogicalPath,
                snapshot.ByteLength,
                snapshot.Sha256))
            .ToArray();
        var animationCount = firstCatalog.Entities.Sum(entity => entity.Animations.Count);
        var resolvedFrameCount = firstCatalog.Entities.Sum(entity =>
            entity.Animations.Sum(animation => animation.Frames.Count));

        return new RealInstallConformanceReport(
            SchemaVersion: 1,
            TaskId: "CDR-016",
            ProfileId: CelesteInstallProfiles.WindowsFoundation.Id,
            Passed: true,
            ReadOnly: true,
            GameLaunched: false,
            GuiLaunched: false,
            InstallationWritesObserved: false,
            PersistedCommercialBytes: 0,
            SelectedRootPersisted: false,
            SourceFileCount: sourceSummaries.Length,
            SourceFingerprintSha256: ComputeAggregate(sourceSummaries.Select(summary =>
                $"{summary.LogicalPath}\0{summary.ByteLength}\0{summary.Sha256}")),
            Sources: Array.AsReadOnly(sourceSummaries),
            AtlasPageCount: atlas.Pages.Count,
            AtlasEntryCount: atlas.Pages.Sum(page => page.Entries.Count),
            SpriteDefinitionCount: firstCatalog.Entities.Count,
            AnimationCount: animationCount,
            ResolvedFrameCount: resolvedFrameCount,
            DecodedPageCount: firstCatalog.DecodedPageCount,
            DecoderFingerprintStable: true,
            DecoderFingerprintSha256: ComputeAggregate(decoderHashes),
            CatalogFingerprintStable: true,
            CatalogFingerprintSha256: firstCatalog.CatalogSha256);
    }

    private static SourceSnapshot Capture(string logicalPath, string absolutePath)
    {
        var info = new FileInfo(absolutePath);
        using var stream = OpenRead(absolutePath);
        return new SourceSnapshot(
            logicalPath,
            info.Length,
            info.LastWriteTimeUtc.Ticks,
            Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant());
    }

    private static string ResolveReadOnlyDirectory(string root, string relativePath)
    {
        var path = ResolveContained(root, relativePath);
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.Directory) == 0 ||
            (attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new ConformanceException(ConformanceCodes.SourceInvalid);
        }
        return path;
    }

    private static string ResolveReadOnlyFile(string root, string relativePath)
    {
        var path = ResolveContained(root, relativePath);
        ValidateRegularFile(path);
        return path;
    }

    private static string ResolvePageFile(string atlasDirectory, string logicalPath)
    {
        if (Path.IsPathRooted(logicalPath))
        {
            throw new ConformanceException(ConformanceCodes.SourceInvalid);
        }

        var candidates = new[]
        {
            ResolveContained(atlasDirectory, logicalPath),
            ResolveContained(atlasDirectory, logicalPath + ".data")
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(File.Exists)
        .ToArray();
        if (candidates.Length != 1)
        {
            throw new ConformanceException(ConformanceCodes.SourceInvalid);
        }

        ValidateRegularFile(candidates[0]);
        return candidates[0];
    }

    private static string ResolveContained(string root, string relativePath)
    {
        var canonicalRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var candidate = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath));
        if (!candidate.StartsWith(canonicalRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConformanceException(ConformanceCodes.SourceInvalid);
        }
        return candidate;
    }

    private static void ValidateRegularFile(string path)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.Directory) != 0 ||
            (attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new ConformanceException(ConformanceCodes.SourceInvalid);
        }
    }

    private static FileStream OpenRead(string path) => new(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 64 * 1024,
        FileOptions.SequentialScan);

    private static string ComputeAggregate(IEnumerable<string> values)
    {
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            foreach (var value in values)
            {
                writer.Write(value);
            }
        }
        return Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
    }

    private sealed record SourceSnapshot(
        string LogicalPath,
        long ByteLength,
        long LastWriteTimeUtcTicks,
        string Sha256);

    private sealed class ReadOnlyPageSource : IAtlasPageStreamSource
    {
        private readonly IReadOnlyDictionary<string, string> _pageFiles;

        public ReadOnlyPageSource(IReadOnlyDictionary<string, string> pageFiles) =>
            _pageFiles = pageFiles;

        public Stream? OpenPage(string logicalPath) =>
            _pageFiles.TryGetValue(logicalPath, out var path) ? OpenRead(path) : null;
    }
}
