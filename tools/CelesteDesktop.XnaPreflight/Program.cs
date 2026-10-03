using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

var runId = Guid.NewGuid().ToString();

try
{
    if (args is ["--self-test"])
    {
        var own = Inspect(typeof(Program).Assembly.Location);
        if (!own.ilOnly || own.pinvokeMethods != 0 || own.nativeMethodCount != 0) throw new InvalidDataException("OWN_METADATA_FAILED");
        var rejected = false; try { Scope("artifacts/outside"); } catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new InvalidDataException("SCOPE_NEGATIVE_FAILED");
        if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name?.StartsWith("Microsoft.Xna.Framework", StringComparison.Ordinal) == true))
            throw new InvalidDataException("XNA_UNEXPECTED_LOAD");
        using (var stream = File.OpenRead(typeof(Program).Assembly.Location))
        using (var pe = new PEReader(stream))
        {
            var reader = pe.GetMetadataReader();
            var trap = reader.TypeDefinitions.First(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "AuditTrap");
            var constructor = reader.GetTypeDefinition(trap).GetMethods().First(h => reader.GetString(reader.GetMethodDefinition(h).Name) == ".cctor");
            var analysis = InitializationAnalyzer.Analyze(pe, reader, new[] { constructor });
            if (analysis.visitedManagedMethods != 1 || analysis.memberReferenceBoundaries < 1 || analysis.runtimeSafetyEstablished)
                throw new InvalidDataException("INITIALIZER_TRAP_AUDIT_FAILED");
        }
        Console.WriteLine("XNA_PREFLIGHT_TESTS passed=4 failed=0 targetExecuted=false");
        return 0;
    }
    if (args is not ["--cached-root", var root]) throw new ArgumentException("XNA_PREFLIGHT_ARGS");
    Scope(root);
    var hashes = new Dictionary<string, string> {
        ["Microsoft.Xna.Framework"] = "38E7093F52D7474BBC6256906519781A1210D7DA50A1C667B52716FCF49CA130",
        ["Microsoft.Xna.Framework.Game"] = "B5DFFDD8125ABEF2A4507BA4E1D2F11062143F0A63D48FE4F298B95AD746A1F0",
        ["Microsoft.Xna.Framework.Graphics"] = "560080FC39021C611CA9D076DCEBED312FAF6D7D1413C2DC523683EA635E9F55" };
    var rows = new List<object>();
    foreach (var pair in hashes)
    {
        var file = Path.Combine(root, pair.Key + ".dll"); NoLinks(file);
        if (new FileInfo(file).Length > 16 * 1024 * 1024) throw new InvalidDataException("XNA_PREFLIGHT_BUDGET");
        using (var input = File.OpenRead(file))
            if (Convert.ToHexString(SHA256.HashData(input)) != pair.Value) throw new InvalidDataException("XNA_BASELINE_CHANGED");
        var row = Inspect(file);
        if (row.name != pair.Key) throw new InvalidDataException("XNA_IDENTITY_CHANGED");
        rows.Add(row);
    }
    Console.WriteLine(JsonSerializer.Serialize(new { taskId = "CDR-082", stage = "cached-xna-static-initialization-preflight", rows,
        assemblyExecuted = false, runtimeSafetyEstablished = false, originalTestExecuted = false,
        gameLaunched = false, guiOpened = false, newDependenciesRead = false, installationWrites = 0 }));
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new { timestampUtc = DateTime.UtcNow, runId,
        eventId = "XNA_PREFLIGHT_FAILED", subsystem = "XnaPreflight", stage = "metadata-only",
        severity = "Error", outcome = "failed", durationMs = 0, durationMeasured = false,
        exception = Describe(exception, 0), recoverable = false }));
    return 1;
}

static object? Describe(Exception exception, int depth)
{
    if (depth >= 4) return new { truncated = true };
    string Redact(string? text) => Regex.Replace(text ?? "", @"[A-Za-z]:[\\/][^\r\n""']+", "[local-path]");
    return new { exceptionType = exception.GetType().Name, message = Redact(exception.Message),
        hresult = exception.HResult, stack = Redact(exception.StackTrace),
        inner = exception.InnerException is null ? null : Describe(exception.InnerException, depth + 1) };
}

static void NoLinks(string path)
{
    for (var cursor = Path.GetFullPath(path); cursor is not null; cursor = Path.GetDirectoryName(cursor))
        if ((File.Exists(cursor) || Directory.Exists(cursor)) && (File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("XNA_PREFLIGHT_LINK");
}
static void Scope(string root)
{
    var prefix = Path.GetFullPath("local-cache/cdr-082-xna") + Path.DirectorySeparatorChar;
    var path = Path.GetFullPath(root);
    if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(path) != "runtime")
        throw new ArgumentException("XNA_PREFLIGHT_SCOPE");
    NoLinks(path);
}
static MetadataFact Inspect(string path)
{
    using var stream = File.OpenRead(path);
    if (stream.Length > 16 * 1024 * 1024) throw new InvalidDataException("XNA_PREFLIGHT_BUDGET");
    using var pe = new PEReader(stream);
    var metadata = pe.GetMetadataReader();
    if (metadata.MethodDefinitions.Count > 50000 || metadata.TypeDefinitions.Count > 10000 || metadata.AssemblyReferences.Count > 256)
        throw new InvalidDataException("XNA_PREFLIGHT_ROWS");
    var globalInitializer = false;
    var roots = new List<MethodDefinitionHandle>();
    foreach (var handle in metadata.TypeDefinitions)
    {
        var type = metadata.GetTypeDefinition(handle);
        if (metadata.GetString(type.Name) == "<Module>")
            roots.AddRange(type.GetMethods().Where(m => metadata.GetString(metadata.GetMethodDefinition(m).Name) == ".cctor"));
    }
    globalInitializer = roots.Count != 0;
    var methods = metadata.MethodDefinitions.Select(metadata.GetMethodDefinition).ToArray();
    return new MetadataFact(metadata.GetString(metadata.GetAssemblyDefinition().Name),
        (pe.PEHeaders.CorHeader!.Flags & CorFlags.ILOnly) != 0, globalInitializer,
        methods.Count(m => (m.Attributes & MethodAttributes.PinvokeImpl) != 0),
        methods.Count(m => (m.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native),
        metadata.AssemblyReferences.Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name)).ToArray(),
        InitializationAnalyzer.Analyze(pe, metadata, roots));
}
internal sealed record MetadataFact(string name, bool ilOnly, bool moduleInitializerPresent,
    int pinvokeMethods, int nativeMethodCount, string[] referencedAssemblyNames, InitializationFact initialization);
