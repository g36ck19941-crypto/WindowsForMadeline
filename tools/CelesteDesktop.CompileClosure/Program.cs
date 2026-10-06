using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

try
{
    if (args is ["--method-design", var methodSource, "--output", var methodOutput])
        return MethodDesign.Run(methodSource, methodOutput);
    if (args is ["--migration-inventory", var inventorySource, "--output", var inventoryOutput])
        return MigrationInventory.Run(inventorySource, inventoryOutput);
    if (args.SequenceEqual(new[] { "--self-test" })) return SyntheticChecks();
    if (args.Length != 6 || args[0] != "--source" || args[2] != "--references" || args[4] != "--output")
        throw new InvalidDataException("EXPLICIT_INPUTS_REQUIRED");
    var repo = Directory.GetCurrentDirectory();
    var cache = Path.Combine(repo, "local-cache");
    var source = Path.GetFullPath(args[1]);
    var output = Path.GetFullPath(args[5]);
    Contained(cache, source); Contained(cache, output);
    var referencesFile = Path.GetFullPath(args[3]);
    Contained(cache, referencesFile);
    var references = JsonSerializer.Deserialize<ReferenceInput[]>(File.ReadAllText(referencesFile))
        ?? throw new InvalidDataException("REFERENCES_EMPTY");
    if (references.Length != 8) throw new InvalidDataException("EIGHT_AUTHORIZED_REFERENCES_REQUIRED");
    var names = new HashSet<string>(StringComparer.Ordinal);
    foreach (var reference in references)
    {
        if (!names.Add(reference.Name) || reference.Name is not ("mscorlib" or "System" or "System.Core" or "System.Xml" or
            "Microsoft.Xna.Framework" or "Microsoft.Xna.Framework.Game" or "Microsoft.Xna.Framework.Graphics" or "Steamworks.NET"))
            throw new InvalidDataException("REFERENCE_NOT_AUTHORIZED");
        NoLinks(reference.Path);
        if (new FileInfo(reference.Path).Length > 32 * 1024 * 1024 || Hash(reference.Path) != reference.Sha256)
            throw new InvalidDataException("REFERENCE_IDENTITY_CHANGED");
        if (reference.Name == "Steamworks.NET") ValidateSteamworks(reference);
    }
    var trees = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);
    var pendingDirectories = new Stack<string>(); pendingDirectories.Push(source);
    long total = 0;
    while (pendingDirectories.TryPop(out var directory))
    {
        NoLinks(directory);
        foreach (var child in Directory.EnumerateDirectories(directory)) { NoLinks(child); pendingDirectories.Push(child); }
        foreach (var path in Directory.EnumerateFiles(directory, "*.cs"))
        {
            NoLinks(path);
            total += new FileInfo(path).Length;
            if (total > 64 * 1024 * 1024 || trees.Count >= 10000) throw new InvalidDataException("SOURCE_BUDGET");
            trees.Add(Path.GetRelativePath(source, path).Replace('\\', '/'), CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path));
        }
    }
    var seeds = new[] { "Celeste/Player.cs", "Celeste/Actor.cs", "Celeste/Solid.cs" };
    if (seeds.Any(seed => !trees.ContainsKey(seed))) throw new InvalidDataException("CORE_SEED_MISSING");
    var selected = Close(trees, seeds);
    var compilation = CSharpCompilation.Create("OriginalCoreCompileProbe", selected.Select(key => trees[key]),
        references.Select(reference => MetadataReference.CreateFromFile(reference.Path)),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release,
            platform: Platform.X86, deterministic: true));
    Directory.CreateDirectory(output);
    var assembly = Path.Combine(output, "OriginalCoreCompileProbe.dll");
    Microsoft.CodeAnalysis.Emit.EmitResult result;
    using (var stream = File.Create(assembly)) result = compilation.Emit(stream);
    foreach (var reference in references)
        if (Hash(reference.Path) != reference.Sha256) throw new InvalidDataException("REFERENCE_CHANGED_DURING_EMIT");
    if (AppDomain.CurrentDomain.GetAssemblies().Any(loaded => loaded.GetName().Name is "Steamworks.NET" or "Celeste" or "OriginalCoreCompileProbe"))
        throw new InvalidDataException("UNEXPECTED_RUNTIME_ASSEMBLY_LOAD");
    if (!result.Success) File.Delete(assembly); // Only this exact, tool-created failed output in the contained run directory.
    var counts = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
        .GroupBy(d => d.Id).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => g.Count());
    var privateDiagnostics = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => {
        var span = d.Location.GetLineSpan();
        return new { code = d.Id, file = Path.GetRelativePath(source, span.Path).Replace('\\', '/'),
            line = span.StartLinePosition.Line + 1, column = span.StartLinePosition.Character + 1,
            message = d.GetMessage(System.Globalization.CultureInfo.InvariantCulture) };
    }).ToArray();
    File.WriteAllText(Path.Combine(output, "diagnostic-locations-local-only.json"), JsonSerializer.Serialize(privateDiagnostics,
        new JsonSerializerOptions { WriteIndented = true }));
    var steamworksErrors = privateDiagnostics.Count(d => d.message.Contains("Steam", StringComparison.Ordinal));
    var outputTypes = new HashSet<string>(StringComparer.Ordinal);
    var outputReferenceCount = 0;
    var embeddedResourceCount = 0;
    string? outputHash = null;
    if (result.Success)
    {
        outputHash = Hash(assembly);
        using var stream = File.OpenRead(assembly); using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        outputReferenceCount = reader.AssemblyReferences.Count;
        embeddedResourceCount = reader.ManifestResources.Count;
        foreach (var handle in reader.TypeDefinitions)
        {
            var definition = reader.GetTypeDefinition(handle);
            outputTypes.Add(reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name));
        }
        if (!outputTypes.Contains("Celeste.Player") || !outputTypes.Contains("Celeste.Actor") || !outputTypes.Contains("Celeste.Solid"))
            throw new InvalidDataException("EMITTED_CORE_TYPES_MISSING");
    }
    // Identifier-only source/catalogue evidence stays ignored, never in public logs.
    File.WriteAllText(Path.Combine(output, "selected-files.json"), JsonSerializer.Serialize(selected.Order().ToArray()));
    var sourceDigest = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",
        selected.Order().Select(key => key + ":" + Hash(Path.Combine(source, key)))))));
    var report = new {
        taskId = "CDR-082", stage = "xna-net472-original-closure", availableSourceFiles = trees.Count,
        selectedSourceFiles = selected.Count, seedFiles = 3, seedIncludesPlayer = true,
        sourceClosureMethod = "conservative-identifier-closure-not-minimality-proof", selectedSourceDigest = sourceDigest,
        referenceCount = references.Length, steamworksMetadataOnly = true, steamworksApiCalled = false,
        steamworksOrOriginalAssemblyRuntimeLoaded = false,
        framework = "net472-references-experimental-from-net45", platform = "x86",
        sourceEdited = false, inventedStubs = false, generatedProjectExecuted = false, analyzersRun = false,
        emitSucceeded = result.Success, errorCounts = counts, steamworksDiagnosticCount = steamworksErrors,
        outputSha256 = outputHash, outputTypeCount = outputTypes.Count, outputReferenceCount, embeddedResourceCount,
        emittedCoreTypesPresent = result.Success, runtimeDependencyClosureEstablished = false,
        minimalIsolatedCoreEstablished = false, recoveredCodeExecuted = false,
        guiOpened = false, gameLaunched = false, installationWrites = 0, dependencyDownloads = 0,
        commercialMaterialLocalOnly = true, runtimeIntegrated = false
    };
    File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"ORIGINAL_CLOSURE_COMPILE selected={selected.Count} available={trees.Count} emitted={result.Success} sourceEdited=false codeExecuted=false");
    foreach (var (code, count) in counts) Console.WriteLine($"COMPILER_DIAGNOSTIC code={code} count={count}");
    return result.Success ? 0 : 2;
}
catch (Exception ex)
{
    // No commercial source excerpts, diagnostic text or private paths.
    var eventId = args.FirstOrDefault() switch { "--method-design" => "METHOD_DESIGN_FAILED", "--migration-inventory" => "MIGRATION_INVENTORY_FAILED", _ => "CLOSURE_PROBE_FAILED" };
    Console.Error.WriteLine($"{eventId} exceptionType={ex.GetType().Name} hresult={ex.HResult}");
    return 3;
}

