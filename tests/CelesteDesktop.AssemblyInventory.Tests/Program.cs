using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using CelesteDesktop.AssemblyInventory;

var tests = new (string, Action)[]
{
    ("requires explicit argument paths", () => Expect("EXPLICIT_PATHS_REQUIRED", () => InventoryOptions.Parse(["--root", ".", "--output", "."]))),
    ("rejects extra arguments", () => Expect("EXPLICIT_PATHS_REQUIRED", () => InventoryOptions.Parse(["--root", "a", "--output", "b", "--extra"]))),
    ("rejects swapped arguments", () => Expect("EXPLICIT_PATHS_REQUIRED", () => InventoryOptions.Parse(["--output", "a", "--root", "b"]))),
    ("rejects output within install", () => With(f => Expect("OUTPUT_IN_INSTALLATION", () => OutputGuard.Validate(f.Root, Path.Combine(f.Root, "result.json"))))),
    ("rejects output equal to install", () => With(f => Expect("OUTPUT_IN_INSTALLATION", () => OutputGuard.Validate(f.Root, f.Root)))),
    ("allows sibling output with common prefix", () => With(f => OutputGuard.Validate(f.Root, f.Root + "-report/result.json"))),
    ("accepts explicit options", () => With(f => Check(InventoryOptions.Parse(["--root", f.Root, "--output", Path.Combine(f.Parent, "report.json")]).Root == f.Root))),
    ("missing root is explicit failure", () => Expect("INSTALLATION_MISSING", () => new InventoryRunner().Inspect(Path.GetFullPath("no-such-cdr081-install")))),
    ("missing managed candidates fails", () => With(f => Expect("MANAGED_GAME_CANDIDATE_MISSING", () => new InventoryRunner().Inspect(f.Root)))),
    ("invalid image is contained", () => With(f => { File.WriteAllBytes(Path.Combine(f.Root, "Celeste.exe"), [0, 1, 2]); Expect("MANAGED_GAME_CANDIDATE_MISSING", () => new InventoryRunner().Inspect(f.Root)); })),
    ("oversize candidate is bounded", () => With(f => { using (var s = File.Create(Path.Combine(f.Root, "Celeste.dll"))) s.SetLength(InventoryRunner.MaximumFileBytes + 1); Expect("FILE_SIZE_LIMIT", () => new InventoryRunner().Inspect(f.Root)); })),
    ("assembly reference count is bounded", () => With(f => { f.Write("Celeste.dll", "Celeste", refs: Enumerable.Range(0, 257).Select(i => Id("Ref" + i)).ToArray()); Expect("METADATA_ROW_LIMIT", () => new InventoryRunner().Inspect(f.Root)); })),
    ("managed identity and hash recovered without load", ManagedIdentity),
    ("native apphost is not mistaken for managed game", NativeHost),
    ("original backup considered independently", Backup),
    ("mod namespace is distinguished", ModNamespace),
    ("mod reference is distinguished", ModReference),
    ("target framework attribute read as metadata", TargetFramework),
    ("required types are qualified not filename guesses", QualifiedTypes),
    ("exact dependency identity found", ExactDependency),
    ("dependency version mismatch not accepted", VersionMismatch),
    ("dependency culture mismatch not accepted", () => Check(!InventoryRunner.IdentityMatches(Id("FNA"), Id("FNA") with { Culture = "fr" }))),
    ("dependency public key mismatch not accepted", () => Check(!InventoryRunner.IdentityMatches(Id("FNA"), Id("FNA") with { PublicKeyToken = "abcdef" }))),
    ("framework refs not called complete", FrameworkReference),
    ("missing external dependency is explicit", MissingDependency),
    ("dependency cycles terminate", Cycles),
    ("unsafe reference cannot escape install", UnsafeReference),
    ("source and saves directories not traversed", NoTraversal),
    ("reports never include installation path or raw bytes", NoPayload),
    ("diagnostics preserve details while removing private paths", Redaction),
    ("success does not claim pristine compilation or closure", NoOverclaim),
    ("read inspection leaves synthetic installation identical", ReadOnly),
    ("fixed candidate slots immutable", () => Check(InventoryRunner.CandidateSlots.Count == 4 && ((IList<string>)InventoryRunner.CandidateSlots).IsReadOnly)),
    ("assembly names reject traversal separators and devices", SafeNames),
    ("fresh runner invocation resets inventory", Reset)
};
var failures = 0;
foreach (var (name, run) in tests)
{
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failures++; Console.Error.WriteLine($"FAIL {name} {ex.GetType().Name}: {ex.Message}"); }
}
Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failures} failed={failures}");
return failures == 0 ? 0 : 1;

