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

    public static InitializationFact Analyze(PEReader pe, MetadataReader reader, IEnumerable<MethodDefinitionHandle> roots)
    {
        var rootList = roots.ToArray();
        var pending = new Stack<MethodDefinitionHandle>(rootList);
        var visited = new HashSet<MethodDefinitionHandle>();
        var managed = 0; var native = 0; var pinvoke = 0; var memberBoundaries = 0; var indirect = 0; var unknownBodies = 0;
        while (pending.Count != 0)
        {
            var handle = pending.Pop(); if (!visited.Add(handle)) continue;
            if (visited.Count > 4096) throw new InvalidDataException("XNA_INITIALIZER_GRAPH_BUDGET");
            var method = reader.GetMethodDefinition(handle);
            if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0) { pinvoke++; continue; }
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
                    else if (target.Kind == HandleKind.MemberReference) memberBoundaries++;
                    else throw new InvalidDataException("XNA_INITIALIZER_TOKEN_KIND");
                }
                position += width;
                void Require(int count) { if (count > bytes.Length - position) throw new InvalidDataException("XNA_INITIALIZER_TRUNCATED_IL"); }
            }
        }
        return new InitializationFact(rootList.Length, managed, native, pinvoke, memberBoundaries, indirect, unknownBodies,
            "conservative-local-method-graph", false);
    }
}
internal sealed record InitializationFact(int rootCount, int visitedManagedMethods, int nativeMethodBoundaries,
    int pinvokeBoundaries, int memberReferenceBoundaries, int indirectCalls, int unresolvedBodies,
    string method, bool runtimeSafetyEstablished);

internal static class AuditTrap
{
    static AuditTrap() { throw new InvalidOperationException("STATIC_AUDIT_MUST_NOT_INVOKE"); }
}