static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
static void ValidateSteamworks(ReferenceInput reference)
{
    if (reference.Sha256 != "6C6B307E907294003014DA3ED4610E362A9B6EE4093A20E514B0E23454DA3085")
        throw new InvalidDataException("STEAMWORKS_BASELINE_CHANGED");
    using var stream = File.OpenRead(reference.Path); using var pe = new PEReader(stream);
    var reader = pe.GetMetadataReader(); var definition = reader.GetAssemblyDefinition();
    if (reader.GetString(definition.Name) != "Steamworks.NET" || definition.Version.ToString() != "10.0.0.0" ||
        !definition.PublicKey.IsNil || reader.GetString(definition.Culture) != "")
        throw new InvalidDataException("STEAMWORKS_IDENTITY_CHANGED");
}
static void Contained(string root, string path)
{
    if (!path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException("CACHE_ESCAPE");
    NoLinks(path);
}
static void NoLinks(string path)
{
    for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("REPARSE_POINT");
}
static HashSet<string> Close(IReadOnlyDictionary<string, SyntaxTree> trees, IEnumerable<string> seeds)
{
    var index = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    foreach (var (key, tree) in trees)
        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            var name = node switch { BaseTypeDeclarationSyntax declaration => declaration.Identifier.ValueText,
                DelegateDeclarationSyntax declaration => declaration.Identifier.ValueText, _ => null };
            if (name is null) continue;
            if (!index.TryGetValue(name, out var files)) index.Add(name, files = []);
            files.Add(key);
        }
    var selected = new HashSet<string>(seeds, StringComparer.OrdinalIgnoreCase);
    var pending = new Queue<string>(selected);
    while (pending.TryDequeue(out var key))
        foreach (var token in trees[key].GetRoot().DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)))
            if (index.TryGetValue(token.ValueText, out var files))
                foreach (var file in files) if (selected.Add(file)) pending.Enqueue(file);
    return selected;
}
static int SyntheticChecks()
{
    var passed = 0;
    void Check(bool value) { if (!value) throw new InvalidOperationException("SYNTHETIC_CHECK_FAILED"); passed++; }
    var trees = new Dictionary<string, SyntaxTree> {
        ["Player.cs"] = CSharpSyntaxTree.ParseText("class Player : Actor { Helper value; }"),
        ["Actor.cs"] = CSharpSyntaxTree.ParseText("class Actor { }"),
        ["Helper.cs"] = CSharpSyntaxTree.ParseText("class Helper { Player value; }"),
        ["Unused.cs"] = CSharpSyntaxTree.ParseText("class Unused { }") };
    var selected = Close(trees, new[] { "Player.cs" });
    Check(selected.Count == 3); Check(selected.Contains("Actor.cs")); Check(selected.Contains("Helper.cs"));
    Check(!selected.Contains("Unused.cs")); Check(selected.SetEquals(Close(trees, new[] { "Player.cs" })));
    var compilation = CSharpCompilation.Create("Generated", new[] { CSharpSyntaxTree.ParseText("public class Generated { }") },
        new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    using var buffer = new MemoryStream(); Check(compilation.Emit(buffer).Success); Check(buffer.Length > 0);
    var bad = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Generated { Missing value; }"));
    using var badBuffer = new MemoryStream(); var badResult = bad.Emit(badBuffer); Check(!badResult.Success);
    Check(badResult.Diagnostics.Any(d => d.Id == "CS0246"));
    var rejected = false; try { Contained(Path.GetFullPath("local-cache"), Path.GetFullPath("artifacts/out")); } catch (InvalidDataException) { rejected = true; }
    Check(rejected);
    rejected = false;
    try { ValidateSteamworks(new ReferenceInput("Steamworks.NET", "not-read", new string('0', 64))); }
    catch (InvalidDataException) { rejected = true; }
    Check(rejected);
    var trap = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class Generated { static Generated() { throw new System.Exception(); } }"));
    using var trapBuffer = new MemoryStream(); Check(trap.Emit(trapBuffer).Success);
    Check(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "Generated"));
    Console.WriteLine($"CLOSURE_SYNTHETIC_CHECKS passed={passed} failed=0 codeExecuted=false");
    return 0;
}
sealed record ReferenceInput(string Name, string Path, string Sha256);
