using CelesteDesktop.Contracts.Install;
using CelesteDesktop.Install;
using CelesteDesktop.Install.FileSystem;
using CelesteDesktop.Install.Tests;

var tests = new (string Name, Action Body)[]
{
    ("complete synthetic install is accepted", CompleteInstallIsAccepted),
    ("blank root is rejected", BlankRootIsRejected),
    ("relative root is rejected", RelativeRootIsRejected),
    ("missing root is rejected", MissingRootIsRejected),
    ("file root is rejected", FileRootIsRejected),
    ("root reparse point is rejected", RootReparsePointIsRejected),
    ("missing required file is rejected", MissingRequiredFileIsRejected),
    ("parent reparse point is rejected", ParentReparsePointIsRejected),
    ("required file reparse point is rejected", RequiredFileReparsePointIsRejected),
    ("profile path escape is rejected", ProfilePathEscapeIsRejected),
    ("absolute profile path is rejected", AbsoluteProfilePathIsRejected),
    ("directory in required-file slot is rejected", WrongEntryTypeIsRejected),
    ("inaccessible required file is rejected", InaccessibleFileIsRejected),
    ("duplicate required path is rejected", DuplicateRequiredPathIsRejected),
    ("filesystem contract cannot enumerate or read content", FileSystemContractIsMetadataOnly)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void CompleteInstallIsAccepted()
{
    var (fileSystem, root) = CompleteFixture();
    var result = new InstallVerifier(fileSystem).Validate(
        root,
        CelesteInstallProfiles.WindowsFoundation);

    Assert(result.IsValid, "Expected the complete fixture to be valid.");
    Assert(result.CanonicalRoot == fileSystem.GetFullPath(root), "Canonical root mismatch.");
    Assert(result.Issues.Count == 0, "A valid result must not contain issues.");
    Assert(fileSystem.InspectionCount == 9, "Validation should inspect only the root and exact required path segments.");
}

static void BlankRootIsRejected()
{
    var result = new InstallVerifier(new SyntheticInstallFileSystem()).Validate(
        " ",
        CelesteInstallProfiles.WindowsFoundation);

    AssertSingleIssue(result, InstallValidationCodes.RootRequired);
}

static void RelativeRootIsRejected()
{
    var result = new InstallVerifier(new SyntheticInstallFileSystem()).Validate(
        "relative/celeste",
        CelesteInstallProfiles.WindowsFoundation);

    AssertSingleIssue(result, InstallValidationCodes.RootNotAbsolute);
}

static void MissingRootIsRejected()
{
    var result = new InstallVerifier(new SyntheticInstallFileSystem()).Validate(
        SyntheticRoot(),
        CelesteInstallProfiles.WindowsFoundation);

    AssertSingleIssue(result, InstallValidationCodes.RootMissing);
}

static void RootReparsePointIsRejected()
{
    var fileSystem = new SyntheticInstallFileSystem();
    var root = SyntheticRoot();
    fileSystem.Set(root, FileSystemEntryStatus.ReparsePoint);

    var result = new InstallVerifier(fileSystem).Validate(
        root,
        CelesteInstallProfiles.WindowsFoundation);

    AssertSingleIssue(result, InstallValidationCodes.ReparsePointRejected);
}

static void FileRootIsRejected()
{
    var fileSystem = new SyntheticInstallFileSystem();
    var root = SyntheticRoot();
    fileSystem.Set(root, FileSystemEntryStatus.File);

    var result = new InstallVerifier(fileSystem).Validate(
        root,
        CelesteInstallProfiles.WindowsFoundation);

    AssertSingleIssue(result, InstallValidationCodes.RootNotDirectory);
}

static void MissingRequiredFileIsRejected()
{
    var (fileSystem, root) = CompleteFixture();
    fileSystem.Remove(Under(root, "Content/Graphics/Sprites.xml"));

    var result = new InstallVerifier(fileSystem).Validate(
        root,
        CelesteInstallProfiles.WindowsFoundation);

    AssertContainsIssue(
        result,
        InstallValidationCodes.RequiredFileMissing,
        PlatformPath("Content/Graphics/Sprites.xml"));
}

static void ParentReparsePointIsRejected()
{
    var (fileSystem, root) = CompleteFixture();
    fileSystem.Set(
        Under(root, "Content/Graphics"),
        FileSystemEntryStatus.ReparsePoint);

    var result = new InstallVerifier(fileSystem).Validate(
        root,
        CelesteInstallProfiles.WindowsFoundation);

    AssertContainsIssue(
        result,
        InstallValidationCodes.ReparsePointRejected,
        PlatformPath("Content/Graphics/Atlases/Gameplay.meta"));
}

static void ProfilePathEscapeIsRejected()
{
    var fileSystem = new SyntheticInstallFileSystem();
    var root = SyntheticRoot();
    fileSystem.Set(root, FileSystemEntryStatus.Directory);
    var profile = new InstallValidationProfile(
        "path-escape",
        new[] { "../outside.bin" });

    var result = new InstallVerifier(fileSystem).Validate(root, profile);

    AssertSingleIssue(result, InstallValidationCodes.PathEscapeRejected);
    AssertIssuesDoNotContainRoot(result, root);
}

static void RequiredFileReparsePointIsRejected()
{
    var (fileSystem, root) = CompleteFixture();
    fileSystem.Set(
        Under(root, "Content/Graphics/Sprites.xml"),
        FileSystemEntryStatus.ReparsePoint);

    var result = new InstallVerifier(fileSystem).Validate(
        root,
        CelesteInstallProfiles.WindowsFoundation);

    AssertContainsIssue(
        result,
        InstallValidationCodes.ReparsePointRejected,
        PlatformPath("Content/Graphics/Sprites.xml"));
}

static void AbsoluteProfilePathIsRejected()
{
    var fileSystem = new SyntheticInstallFileSystem();
    var root = SyntheticRoot();
    fileSystem.Set(root, FileSystemEntryStatus.Directory);
    var profile = new InstallValidationProfile(
        "absolute-path",
        new[] { Path.Combine(Path.GetPathRoot(root)!, "outside.bin") });

    var result = new InstallVerifier(fileSystem).Validate(root, profile);

    AssertSingleIssue(result, InstallValidationCodes.PathEscapeRejected);
    AssertIssuesDoNotContainRoot(result, root);
}

static void WrongEntryTypeIsRejected()
{
    var (fileSystem, root) = CompleteFixture();
    fileSystem.Set(Under(root, "Celeste.exe"), FileSystemEntryStatus.Directory);

    var result = new InstallVerifier(fileSystem).Validate(
        root,
        CelesteInstallProfiles.WindowsFoundation);

    AssertContainsIssue(
        result,
        InstallValidationCodes.EntryTypeInvalid,
        "Celeste.exe");
}

static void InaccessibleFileIsRejected()
{
    var (fileSystem, root) = CompleteFixture();
    fileSystem.Set(
        Under(root, "Content/Graphics/Atlases/Gameplay.meta"),
        FileSystemEntryStatus.Inaccessible);

    var result = new InstallVerifier(fileSystem).Validate(
        root,
        CelesteInstallProfiles.WindowsFoundation);

    AssertContainsIssue(
        result,
        InstallValidationCodes.EntryInaccessible,
        PlatformPath("Content/Graphics/Atlases/Gameplay.meta"));
}

static void DuplicateRequiredPathIsRejected()
{
    var (fileSystem, root) = CompleteFixture();
    var profile = new InstallValidationProfile(
        "duplicate",
        new[] { "Celeste.exe", "celeste.exe" });

    var result = new InstallVerifier(fileSystem).Validate(root, profile);

    AssertContainsIssue(
        result,
        InstallValidationCodes.ProfileInvalid,
        "celeste.exe");
}

static void FileSystemContractIsMetadataOnly()
{
    var forbiddenTerms = new[] { "Enumerate", "ReadAll", "OpenRead", "GetFiles" };
    var methodNames = typeof(IInstallFileSystem)
        .GetMethods()
        .Select(method => method.Name)
        .ToArray();

    Assert(
        methodNames.All(name => forbiddenTerms.All(term =>
            !name.Contains(term, StringComparison.OrdinalIgnoreCase))),
        "The CDR-010 filesystem contract must not enumerate directories or read file content.");
}

static (SyntheticInstallFileSystem FileSystem, string Root) CompleteFixture()
{
    var fileSystem = new SyntheticInstallFileSystem();
    var root = SyntheticRoot();

    fileSystem.Set(root, FileSystemEntryStatus.Directory);
    fileSystem.Set(Under(root, "Celeste.exe"), FileSystemEntryStatus.File);
    fileSystem.Set(Under(root, "Content"), FileSystemEntryStatus.Directory);
    fileSystem.Set(Under(root, "Content/Graphics"), FileSystemEntryStatus.Directory);
    fileSystem.Set(Under(root, "Content/Graphics/Atlases"), FileSystemEntryStatus.Directory);
    fileSystem.Set(
        Under(root, "Content/Graphics/Atlases/Gameplay.meta"),
        FileSystemEntryStatus.File);
    fileSystem.Set(
        Under(root, "Content/Graphics/Sprites.xml"),
        FileSystemEntryStatus.File);

    return (fileSystem, root);
}

static string SyntheticRoot()
{
    var currentRoot = Path.GetPathRoot(Path.GetFullPath(Environment.CurrentDirectory));
    if (string.IsNullOrEmpty(currentRoot))
    {
        throw new InvalidOperationException("A fully qualified synthetic root is required.");
    }

    return Path.Combine(currentRoot, "cdr-synthetic", "celeste");
}

static string Under(string root, string relativePath) =>
    Path.Combine(root, PlatformPath(relativePath));

static string PlatformPath(string relativePath) => relativePath
    .Replace('/', Path.DirectorySeparatorChar)
    .Replace('\\', Path.DirectorySeparatorChar);

static void AssertSingleIssue(InstallValidationResult result, string code)
{
    Assert(!result.IsValid, "Expected validation to fail.");
    Assert(result.CanonicalRoot is null, "Invalid results must not publish a root.");
    Assert(result.Issues.Count == 1, $"Expected one issue, got {result.Issues.Count}.");
    Assert(result.Issues[0].Code == code, $"Expected {code}, got {result.Issues[0].Code}.");
}

static void AssertContainsIssue(
    InstallValidationResult result,
    string code,
    string relativePath)
{
    Assert(!result.IsValid, "Expected validation to fail.");
    Assert(result.CanonicalRoot is null, "Invalid results must not publish a root.");
    Assert(
        result.Issues.Any(issue =>
            issue.Code == code &&
            string.Equals(issue.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase)),
        $"Expected issue {code} for {relativePath}.");
}

static void AssertIssuesDoNotContainRoot(InstallValidationResult result, string root)
{
    Assert(
        result.Issues.All(issue =>
            issue.RelativePath is null ||
            !issue.RelativePath.Contains(root, StringComparison.OrdinalIgnoreCase)),
        "Validation issues must not disclose the absolute selected root.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
