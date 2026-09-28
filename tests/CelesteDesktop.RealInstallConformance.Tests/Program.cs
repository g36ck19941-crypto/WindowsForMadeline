using System.Collections;
using System.Reflection;
using System.Text.Json;

var tests = new (string Name, Action Body)[]
{
    ("command line requires explicit absolute root and output", CommandLineRequiresExplicitAbsolutePaths),
    ("command line accepts either option order", CommandLineAcceptsEitherOrder),
    ("output inside selected installation is rejected", OutputInsideInstallIsRejected),
    ("sibling output with common prefix is accepted", SiblingPrefixIsAccepted),
    ("required sprite allowlist is exact and immutable", RequiredSpriteAllowlistIsExactAndImmutable),
    ("report contract contains no installation root", ReportContractContainsNoInstallRoot),
    ("report contract cannot carry raw commercial bytes", ReportContractCannotCarryRawBytes),
    ("serialized report contains summaries only", SerializedReportContainsSummariesOnly),
    ("stable conformance codes are unique", ConformanceCodesAreUnique),
    ("runner surface has no automatic discovery API", RunnerHasNoAutomaticDiscoveryApi)
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

static void CommandLineRequiresExplicitAbsolutePaths()
{
    ExpectCode(() => CommandLine.Parse([]), ConformanceCodes.ArgumentsInvalid);
    ExpectCode(() => CommandLine.Parse(["--root", ".", "--output", "report.json"]), ConformanceCodes.ArgumentsInvalid);
    ExpectCode(() => CommandLine.Parse(["--root", Path.GetFullPath("install"), "--other", Path.GetFullPath("report.json")]), ConformanceCodes.ArgumentsInvalid);
}

static void CommandLineAcceptsEitherOrder()
{
    var root = Path.GetFullPath("install");
    var output = Path.GetFullPath("report.json");
    var parsed = CommandLine.Parse(["--output", output, "--root", root]);
    Assert(parsed.InstallRoot == root, "Explicit root was not preserved.");
    Assert(parsed.OutputPath == output, "Explicit output was not preserved.");
}

static void OutputInsideInstallIsRejected()
{
    var root = Path.GetFullPath("selected-install");
    var output = Path.Combine(root, "report.json");
    ExpectCode(() => ConformancePathGuard.EnsureOutputOutsideInstall(root, output), ConformanceCodes.OutputInsideInstall);
}

static void SiblingPrefixIsAccepted()
{
    var parent = Path.GetFullPath("path-guard");
    var root = Path.Combine(parent, "Celeste");
    var output = Path.Combine(parent, "Celeste-evidence", "report.json");
    ConformancePathGuard.EnsureOutputOutsideInstall(root, output);
}

static void RequiredSpriteAllowlistIsExactAndImmutable()
{
    Assert(
        RealInstallConformanceRunner.RequiredSpriteIds.SequenceEqual(["player", "theo_crystal", "glider", "bumper", "pufferFish"]),
        "The real-install allowlist changed.");
    Assert(RealInstallConformanceRunner.RequiredSpriteIds is not string[], "The allowlist exposes a mutable array.");
    Assert(RealInstallConformanceRunner.RequiredSpriteIds is IList list && list.IsReadOnly, "The allowlist is not read-only.");
}

static void ReportContractContainsNoInstallRoot()
{
    var names = typeof(RealInstallConformanceReport)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(property => property.Name)
        .ToArray();
    Assert(!names.Any(name =>
            (name.Contains("Root", StringComparison.OrdinalIgnoreCase) && name != nameof(RealInstallConformanceReport.SelectedRootPersisted)) ||
            name.Contains("Absolute", StringComparison.OrdinalIgnoreCase)),
        "The report contract can persist an installation root.");
}

static void ReportContractCannotCarryRawBytes()
{
    var forbidden = typeof(RealInstallConformanceReport)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.PropertyType == typeof(byte[]) ||
                           typeof(Stream).IsAssignableFrom(property.PropertyType))
        .ToArray();
    Assert(forbidden.Length == 0, "The report contract can carry raw byte or stream payloads.");
}

static void SerializedReportContainsSummariesOnly()
{
    const string sentinelRoot = "X:\\PRIVATE\\CELESTE";
    var report = SampleReport();
    var json = JsonSerializer.Serialize(report);
    Assert(!json.Contains(sentinelRoot, StringComparison.OrdinalIgnoreCase), "A selected root leaked into JSON.");
    Assert(json.Contains("Content/Graphics/Atlases/Gameplay.meta", StringComparison.Ordinal), "Logical source summary was omitted.");
    Assert(!json.Contains("pixel", StringComparison.OrdinalIgnoreCase), "Raw pixel-shaped data appeared in JSON.");
}

static void ConformanceCodesAreUnique()
{
    var codes = typeof(ConformanceCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (string?)field.GetRawConstantValue())
        .Where(value => value is not null)
        .Cast<string>()
        .ToArray();
    Assert(codes.Length == codes.Distinct(StringComparer.Ordinal).Count(), "Conformance codes are not unique.");
    Assert(codes.All(code => code.StartsWith("CONFORMANCE_", StringComparison.Ordinal)), "A conformance code has an unstable prefix.");
}

static void RunnerHasNoAutomaticDiscoveryApi()
{
    var surface = typeof(RealInstallConformanceRunner)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        .Where(method => method.DeclaringType == typeof(RealInstallConformanceRunner))
        .ToArray();
    Assert(surface.Length == 2, "Unexpected public runner API was added.");
    Assert(surface.Count(method => method.Name == nameof(RealInstallConformanceRunner.Run)) == 1, "Explicit Run API is missing.");
    Assert(surface.Count(method => method.Name == "get_RequiredSpriteIds") == 1, "Allowlist getter is missing.");
}

static RealInstallConformanceReport SampleReport() => new(
    1,
    "CDR-016",
    "synthetic-profile",
    true,
    true,
    false,
    false,
    false,
    0,
    false,
    1,
    new string('a', 64),
    Array.AsReadOnly([new ConformanceSourceSummary("Content/Graphics/Atlases/Gameplay.meta", 12, new string('b', 64))]),
    1,
    1,
    1,
    1,
    1,
    1,
    true,
    new string('c', 64),
    true,
    new string('d', 64));

static void ExpectCode(Action action, string expected)
{
    try
    {
        action();
    }
    catch (ConformanceException exception)
    {
        Assert(exception.Code == expected, $"Expected {expected}, got {exception.Code}.");
        return;
    }
    throw new InvalidOperationException($"Expected {expected}.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
