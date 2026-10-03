using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

// Fixed declared-dependency slots only; no loader, resolver or recursive discovery.
internal static class SystemDependencyAudit
{
    private static readonly string[] Slots = {
        @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\Microsoft.VisualC\v4.0_10.0.0.0__b03f5f7f11d50a3a\Microsoft.VisualC.dll",
        @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\Microsoft.Xna.Framework.GamerServices\v4.0_4.0.0.0__842cf8be1de50553\Microsoft.Xna.Framework.GamerServices.dll",
        @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\Microsoft.Xna.Framework.Input.Touch\v4.0_4.0.0.0__842cf8be1de50553\Microsoft.Xna.Framework.Input.Touch.dll",
        @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Windows.Forms\v4.0_4.0.0.0__b77a5c561934e089\System.Windows.Forms.dll",
        @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Drawing\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Drawing.dll",
        @"C:\Windows\Microsoft.NET\assembly\GAC_32\mscorlib\v4.0_4.0.0.0__b77a5c561934e089\mscorlib.dll",
        @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System\v4.0_4.0.0.0__b77a5c561934e089\System.dll",
        @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Core\v4.0_4.0.0.0__b77a5c561934e089\System.Core.dll",
        @"C:\Windows\SysWOW64\msvcr100.dll", @"C:\Windows\SysWOW64\xinput1_3.dll",
        @"C:\Windows\SysWOW64\kernel32.dll", @"C:\Windows\SysWOW64\user32.dll"
    };

    public static object Run() => new { taskId = "CDR-082", stage = "readonly-system-dependency-audit",
        rows = Slots.Select(ReadApproved).ToArray(), targetExecuted = false, dependenciesCopied = false,
        recursivelyResolved = false, runtimeSafetyEstablished = false, gameDirectoryAccessed = false };

    private static object ReadApproved(string path)
    {
        ValidateSlot(path);
        for (var cursor = path; cursor is not null; cursor = Path.GetDirectoryName(cursor))
            if ((File.Exists(cursor) || Directory.Exists(cursor)) && (File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("SYSTEM_DEPENDENCY_LINK");
        if (!File.Exists(path)) return new { name = Path.GetFileName(path), status = "missing-in-fixed-slot" };
        using var input = File.OpenRead(path);
        if (input.Length > 128 * 1024 * 1024) throw new InvalidDataException("SYSTEM_DEPENDENCY_SIZE");
        var before = Convert.ToHexString(SHA256.HashData(input)); input.Position = 0;
        using var pe = new PEReader(input, PEStreamOptions.LeaveOpen);
        var fact = Inspect(pe);
        input.Position = 0;
        if (before != Convert.ToHexString(SHA256.HashData(input))) throw new InvalidDataException("SYSTEM_DEPENDENCY_CHANGED");
        return new { name = Path.GetFileName(path), status = "inspected", sha256 = before, hashesStable = true, fact };
    }

    private static void ValidateSlot(string path)
    {
        if (!Slots.Contains(path, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("SYSTEM_DEPENDENCY_SCOPE");
    }

    private static object Inspect(PEReader pe)
    {
        var header = pe.PEHeaders.PEHeader ?? throw new InvalidDataException("SYSTEM_DEPENDENCY_NO_PE");
        byte[] Read(int rva, int count) {
            if (rva <= 0 || count < 0 || count > 4096) throw new InvalidDataException("IMPORT_RVA_BUDGET");
            var data = pe.GetSectionData(rva);
            if (data.Length < count) throw new InvalidDataException("IMPORT_TRUNCATED");
            return data.GetContent(0, count).ToArray();
        }
        var imports = Imports(Read, header.ImportTableDirectory.RelativeVirtualAddress, header.ImportTableDirectory.Size, false);
        var delays = Imports(Read, header.DelayImportTableDirectory.RelativeVirtualAddress, header.DelayImportTableDirectory.Size, true);
        string? name = null, version = null, token = null; string[] references = Array.Empty<string>();
        var moduleInitializer = false; var pinvoke = 0; string[] pinvokeModules = Array.Empty<string>();
        if (pe.HasMetadata)
        {
            var reader = pe.GetMetadataReader();
            if (reader.MethodDefinitions.Count > 200000 || reader.TypeDefinitions.Count > 50000) throw new InvalidDataException("SYSTEM_METADATA_BUDGET");
            var identity = reader.GetAssemblyDefinition(); name = reader.GetString(identity.Name); version = identity.Version.ToString();
            var key = reader.GetBlobBytes(identity.PublicKey);
            token = key.Length == 0 ? "" : Convert.ToHexString(SHA1.HashData(key)[^8..].Reverse().ToArray()).ToLowerInvariant();
            references = reader.AssemblyReferences.Select(h => reader.GetString(reader.GetAssemblyReference(h).Name)).ToArray();
            var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in reader.MethodDefinitions)
            {
                var method = reader.GetMethodDefinition(handle);
                if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0) {
                    pinvoke++; var import = method.GetImport();
                    if (!import.Module.IsNil) modules.Add(reader.GetString(reader.GetModuleReference(import.Module).Name));
                }
            }
            pinvokeModules = modules.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var handle in reader.TypeDefinitions) {
                var type = reader.GetTypeDefinition(handle);
                if (reader.GetString(type.Name) == "<Module>")
                    moduleInitializer |= type.GetMethods().Any(h => reader.GetString(reader.GetMethodDefinition(h).Name) == ".cctor");
            }
        }
        return new { machine = pe.PEHeaders.CoffHeader.Machine.ToString(), managed = pe.HasMetadata,
            ilOnly = pe.PEHeaders.CorHeader is null ? (bool?)null : (pe.PEHeaders.CorHeader.Flags & CorFlags.ILOnly) != 0,
            name, version, publicKeyToken = token, references, moduleInitializer, pinvokeMethods = pinvoke, pinvokeModules,
            nativeImports = imports, delayImports = delays, nativeEntryPointPresent = header.AddressOfEntryPoint != 0,
            tlsDirectoryPresent = header.ThreadLocalStorageTableDirectory.Size != 0 };
    }

