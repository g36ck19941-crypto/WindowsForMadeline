using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

// Conservative local method-token traversal only. Does not resolve/load any assembly,
// evaluate branches, invoke constructors or claim a complete native initialization audit.
internal static class InitializationAnalyzer
{
    private static readonly Dictionary<ushort, OpCode> Codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(c => unchecked((ushort)c.Value));

    public static InitializationFact Analyze(PEReader pe, MetadataReader reader, IEnumerable<MethodDefinitionHandle> roots,
        ICollection<object>? privateDetails = null)
    {
        var rootList = roots.ToArray();
        var pending = new Stack<MethodDefinitionHandle>(rootList);
        var visited = new HashSet<MethodDefinitionHandle>();
        var managed = 0; var native = 0; var pinvoke = 0; var memberBoundaries = 0; var indirect = 0; var unknownBodies = 0;
        var nativeRvaImports = 0; var emptyModuleImports = 0;
        var memberScopes = new Dictionary<string, int>();
        var nativeEntryForms = new Dictionary<string, int>();
        while (pending.Count != 0)
        {
            var handle = pending.Pop(); if (!visited.Add(handle)) continue;
            if (visited.Count > 4096) throw new InvalidDataException("XNA_INITIALIZER_GRAPH_BUDGET");
            var method = reader.GetMethodDefinition(handle);
            if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0) {
                pinvoke++;
                var import = method.GetImport();
                var moduleName = import.Module.IsNil ? "" : reader.GetString(reader.GetModuleReference(import.Module).Name);
                if (moduleName.Length == 0) emptyModuleImports++;
                object? entryFact = null;
                if ((method.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native && method.RelativeVirtualAddress != 0) {
                    nativeRvaImports++;
                    var classified = (NativeEntryFact)NativeEntryClassifier.Classify(pe, method.RelativeVirtualAddress);
                    nativeEntryForms[classified.form] = nativeEntryForms.GetValueOrDefault(classified.form) + 1;
                    entryFact = classified;
                }
                privateDetails?.Add(new { kind = "pinvoke-boundary", token = MetadataTokens.GetToken(handle),
                    name = reader.GetString(method.Name), importName = reader.GetString(import.Name), moduleName,
                    implementation = method.ImplAttributes.ToString(), rva = method.RelativeVirtualAddress, entryFact });
                continue;
            }
            if ((method.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native) { native++; continue; }
            if (method.RelativeVirtualAddress == 0 || (method.ImplAttributes & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL)
            { unknownBodies++; continue; }
            managed++;
            var bytes = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes() ?? throw new InvalidDataException("XNA_INITIALIZER_EMPTY_IL");
            if (bytes.Length > 1048576) throw new InvalidDataException("XNA_INITIALIZER_IL_BUDGET");
            var position = 0;
            while (position < bytes.Length)
            {
                ushort code = bytes[position++];
                if (code == 0xfe) { Require(1); code = (ushort)(0xfe00 | bytes[position++]); }
                if (!Codes.TryGetValue(code, out var op)) throw new InvalidDataException("XNA_INITIALIZER_UNKNOWN_OPCODE");
                var width = op.OperandType switch {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    _ => 4 };
                Require(width);
                if (op.OperandType == OperandType.InlineSwitch)
                {
                    var count = BitConverter.ToInt32(bytes, position);
                    if (count < 0 || count > 65536) throw new InvalidDataException("XNA_INITIALIZER_SWITCH_BUDGET");
                    width = checked(4 + count * 4); Require(width);
                }
                if (op == OpCodes.Calli) indirect++;
                if (op.OperandType == OperandType.InlineMethod)
                {
                    var target = MetadataTokens.EntityHandle(BitConverter.ToInt32(bytes, position));
                    if (target.Kind == HandleKind.MethodSpecification) target = reader.GetMethodSpecification((MethodSpecificationHandle)target).Method;
                    if (target.Kind == HandleKind.MethodDefinition) pending.Push((MethodDefinitionHandle)target);
                    else if (target.Kind == HandleKind.MemberReference) {
                        memberBoundaries++;
                        var reference = reader.GetMemberReference((MemberReferenceHandle)target);
                        var scope = MemberScope(reader, reference.Parent);
                        memberScopes[scope] = memberScopes.GetValueOrDefault(scope) + 1;
                        privateDetails?.Add(new { kind = "member-boundary", token = MetadataTokens.GetToken(target),
                            name = reader.GetString(reference.Name), parentKind = reference.Parent.Kind.ToString(), scope });
                    }
                    else throw new InvalidDataException("XNA_INITIALIZER_TOKEN_KIND");
                }
                position += width;
                void Require(int count) { if (count > bytes.Length - position) throw new InvalidDataException("XNA_INITIALIZER_TRUNCATED_IL"); }
            }
        }
        return new InitializationFact(rootList.Length, managed, native, pinvoke, memberBoundaries, indirect, unknownBodies,
            "conservative-local-method-graph", false, nativeRvaImports, emptyModuleImports, memberScopes, nativeEntryForms);
    }

    private static string MemberScope(MetadataReader reader, EntityHandle parent)
    {
        for (var depth = 0; depth < 32; depth++) {
            if (parent.Kind == HandleKind.TypeReference) { parent = reader.GetTypeReference((TypeReferenceHandle)parent).ResolutionScope; continue; }
            if (parent.Kind == HandleKind.AssemblyReference) return reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)parent).Name);
            if (parent.Kind is HandleKind.ModuleDefinition or HandleKind.TypeDefinition or HandleKind.MethodDefinition) return "local-unresolved";
            if (parent.Kind == HandleKind.ModuleReference) return "module-reference-unresolved";
            return "unresolved-" + parent.Kind;
        }
        throw new InvalidDataException("XNA_MEMBER_SCOPE_BUDGET");
    }
}
internal sealed record InitializationFact(int rootCount, int visitedManagedMethods, int nativeMethodBoundaries,
    int pinvokeBoundaries, int memberReferenceBoundaries, int indirectCalls, int unresolvedBodies,
    string method, bool runtimeSafetyEstablished, int pinvokeWithNativeRva, int pinvokeEmptyModuleNames,
    Dictionary<string, int> memberReferenceScopes, Dictionary<string, int> nativeEntryForms);

internal static class AuditTrap
{
    static AuditTrap() { throw new InvalidOperationException("STATIC_AUDIT_MUST_NOT_INVOKE"); }
}