static AssemblyIdentity Id(string name, string version = "1.0.0.0") => new(name, version, "", "");
static void Check(bool ok) { if (!ok) throw new InvalidOperationException("Synthetic assertion failed."); }
static void Expect(string code, Action body)
{
    try { body(); } catch (InventoryException ex) when (ex.Code == code) { return; }
    throw new InvalidOperationException($"Expected {code}.");
}
static void With(Action<Fixture> body) { using var f = new Fixture(); body(f); }
static AssemblyFact MainFact(InventoryReport report) => report.Candidates.Single(c => c.Slot == "Celeste.dll");
static void ManagedIdentity() => With(f =>
{
    f.Write("Celeste.dll", "Celeste");
    var before = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).ToHashSet();
    var fact = MainFact(new InventoryRunner().Inspect(f.Root));
    Check(fact.Status == "managed-assembly" && fact.Identity == Id("Celeste"));
    Check(fact.Sha256 == Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(f.Root, "Celeste.dll")))));
    Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => !before.Contains(a.GetName().Name) && a.GetName().Name == "Celeste"));
});
static void Backup() => With(f =>
{
    f.Write("Celeste.dll", "Celeste", mod: true); f.Write("orig/Celeste.exe", "Celeste");
    var report = new InventoryRunner().Inspect(f.Root);
    Check(MainFact(report).Classification == "mod-bearing");
    Check(report.Candidates.Single(c => c.Slot == "orig/Celeste.exe").Classification == "unmodified-candidate-not-authenticated");
});
static void NativeHost() => With(f =>
{
    f.Write("Celeste.dll", "Celeste");
    var path = Path.Combine(f.Root, "Celeste.exe");
    f.Write("Celeste.exe", "SyntheticNative");
    var bytes = File.ReadAllBytes(path);
    var peOffset = BitConverter.ToInt32(bytes, 0x3c);
    var optional = peOffset + 24;
    var cliDirectory = optional + (BitConverter.ToUInt16(bytes, optional) == 0x20b ? 112 : 96) + 14 * 8;
    Array.Clear(bytes, cliDirectory, 8);
    File.WriteAllBytes(path, bytes);
    Check(new InventoryRunner().Inspect(f.Root).Candidates.Single(c => c.Slot == "Celeste.exe").Status == "native-pe");
});
static void TargetFramework() => With(f =>
{
    f.Write("Celeste.dll", "Celeste", framework: ".NETFramework,Version=v4.5");
    Check(MainFact(new InventoryRunner().Inspect(f.Root)).TargetFramework == ".NETFramework,Version=v4.5");
});
static void ModNamespace() => With(f => { f.Write("Celeste.dll", "Celeste", mod: true); Check(MainFact(new InventoryRunner().Inspect(f.Root)).ModTypeCount == 1); });
static void ModReference() => With(f => { f.Write("Celeste.dll", "Celeste", refs: [Id("MonoMod.Utils")]); Check(MainFact(new InventoryRunner().Inspect(f.Root)).ModReferenceObserved); });
static void QualifiedTypes() => With(f =>
{
    f.Write("Celeste.dll", "Celeste");
    var facts = MainFact(new InventoryRunner().Inspect(f.Root)).RequiredTypes;
    Check(facts.Single(t => t.Name == "Celeste.Player").Present && !facts.Single(t => t.Name == "Monocle.Actor").Present);
});
static void ExactDependency() => With(f =>
{
    f.Write("Celeste.dll", "Celeste", refs: [Id("FNA")]); f.Write("FNA.dll", "FNA");
    var reference = new InventoryRunner().Inspect(f.Root).Dependencies.Single(d => d.Slot == "Celeste.dll").References.Single();
    Check(reference.Status == "exact-metadata-match" && reference.Matches.Single() == "FNA.dll");
});
static void VersionMismatch() => With(f =>
{
    f.Write("Celeste.dll", "Celeste", refs: [Id("FNA", "2.0.0.0")]); f.Write("FNA.dll", "FNA");
    Check(new InventoryRunner().Inspect(f.Root).Dependencies.First().References.Single().Status == "identity-mismatch");
});
static void FrameworkReference() => With(f =>
{
    f.Write("Celeste.dll", "Celeste", refs: [Id("mscorlib")]);
    Check(new InventoryRunner().Inspect(f.Root).Dependencies.First().References.Single().Status == "framework-not-inspected");
});
static void MissingDependency() => With(f =>
{
    f.Write("Celeste.dll", "Celeste", refs: [Id("RequiredDependency")]);
    Check(new InventoryRunner().Inspect(f.Root).Dependencies.First().References.Single().Status == "missing-or-unreadable");
});
static void Cycles() => With(f =>
{
    f.Write("Celeste.dll", "Celeste", refs: [Id("FNA")]); f.Write("FNA.dll", "FNA", refs: [Id("Celeste")]);
    Check(new InventoryRunner().Inspect(f.Root).Dependencies.Count == 2);
});
static void UnsafeReference() => With(f =>
{
    f.Write("Celeste.dll", "Celeste", refs: [Id("..Evil")]);
    Expect("UNSAFE_ASSEMBLY_NAME", () => new InventoryRunner().Inspect(f.Root));
});
static void NoTraversal() => With(f =>
{
    f.Write("Celeste.dll", "Celeste");
    Directory.CreateDirectory(Path.Combine(f.Root, "Saves"));
    File.WriteAllBytes(Path.Combine(f.Root, "Saves", "Celeste.dll"), [1, 2, 3]);
    Check(new InventoryRunner().Inspect(f.Root).Dependencies.Count == 1);
});
static void NoPayload() => With(f =>
{
    f.Write("Celeste.dll", "Celeste");
    var json = JsonSerializer.Serialize(new InventoryRunner().Inspect(f.Root), CelesteDesktop.AssemblyInventory.Program.JsonOptions);
    Check(!json.Contains(f.Root) && !json.Contains("byte[]") && !json.Contains("ilBody") && !json.Contains("sourceCode"));
});
static void NoOverclaim() => With(f =>
{
    f.Write("Celeste.dll", "Celeste");
    var report = new InventoryRunner().Inspect(f.Root);
    Check(!report.PristineVanillaEstablished && !report.CompilationEstablished && !report.DependencyClosureEstablished &&
        !report.AssemblyExecuted && !report.GameLaunched && report.InstallationWrites == 0 && report.PersistedCommercialBytes == 0);
});
static void Redaction() => With(f =>
{
    var inner = new IOException("Failed at " + Path.Combine(f.Root, "Celeste.dll"));
    var ex = new InventoryException("read", "READ_FAILED", inner);
    var detail = Diagnostics.Describe(ex, [f.Root]);
    var json = JsonSerializer.Serialize(detail);
    Check(detail.Inner is not null && detail.Inner.Type == typeof(IOException).FullName && detail.Inner.HResult == inner.HResult &&
        !json.Contains(f.Root) && detail.Inner.Message.Contains("<selected-path>"));
});
static void ReadOnly() => With(f =>
{
    f.Write("Celeste.dll", "Celeste");
    var path = Path.Combine(f.Root, "Celeste.dll"); var before = SHA256.HashData(File.ReadAllBytes(path));
    new InventoryRunner().Inspect(f.Root);
    Check(before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(path))) && Directory.GetFiles(f.Root).Length == 1);
});
static void SafeNames()
{
    foreach (var name in new[] { "../evil", "a/b", "a\\b", "a:b", "..Evil", "CON", "LPT1", "" }) Check(!InventoryRunner.IsSafeAssemblyName(name));
    Check(InventoryRunner.IsSafeAssemblyName("Steamworks.NET"));
}
static void Reset() => With(f =>
{
    f.Write("Celeste.dll", "Celeste");
    var runner = new InventoryRunner(); Check(MainFact(runner.Inspect(f.Root)).ModTypeCount == 0);
    f.Write("Celeste.dll", "Celeste", mod: true); Check(MainFact(runner.Inspect(f.Root)).ModTypeCount == 1);
});

