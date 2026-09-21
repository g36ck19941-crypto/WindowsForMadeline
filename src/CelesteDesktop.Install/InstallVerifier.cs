using CelesteDesktop.Contracts.Install;
using CelesteDesktop.Install.FileSystem;

namespace CelesteDesktop.Install;

public sealed class InstallVerifier
{
    private static readonly char[] ExplicitlyInvalidSegmentCharacters =
    {
        '\0', ':', '*', '?', '"', '<', '>', '|'
    };

    private readonly IInstallFileSystem _fileSystem;

    public InstallVerifier(IInstallFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public InstallValidationResult Validate(
        string? selectedRoot,
        InstallValidationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(selectedRoot))
        {
            return Invalid(InstallValidationCodes.RootRequired, "select-root");
        }

        if (!Path.IsPathFullyQualified(selectedRoot))
        {
            return Invalid(InstallValidationCodes.RootNotAbsolute, "canonicalize-root");
        }

        string canonicalRoot;
        try
        {
            canonicalRoot = Path.TrimEndingDirectorySeparator(_fileSystem.GetFullPath(selectedRoot));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Invalid(InstallValidationCodes.RootInvalid, "canonicalize-root");
        }

        var rootStatus = _fileSystem.GetEntryStatus(canonicalRoot);
        var rootIssue = InspectRoot(rootStatus);
        if (rootIssue is not null)
        {
            return InstallValidationResult.Invalid(new[] { rootIssue });
        }

        var issues = new List<InstallValidationIssue>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var requiredFile in profile.RequiredFiles)
        {
            if (!TryNormalizeRelativePath(requiredFile, out var normalizedRelativePath))
            {
                issues.Add(new InstallValidationIssue(
                    InstallValidationCodes.PathEscapeRejected,
                    "validate-profile-path"));
                continue;
            }

            if (!seenPaths.Add(normalizedRelativePath))
            {
                issues.Add(new InstallValidationIssue(
                    InstallValidationCodes.ProfileInvalid,
                    "validate-profile",
                    normalizedRelativePath));
                continue;
            }

            InspectRequiredFile(canonicalRoot, normalizedRelativePath, issues);
        }

        return issues.Count == 0
            ? InstallValidationResult.Valid(canonicalRoot)
            : InstallValidationResult.Invalid(issues);
    }

    private static InstallValidationIssue? InspectRoot(FileSystemEntryStatus status) => status switch
    {
        FileSystemEntryStatus.Directory => null,
        FileSystemEntryStatus.Missing => new(
            InstallValidationCodes.RootMissing,
            "inspect-root"),
        FileSystemEntryStatus.ReparsePoint => new(
            InstallValidationCodes.ReparsePointRejected,
            "inspect-root"),
        FileSystemEntryStatus.Inaccessible => new(
            InstallValidationCodes.EntryInaccessible,
            "inspect-root"),
        _ => new(
            InstallValidationCodes.RootNotDirectory,
            "inspect-root")
    };

    private void InspectRequiredFile(
        string canonicalRoot,
        string normalizedRelativePath,
        ICollection<InstallValidationIssue> issues)
    {
        string candidate;
        try
        {
            candidate = _fileSystem.GetFullPath(
                Path.Combine(canonicalRoot, normalizedRelativePath));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            issues.Add(new InstallValidationIssue(
                InstallValidationCodes.PathEscapeRejected,
                "canonicalize-required-path"));
            return;
        }

        if (!IsContainedBy(canonicalRoot, candidate))
        {
            issues.Add(new InstallValidationIssue(
                InstallValidationCodes.PathEscapeRejected,
                "containment-check"));
            return;
        }

        var segments = normalizedRelativePath.Split(Path.DirectorySeparatorChar);
        var current = canonicalRoot;

        for (var index = 0; index < segments.Length; index++)
        {
            try
            {
                current = _fileSystem.GetFullPath(Path.Combine(current, segments[index]));
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                issues.Add(new InstallValidationIssue(
                    InstallValidationCodes.PathEscapeRejected,
                    "canonicalize-required-segment"));
                return;
            }

            var status = _fileSystem.GetEntryStatus(current);
            var isLeaf = index == segments.Length - 1;

            if (status == FileSystemEntryStatus.ReparsePoint)
            {
                issues.Add(new InstallValidationIssue(
                    InstallValidationCodes.ReparsePointRejected,
                    isLeaf ? "inspect-required-file" : "inspect-required-parent",
                    normalizedRelativePath));
                return;
            }

            if (status == FileSystemEntryStatus.Inaccessible)
            {
                issues.Add(new InstallValidationIssue(
                    InstallValidationCodes.EntryInaccessible,
                    isLeaf ? "inspect-required-file" : "inspect-required-parent",
                    normalizedRelativePath));
                return;
            }

            if (status == FileSystemEntryStatus.Missing)
            {
                issues.Add(new InstallValidationIssue(
                    InstallValidationCodes.RequiredFileMissing,
                    isLeaf ? "inspect-required-file" : "inspect-required-parent",
                    normalizedRelativePath));
                return;
            }

            var expected = isLeaf
                ? FileSystemEntryStatus.File
                : FileSystemEntryStatus.Directory;

            if (status != expected)
            {
                issues.Add(new InstallValidationIssue(
                    InstallValidationCodes.EntryTypeInvalid,
                    isLeaf ? "inspect-required-file" : "inspect-required-parent",
                    normalizedRelativePath));
                return;
            }
        }
    }

    private static bool TryNormalizeRelativePath(
        string? relativePath,
        out string normalizedRelativePath)
    {
        normalizedRelativePath = string.Empty;

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var platformPath = relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(platformPath))
        {
            return false;
        }

        var segments = platformPath.Split(Path.DirectorySeparatorChar);
        if (segments.Any(segment =>
                string.IsNullOrWhiteSpace(segment) ||
                segment is "." or ".." ||
                segment.IndexOfAny(ExplicitlyInvalidSegmentCharacters) >= 0))
        {
            return false;
        }

        normalizedRelativePath = string.Join(Path.DirectorySeparatorChar, segments);
        return true;
    }

    private static bool IsContainedBy(string canonicalRoot, string candidate)
    {
        var prefix = Path.EndsInDirectorySeparator(canonicalRoot)
            ? canonicalRoot
            : canonicalRoot + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static InstallValidationResult Invalid(string code, string stage) =>
        InstallValidationResult.Invalid(new[]
        {
            new InstallValidationIssue(code, stage)
        });
}
