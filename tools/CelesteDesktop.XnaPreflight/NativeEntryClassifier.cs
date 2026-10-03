using System.Reflection.PortableExecutable;

// Classifies a small entry prefix and matches declared PE import slots.
// Not a general disassembler, control-flow analyzer or execution-safety proof.
internal static class NativeEntryClassifier
{
    internal static (string Form, uint? Address) Prefix(byte[] bytes, uint entryVa)
    {
        if (bytes.Length < 6) throw new InvalidDataException("NATIVE_PREFIX_TRUNCATED");
        if (bytes[0] == 0xff && bytes[1] == 0x25) return ("absolute-indirect-jump", BitConverter.ToUInt32(bytes, 2));
        if (bytes[0] == 0xe9) return ("relative-jump", unchecked(entryVa + 5 + (uint)BitConverter.ToInt32(bytes, 1)));
        if (bytes[0] == 0xb8 && bytes[5] == 0xc3) return ("constant-return", BitConverter.ToUInt32(bytes, 1));
        return ("body-unclassified", null);
    }

    public static object Classify(PEReader pe, int rva)
    {
        var header = pe.PEHeaders.PEHeader ?? throw new InvalidDataException("NATIVE_NO_PE");
        if (header.Magic != PEMagic.PE32) throw new InvalidDataException("NATIVE_PREFIX_ARCH_UNSUPPORTED");
        byte[] Read(int address, int count) {
            if (address <= 0 || count < 0 || count > 4096) throw new InvalidDataException("NATIVE_READ_BUDGET");
            var block = pe.GetSectionData(address);
            if (block.Length < count) throw new InvalidDataException("NATIVE_READ_TRUNCATED");
            return block.GetContent(0, count).ToArray();
        }
        string Name(int address) {
            var result = new List<char>();
            for (var index = 0; index < 256; index++) {
                var b = Read(checked(address + index), 1)[0];
                if (b == 0) return new string(result.ToArray());
                if (b < 32 || b > 126) throw new InvalidDataException("NATIVE_NAME_INVALID");
                result.Add((char)b);
            }
            throw new InvalidDataException("NATIVE_NAME_BUDGET");
        }
        var prefix = Prefix(Read(rva, 6), checked((uint)(header.ImageBase + (ulong)rva)));
        if (prefix.Form == "constant-return" && prefix.Address >= header.ImageBase && prefix.Address < header.ImageBase + (ulong)header.SizeOfImage)
            return new NativeEntryFact("image-address-return", null, null, false, checked((int)(prefix.Address!.Value - header.ImageBase)));
        if (prefix.Form == "absolute-indirect-jump") {
            var directory = header.ImportTableDirectory;
            if (directory.Size < 20 || directory.Size > 32768) throw new InvalidDataException("NATIVE_IMPORT_BUDGET");
            for (var offset = 0; offset <= directory.Size - 20; offset += 20) {
                var descriptor = Read(checked(directory.RelativeVirtualAddress + offset), 20);
                if (descriptor.All(b => b == 0)) break;
                var lookup = BitConverter.ToUInt32(descriptor, 0);
                var iat = BitConverter.ToUInt32(descriptor, 16);
                if (prefix.Address < header.ImageBase + iat) continue;
                var delta = prefix.Address!.Value - (header.ImageBase + iat);
                if (delta % 4 != 0 || delta / 4 >= 4096) continue;
                var index = checked((int)(delta / 4));
                var lookupRva = checked((int)(lookup == 0 ? iat : lookup));
                // Ensure the candidate slot is before this table's null terminator.
                uint target = 0; var terminated = false;
                for (var current = 0; current <= index; current++) {
                    target = BitConverter.ToUInt32(Read(checked(lookupRva + current * 4), 4));
                    if (target == 0) { terminated = true; break; }
                }
                if (terminated) continue;
                var module = Name(checked((int)BitConverter.ToUInt32(descriptor, 12)));
                var symbol = (target & 0x80000000) != 0 ? "ordinal-import" : Name(checked((int)target + 2));
                return new NativeEntryFact("declared-import-thunk", module, symbol, false, null);
            }
        }
        return new NativeEntryFact(prefix.Form, null, null, false, null);
    }

    public static void SelfTest()
    {
        var absolute = Prefix(new byte[] { 0xff, 0x25, 0x44, 0x33, 0x22, 0x11 }, 0x1000);
        if (absolute.Form != "absolute-indirect-jump" || absolute.Address != 0x11223344) throw new InvalidDataException("NATIVE_PREFIX_ABSOLUTE_TEST");
        var relative = Prefix(new byte[] { 0xe9, 0xfb, 0xff, 0xff, 0xff, 0 }, 0x1000);
        if (relative.Form != "relative-jump" || relative.Address != 0x1000) throw new InvalidDataException("NATIVE_PREFIX_RELATIVE_TEST");
        var rejected = false; try { Prefix(new byte[] { 0xff }, 0); } catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new InvalidDataException("NATIVE_PREFIX_NEGATIVE_TEST");
        var constant = Prefix(new byte[] { 0xb8, 0x78, 0x56, 0x34, 0x12, 0xc3 }, 0x1000);
        if (constant.Form != "constant-return" || constant.Address != 0x12345678) throw new InvalidDataException("NATIVE_PREFIX_CONSTANT_TEST");
    }
}
internal sealed record NativeEntryFact(string form, string? declaredImportModule, string? declaredImportSymbol,
    bool runtimeSafetyEstablished, int? referencedImageRva);