    internal static string[] Imports(Func<int, int, byte[]> read, int rva, int size, bool delay)
    {
        if (rva == 0 && size == 0) return Array.Empty<string>();
        var width = delay ? 32 : 20;
        if (rva <= 0 || size < width || size > 32768) throw new InvalidDataException("IMPORT_DIRECTORY_BUDGET");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var offset = 0; offset <= size - width; offset += width)
        {
            var descriptor = read(checked(rva + offset), width);
            if (descriptor.All(b => b == 0)) return names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            if (delay && BitConverter.ToUInt32(descriptor, 0) != 1) throw new InvalidDataException("DELAY_IMPORT_VA_UNSUPPORTED");
            var nameRva = checked((int)BitConverter.ToUInt32(descriptor, delay ? 4 : 12));
            var chars = new List<char>();
            for (var index = 0; index < 256; index++) {
                var b = read(checked(nameRva + index), 1)[0];
                if (b == 0) { if (chars.Count == 0) throw new InvalidDataException("IMPORT_EMPTY_NAME"); break; }
                if (b < 32 || b > 126 || index == 255) throw new InvalidDataException("IMPORT_NAME_BUDGET");
                chars.Add((char)b);
            }
            names.Add(new string(chars.ToArray()));
        }
        throw new InvalidDataException("IMPORT_TERMINATOR_MISSING");
    }

    public static void SelfTest()
    {
        var fixture = new byte[512]; fixture[112] = 200;
        System.Text.Encoding.ASCII.GetBytes("fixture.dll\0").CopyTo(fixture, 200);
        byte[] Read(int rva, int count) => fixture.AsSpan(rva, count).ToArray();
        if (!Imports(Read, 100, 40, false).SequenceEqual(new[] { "fixture.dll" })) throw new InvalidDataException("IMPORT_FIXTURE_FAILED");
        fixture[300] = 1; fixture[304] = 200;
        if (!Imports(Read, 300, 64, true).SequenceEqual(new[] { "fixture.dll" })) throw new InvalidDataException("DELAY_IMPORT_FIXTURE_FAILED");
        var rejected = false; try { Imports(Read, 100, 20, false); } catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new InvalidDataException("IMPORT_NEGATIVE_FAILED");
        fixture[300] = 0; rejected = false;
        try { Imports(Read, 300, 64, true); } catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new InvalidDataException("DELAY_IMPORT_VA_NEGATIVE_FAILED");
        rejected = false; try { ValidateSlot(@"C:\Windows\SysWOW64\unapproved.dll"); } catch (ArgumentException) { rejected = true; }
        if (!rejected) throw new InvalidDataException("SYSTEM_SCOPE_NEGATIVE_FAILED");
        using var own = File.OpenRead(typeof(SystemDependencyAudit).Assembly.Location);
        using var pe = new PEReader(own);
        Inspect(pe); // Own managed PE only; no system dependency read.
    }
}
