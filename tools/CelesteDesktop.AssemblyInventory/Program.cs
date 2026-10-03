using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CelesteDesktop.AssemblyInventory;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 6 && args[0] == "--reference-root" && args[2] == "--name" && args[4] == "--output")
            {
                if (!Path.IsPathFullyQualified(args[1]) || !Path.IsPathFullyQualified(args[5]))
                    throw new InventoryException("arguments", "EXPLICIT_PATHS_REQUIRED");
                OutputGuard.Validate(args[1], args[5]);
                var fact = new InventoryRunner().InspectReference(args[1], args[3]);
                Directory.CreateDirectory(Path.GetDirectoryName(args[5])!);
                File.WriteAllText(args[5], JsonSerializer.Serialize(fact, JsonOptions));
                Console.WriteLine($"REFERENCE_METADATA_COMPLETED name={fact.Slot} status={fact.Status} assemblyExecuted=false");
                return fact.Status == "managed-assembly" ? 0 : 2;
            }
            var options = InventoryOptions.Parse(args);
            var report = new InventoryRunner().Inspect(options.Root);
            OutputGuard.Validate(options.Root, options.Output);
            Directory.CreateDirectory(Path.GetDirectoryName(options.Output)!);
            File.WriteAllText(options.Output, JsonSerializer.Serialize(report, JsonOptions));
            Console.WriteLine($"ASSEMBLY_INVENTORY_COMPLETED candidates={report.Candidates.Count} nodes={report.Dependencies.Count} assemblyExecuted=false");
            return 0;
        }
        catch (InventoryException ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new
            {
                eventId = "ASSEMBLY_INVENTORY_FAILED",
                phase = ex.Phase,
                code = ex.Code,
                exception = Diagnostics.Describe(ex, args)
            }, JsonOptions));
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new
            {
                eventId = "ASSEMBLY_INVENTORY_FAILED",
                phase = "unexpected",
                code = "UNEXPECTED",
                exception = Diagnostics.Describe(ex, args)
            }, JsonOptions));
            return 3;
        }
    }
    public static JsonSerializerOptions JsonOptions { get; } = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}

public sealed record InventoryOptions(string Root, string Output)
{
    public static InventoryOptions Parse(IReadOnlyList<string> args)
    {
        if (args.Count != 4 || args[0] != "--root" || args[2] != "--output" ||
            !Path.IsPathFullyQualified(args[1]) || !Path.IsPathFullyQualified(args[3]))
            throw new InventoryException("arguments", "EXPLICIT_PATHS_REQUIRED");
        var root = Path.GetFullPath(args[1]);
        var output = Path.GetFullPath(args[3]);
        OutputGuard.Validate(root, output);
        return new(root, output);
    }
}

public sealed class InventoryException(string phase, string code, Exception? inner = null) : Exception(code, inner)
{
    public string Phase { get; } = phase;
    public string Code { get; } = code;
}

