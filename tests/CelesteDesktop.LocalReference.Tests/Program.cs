using System.Reflection;
using System.Text.Json;
using CelesteDesktop.LocalReference;

var tests = new (string Name, Action Body)[]
{
    ("explicit absolute paths are required", ExplicitAbsolutePathsAreRequired),
    ("cache root must be named local-cache", CacheRootMustBeLocalCache),
    ("cache and installation cannot overlap", CacheAndInstallCannotOverlap),
    ("Celeste dll is preferred over executable", DllIsPreferred),
    ("missing assembly is reported", MissingAssemblyIsReported),
    ("successful build writes only local reference cache", SuccessfulBuildIsLocalOnly),
    ("failed decompile leaves no staged source", FailedDecompileLeavesNoStage),
    ("manifest contains no path or source payload", ManifestHasNoSensitivePayload),
    ("existing immutable cache is reused", ExistingCacheIsReused),
    ("public builder has no discovery API", PublicSurfaceHasNoDiscovery)
};

var failures = 0;
foreach (var test in tests)
{
    try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL {test.Name}\n{exception}"); }
}
Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failures} failed={failures}");
return failures == 0 ? 0 : 1;

static void ExplicitAbsolutePathsAreRequired()
{
    ExpectCode(() => LocalReferenceCommandLine.Parse([]), LocalReferenceCodes.ArgumentsInvalid);
    ExpectCode(() => LocalReferenceCommandLine.Parse(["--root", ".", "--cache-root", "."]), LocalReferenceCodes.ArgumentsInvalid);
    var root = Path.GetFullPath("install");
    var cache = Path.GetFullPath("local-cache");
    var parsed = LocalReferenceCommandLine.Parse(["--cache-root", cache, "--root", root]);
    Assert(parsed.InstallRoot == root && parsed.CacheRoot == cache, "Explicit paths were changed.");
}

static void CacheRootMustBeLocalCache()
{
    ExpectCode(
        () => LocalReferencePathGuard.Validate(Path.GetFullPath("install"), Path.GetFullPath("other-cache")),
        LocalReferenceCodes.CacheRootInvalid);
}

static void CacheAndInstallCannotOverlap()
{
    var root = Path.Combine(Path.GetTempPath(), "cdr070-overlap", "install");
    ExpectCode(() => LocalReferencePathGuard.Validate(root, Path.Combine(root, "local-cache")), LocalReferenceCodes.CacheOverlapsInstall);
}

static void DllIsPreferred()
{
    using var fixture = Fixture.Create();
    File.WriteAllText(Path.Combine(fixture.Install, "Celeste.exe"), "exe");
    File.WriteAllText(Path.Combine(fixture.Install, "Celeste.dll"), "dll");
    Assert(Path.GetFileName(LocalReferenceBuilder.SelectAssembly(fixture.Install)) == "Celeste.dll", "DLL was not preferred.");
}

static void MissingAssemblyIsReported()
{
    using var fixture = Fixture.Create();
    ExpectCode(() => LocalReferenceBuilder.SelectAssembly(fixture.Install), LocalReferenceCodes.AssemblyMissing);
}

static void SuccessfulBuildIsLocalOnly()
{
    using var fixture = Fixture.Create();
    File.WriteAllText(Path.Combine(fixture.Install, "Celeste.dll"), "synthetic assembly bytes");
    var runner = new FakeRunner(0, writeSource: true);
    var report = new LocalReferenceBuilder(runner).Build(new(fixture.Install, fixture.Cache));
    Assert(report.SourceFileCount == 1, "Source count was not recorded.");
    Assert(!report.GameLaunched && report.InstallationWrites == 0 && !report.CommercialReferenceTracked, "Safety facts are incorrect.");
    Assert(runner.Request is not null && runner.Request.OutputDirectory.StartsWith(fixture.Cache, StringComparison.OrdinalIgnoreCase), "Output escaped the cache.");
    Assert(Directory.EnumerateFiles(fixture.Install).Count() == 1, "The installation was modified.");
}

static void FailedDecompileLeavesNoStage()
{
    using var fixture = Fixture.Create();
    File.WriteAllText(Path.Combine(fixture.Install, "Celeste.exe"), "synthetic assembly bytes");
    ExpectCode(() => new LocalReferenceBuilder(new FakeRunner(7, true)).Build(new(fixture.Install, fixture.Cache)), LocalReferenceCodes.DecompilerFailed);
    Assert(!Directory.EnumerateDirectories(fixture.Cache, "*.staging-*", SearchOption.AllDirectories).Any(), "A staging directory survived failure.");
}

static void ManifestHasNoSensitivePayload()
{
    var forbiddenNames = typeof(LocalReferenceReport).GetProperties()
        .Where(property => property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase) ||
                           property.Name.Contains("Root", StringComparison.OrdinalIgnoreCase) ||
                           property.PropertyType == typeof(byte[]) ||
                           typeof(Stream).IsAssignableFrom(property.PropertyType))
        .ToArray();
    Assert(forbiddenNames.Length == 0, "Manifest contract can carry a path or raw data.");

    var json = JsonSerializer.Serialize(new LocalReferenceReport(1, "CDR-070", "Celeste.dll", 1, new string('A', 64), "key", "ilspycmd", "1", 2, false, 0, false, false));
    Assert(!json.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase) && !json.Contains("class ", StringComparison.Ordinal), "Manifest contains sensitive payload.");
}

static void ExistingCacheIsReused()
{
    using var fixture = Fixture.Create();
    File.WriteAllText(Path.Combine(fixture.Install, "Celeste.dll"), "synthetic assembly bytes");
    var first = new FakeRunner(0, true);
    var builder = new LocalReferenceBuilder(first);
    _ = builder.Build(new(fixture.Install, fixture.Cache));
    var second = new FakeRunner(0, true);
    _ = new LocalReferenceBuilder(second).Build(new(fixture.Install, fixture.Cache));
    Assert(second.Request is null, "Immutable cache was needlessly regenerated.");
}

static void PublicSurfaceHasNoDiscovery()
{
    var names = typeof(LocalReferenceBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        .Where(method => method.DeclaringType == typeof(LocalReferenceBuilder))
        .Select(method => method.Name)
        .ToArray();
    Assert(names.SequenceEqual(["Build", "SelectAssembly"]), "Unexpected public discovery API exists.");
}

static void ExpectCode(Action action, string expected)
{
    try { action(); }
    catch (LocalReferenceException exception) { Assert(exception.Code == expected, $"Expected {expected}, got {exception.Code}."); return; }
    throw new InvalidOperationException($"Expected {expected}.");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class FakeRunner(int exitCode, bool writeSource) : ILocalDecompilerRunner
{
    public DecompilerRequest? Request { get; private set; }
    public DecompilerResult Run(DecompilerRequest request)
    {
        Request = request;
        if (writeSource) File.WriteAllText(Path.Combine(request.OutputDirectory, "Reference.cs"), "// synthetic test output");
        return new(exitCode);
    }
}

sealed class Fixture : IDisposable
{
    private Fixture(string root)
    {
        Root = root;
        Install = Path.Combine(root, "install");
        Cache = Path.Combine(root, "local-cache");
        Directory.CreateDirectory(Install);
        Directory.CreateDirectory(Cache);
    }
    public string Root { get; }
    public string Install { get; }
    public string Cache { get; }
    public static Fixture Create() => new(Path.Combine(Path.GetTempPath(), "cdr070-tests", Guid.NewGuid().ToString("N")));
    public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true); }
}