sealed class Fixture : IDisposable
{
    public string Parent { get; } = Path.Combine(Path.GetTempPath(), "cdr081-synthetic", Guid.NewGuid().ToString("N"));
    public string Root => Path.Combine(Parent, "install");
    public Fixture() => Directory.CreateDirectory(Root);
    public void Write(string slot, string assemblyName, bool mod = false, AssemblyIdentity[]? refs = null, string? framework = null)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString(assemblyName + ".dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        var assemblyHandle = metadata.AddAssembly(metadata.GetOrAddString(assemblyName), new Version(1, 0, 0, 0), default, default, 0, AssemblyHashAlgorithm.Sha256);
        if (framework is not null)
        {
            var scope = metadata.AddAssemblyReference(metadata.GetOrAddString("mscorlib"), new Version(4, 0, 0, 0), default, default, 0, default);
            var type = metadata.AddTypeReference(scope, metadata.GetOrAddString("System.Runtime.Versioning"), metadata.GetOrAddString("TargetFrameworkAttribute"));
            var signature = new BlobBuilder(); signature.WriteBytes(new byte[] { 0x20, 1, 1, 0x0e });
            var constructor = metadata.AddMemberReference(type, metadata.GetOrAddString(".ctor"), metadata.GetOrAddBlob(signature));
            var value = new BlobBuilder(); value.WriteUInt16(1); value.WriteSerializedString(framework); value.WriteUInt16(0);
            metadata.AddCustomAttribute(assemblyHandle, constructor, metadata.GetOrAddBlob(value));
        }
        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default, default, default);
        metadata.AddTypeDefinition(TypeAttributes.Public, metadata.GetOrAddString("Celeste"), metadata.GetOrAddString("Player"), default, default, default);
        if (mod) metadata.AddTypeDefinition(TypeAttributes.Public, metadata.GetOrAddString("Celeste.Mod"), metadata.GetOrAddString("Everest"), default, default, default);
        foreach (var reference in refs ?? [])
            metadata.AddAssemblyReference(metadata.GetOrAddString(reference.Name), Version.Parse(reference.Version),
                metadata.GetOrAddString(reference.Culture), default, 0, default);
        var builder = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
            new MetadataRootBuilder(metadata), new BlobBuilder());
        var blob = new BlobBuilder(); builder.Serialize(blob);
        var path = Path.Combine(Root, slot.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, blob.ToArray());
    }
    public void Dispose() => Directory.Delete(Parent, true);
}