public static class OutputGuard
{
    public static void Validate(string root, string output)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedOutput = Path.GetFullPath(output);
        if (normalizedOutput.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedOutput, normalizedRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InventoryException("output", "OUTPUT_IN_INSTALLATION");
        for (var current = normalizedOutput; current is not null; current = Path.GetDirectoryName(current))
            if ((Directory.Exists(current) || File.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InventoryException("output", "REPARSE_POINT");
    }
}

public sealed record AssemblyIdentity(string Name, string Version, string Culture, string PublicKeyToken);
public sealed record ExceptionFact(string Type, string Message, int HResult, string? Stack, ExceptionFact? Inner);
public static class Diagnostics
{
    public static ExceptionFact Describe(Exception exception, IReadOnlyList<string> privatePaths, int depth = 0)
    {
        string Redact(string value)
        {
            foreach (var path in privatePaths.Where(Path.IsPathFullyQualified))
            {
                value = value.Replace(path, "<selected-path>", StringComparison.OrdinalIgnoreCase);
                value = value.Replace(path.Replace('\\', '/'), "<selected-path>", StringComparison.OrdinalIgnoreCase);
            }
            return value;
        }
        return new(exception.GetType().FullName ?? exception.GetType().Name, Redact(exception.Message),
            exception.HResult, exception.StackTrace is null ? null : Redact(exception.StackTrace),
            depth < 4 && exception.InnerException is not null ? Describe(exception.InnerException, privatePaths, depth + 1) : null);
    }
}
public sealed record TypeFact(string Name, bool Present);
public sealed record ReferenceFact(AssemblyIdentity Identity, string Status, IReadOnlyList<string> Matches);
public sealed record AssemblyFact(
    string Slot, string Status, long Length, string? Sha256, AssemblyIdentity? Identity,
    string? MetadataVersion, string? TargetFramework, string? Machine, int TypeDefinitionCount, int ModTypeCount,
    bool ModReferenceObserved, string Classification, IReadOnlyList<TypeFact> RequiredTypes,
    IReadOnlyList<AssemblyIdentity> References, IReadOnlyList<string> NativeModuleNames,
    string? FailureType, ExceptionFact? Failure = null);
public sealed record DependencyFact(string Slot, AssemblyFact Assembly, IReadOnlyList<ReferenceFact> References);
public sealed record InventoryReport(
    int SchemaVersion, string TaskId, IReadOnlyList<AssemblyFact> Candidates, IReadOnlyList<DependencyFact> Dependencies,
    bool AssemblyExecuted = false, bool GameLaunched = false, int InstallationWrites = 0,
    int PersistedCommercialBytes = 0, bool PristineVanillaEstablished = false,
    bool CompilationEstablished = false, bool DependencyClosureEstablished = false, string? InspectedUtc = null);

public sealed class InventoryRunner
{
    public const long MaximumFileBytes = 32 * 1024 * 1024;
    public const long MaximumTotalBytes = 128 * 1024 * 1024;
    public const int MaximumNodes = 512;
    public const int MaximumRows = 100000;
    public static IReadOnlyList<string> CandidateSlots { get; } = Array.AsReadOnly(new[] {
        "Celeste.dll", "Celeste.exe", "orig/Celeste.dll", "orig/Celeste.exe"
    });
    public static IReadOnlyList<string> RequiredTypeNames { get; } = Array.AsReadOnly(new[] {
        "Celeste.Player", "Monocle.Actor", "Celeste.Actor", "Celeste.Solid",
        "Monocle.Entity", "Monocle.Scene", "Monocle.StateMachine", "Monocle.Sprite",
        "Celeste.PlayerHair", "Celeste.Input", "Monocle.Engine", "Celeste.Session",
        "Celeste.TheoCrystal", "Celeste.Glider", "Celeste.Spring", "Celeste.Refill",
        "Celeste.Water", "Celeste.Bumper", "Celeste.Puffer", "Celeste.Seeker"
    });
    private readonly Dictionary<string, AssemblyFact> files = new(StringComparer.OrdinalIgnoreCase);
    private long totalBytes;
    private string root = "";

    public AssemblyFact InspectReference(string explicitRoot, string name)
    {
        if (!Path.IsPathFullyQualified(explicitRoot)) throw new InventoryException("arguments", "EXPLICIT_ROOT_REQUIRED");
        if (name is not ("Microsoft.Xna.Framework" or "Microsoft.Xna.Framework.Game" or "Microsoft.Xna.Framework.Graphics" or
            "mscorlib" or "System" or "System.Core" or "System.Xml"))
            throw new InventoryException("reference", "REFERENCE_NAME_NOT_ALLOWLISTED");
        root = Path.GetFullPath(explicitRoot);
        EnsureSafePath(root);
        files.Clear();
        totalBytes = 0;
        return ReadSlot(name + ".dll") with { Classification = "reference-candidate-not-runtime-validated" };
    }

    public InventoryReport Inspect(string explicitRoot)
    {
        if (!Path.IsPathFullyQualified(explicitRoot))
            throw new InventoryException("arguments", "EXPLICIT_ROOT_REQUIRED");
        root = Path.GetFullPath(explicitRoot);
        if (!Directory.Exists(root)) throw new InventoryException("path", "INSTALLATION_MISSING");
        files.Clear();
        totalBytes = 0;
        EnsureSafePath(root);
        var candidates = CandidateSlots.Select(ReadSlot).ToArray();
        if (!candidates.Any(c => c.Status == "managed-assembly"))
            throw new InventoryException("candidate", "MANAGED_GAME_CANDIDATE_MISSING");
        var graph = new List<DependencyFact>();
        var pending = new Queue<AssemblyFact>(candidates.Where(c => c.Status == "managed-assembly"));
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current.Slot)) continue;
            var resolved = new List<ReferenceFact>();
            foreach (var reference in current.References)
            {
                if (!IsSafeAssemblyName(reference.Name))
                    throw new InventoryException("reference", "UNSAFE_ASSEMBLY_NAME");
                var slots = current.Slot.StartsWith("orig/", StringComparison.OrdinalIgnoreCase)
                    ? new[] { $"orig/{reference.Name}.dll", $"{reference.Name}.dll" }
                    : new[] { $"{reference.Name}.dll", $"orig/{reference.Name}.dll" };
                var available = slots.Select(ReadSlot).Where(f => f.Status == "managed-assembly").ToArray();
                var exact = available.Where(f => IdentityMatches(reference, f.Identity!)).ToArray();
                foreach (var item in exact) pending.Enqueue(item);
                var status = exact.Length > 0 ? "exact-metadata-match" :
                    available.Length > 0 ? "identity-mismatch" :
                    IsFrameworkName(reference.Name) ? "framework-not-inspected" : "missing-or-unreadable";
                resolved.Add(new(reference, status, exact.Select(a => a.Slot).ToArray()));
            }
            graph.Add(new(current.Slot, current, resolved));
        }
        return new(1, "CDR-081", candidates, graph, InspectedUtc: DateTimeOffset.UtcNow.ToString("O"));
    }

    public static bool IdentityMatches(AssemblyIdentity reference, AssemblyIdentity candidate) =>
        string.Equals(reference.Name, candidate.Name, StringComparison.OrdinalIgnoreCase) &&
        reference.Version == candidate.Version &&
        string.Equals(reference.Culture, candidate.Culture, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(reference.PublicKeyToken, candidate.PublicKeyToken, StringComparison.OrdinalIgnoreCase);

    public static bool IsSafeAssemblyName(string name) => name.Length is > 0 and <= 128 &&
        Regex.IsMatch(name, @"^[A-Za-z0-9_][A-Za-z0-9_.-]*$", RegexOptions.CultureInvariant) &&
        !name.Contains("..", StringComparison.Ordinal) &&
        !Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private AssemblyFact ReadSlot(string slot)
    {
        if (files.TryGetValue(slot, out var existing)) return existing;
        if (files.Count >= MaximumNodes) throw new InventoryException("budget", "FILE_COUNT_LIMIT");
        var relative = slot.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InventoryException("path", "PATH_ESCAPE");
        EnsureSafePath(path);
        AssemblyFact result;
        if (!File.Exists(path))
            result = Empty(slot, "missing", 0, null);
        else
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var length = stream.Length;
                if (length > MaximumFileBytes || length <= 0) throw new InventoryException("budget", "FILE_SIZE_LIMIT");
                if (totalBytes + length > MaximumTotalBytes) throw new InventoryException("budget", "TOTAL_BYTE_LIMIT");
                totalBytes += length;
                var hash = Convert.ToHexString(SHA256.HashData(stream));
                stream.Position = 0;
                using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
                if (!pe.HasMetadata)
                    result = Empty(slot, "native-pe", length, hash);
                else
                {
                    var reader = pe.GetMetadataReader();
                    if (!reader.IsAssembly) throw new BadImageFormatException();
                    if (reader.TypeDefinitions.Count > MaximumRows || reader.MethodDefinitions.Count > MaximumRows ||
                        reader.AssemblyReferences.Count > 256) throw new InventoryException("budget", "METADATA_ROW_LIMIT");
                    var assembly = reader.GetAssemblyDefinition();
                    var identity = new AssemblyIdentity(Safe(reader.GetString(assembly.Name)), assembly.Version.ToString(),
                        Safe(reader.GetString(assembly.Culture)), Token(reader.GetBlobBytes(assembly.PublicKey), true));
                    var references = reader.AssemblyReferences.Select(handle =>
                    {
                        var item = reader.GetAssemblyReference(handle);
                        return new AssemblyIdentity(Safe(reader.GetString(item.Name)), item.Version.ToString(),
                            Safe(reader.GetString(item.Culture)), Token(reader.GetBlobBytes(item.PublicKeyOrToken),
                                (item.Flags & AssemblyFlags.PublicKey) != 0));
                    }).ToArray();
                    var wanted = new HashSet<string>(StringComparer.Ordinal);
                    var mods = 0;
                    foreach (var handle in reader.TypeDefinitions)
                    {
                        var type = reader.GetTypeDefinition(handle);
                        var ns = reader.GetString(type.Namespace);
                        var name = reader.GetString(type.Name);
                        var full = ns + "." + name;
                        if (RequiredTypeNames.Contains(full)) wanted.Add(full);
                        if (ns == "Celeste.Mod" || ns.StartsWith("Celeste.Mod.", StringComparison.Ordinal) ||
                            name == "Everest" || name == "MonoModRules") mods++;
                    }
                    var native = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var handle in reader.MethodDefinitions)
                    {
                        var method = reader.GetMethodDefinition(handle);
                        if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0) continue;
                        var import = method.GetImport();
                        if (!import.Module.IsNil)
                            native.Add(Safe(reader.GetString(reader.GetModuleReference(import.Module).Name)));
                    }
                    var modReference = references.Any(r => r.Name.StartsWith("MonoMod", StringComparison.Ordinal) ||
                        r.Name == "NETCoreifier" || r.Name.StartsWith("Celeste.Mod", StringComparison.Ordinal));
                    result = new(slot, "managed-assembly", length, hash, identity, Safe(reader.MetadataVersion),
                        ReadTargetFramework(reader, assembly),
                        pe.PEHeaders.CoffHeader.Machine.ToString(), reader.TypeDefinitions.Count, mods,
                        modReference, mods > 0 || modReference ? "mod-bearing" : "unmodified-candidate-not-authenticated",
                        RequiredTypeNames.Select(name => new TypeFact(name, wanted.Contains(name))).ToArray(),
                        references, native.OrderBy(n => n, StringComparer.Ordinal).ToArray(), null);
                }
            }
            catch (InventoryException) { throw; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException or InvalidOperationException)
            {
                result = Empty(slot, "unreadable-or-invalid", 0, null, ex.GetType().Name) with { Failure = Diagnostics.Describe(ex, [root]) };
            }
        }
        files.Add(slot, result);
        return result;
    }

    private void EnsureSafePath(string path)
    {
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((Directory.Exists(current) || File.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InventoryException("path", "REPARSE_POINT");
    }

    private static string Token(byte[] bytes, bool fullKey)
    {
        if (bytes.Length == 0) return "";
        if (!fullKey) return Convert.ToHexString(bytes).ToLowerInvariant();
        var hash = SHA1.HashData(bytes);
        return Convert.ToHexString(hash.AsSpan(hash.Length - 8).ToArray().Reverse().ToArray()).ToLowerInvariant();
    }
    private static string? ReadTargetFramework(MetadataReader reader, AssemblyDefinition assembly)
    {
        foreach (var handle in assembly.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
            var constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            if (constructor.Parent.Kind != HandleKind.TypeReference) continue;
            var type = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
            if (reader.GetString(type.Namespace) != "System.Runtime.Versioning" ||
                reader.GetString(type.Name) != "TargetFrameworkAttribute") continue;
            var blob = reader.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() != 1) throw new BadImageFormatException();
            return Safe(blob.ReadSerializedString() ?? "");
        }
        return null;
    }
    private static string Safe(string value)
    {
        if (value.Length > 256 || value.Any(char.IsControl) || value.Contains('/') || value.Contains('\\') || value.Contains(':'))
            throw new InventoryException("metadata", "UNSAFE_METADATA_STRING");
        return value;
    }
    private static bool IsFrameworkName(string name) => name == "mscorlib" || name == "netstandard" ||
        name == "System" || name.StartsWith("System.", StringComparison.Ordinal);
    private static AssemblyFact Empty(string slot, string status, long length, string? hash, string? error = null) =>
        new(slot, status, length, hash, null, null, null, null, 0, 0, false, "not-classified", [], [], [], error);
}
