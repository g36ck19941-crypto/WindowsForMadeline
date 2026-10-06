using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Metadata tables/signatures/constants only. No method-body decoding or reference resolution;
// approved files are also streamed in full for SHA-256 consistency checks.
internal static class TypeContractAudit
{
    private static readonly Dictionary<string,string> Approved=new(){
        ["Microsoft.Xna.Framework"]="38E7093F52D7474BBC6256906519781A1210D7DA50A1C667B52716FCF49CA130",
        ["Microsoft.Xna.Framework.Game"]="B5DFFDD8125ABEF2A4507BA4E1D2F11062143F0A63D48FE4F298B95AD746A1F0",
        ["Microsoft.Xna.Framework.Graphics"]="560080FC39021C611CA9D076DCEBED312FAF6D7D1413C2DC523683EA635E9F55"};
    private static readonly string[] Selected=["Microsoft.Xna.Framework.Input.Keys","Microsoft.Xna.Framework.Input.Buttons",
        "Microsoft.Xna.Framework.Input.ButtonState","Microsoft.Xna.Framework.Input.KeyboardState","Microsoft.Xna.Framework.Input.MouseState",
        "Microsoft.Xna.Framework.Input.GamePadState","Microsoft.Xna.Framework.Input.GamePadButtons","Microsoft.Xna.Framework.Input.GamePadDPad",
        "Microsoft.Xna.Framework.Input.GamePadThumbSticks","Microsoft.Xna.Framework.Input.GamePadTriggers","Microsoft.Xna.Framework.Input.GamePadDeadZone",
        "Microsoft.Xna.Framework.PlayerIndex","Microsoft.Xna.Framework.Vector2","Microsoft.Xna.Framework.Vector3","Microsoft.Xna.Framework.Matrix",
        "Microsoft.Xna.Framework.Rectangle","Microsoft.Xna.Framework.Color","Microsoft.Xna.Framework.MathHelper","Microsoft.Xna.Framework.GameTime",
        "Microsoft.Xna.Framework.Game","Microsoft.Xna.Framework.GraphicsDeviceManager","Microsoft.Xna.Framework.Graphics.GraphicsDevice"];
    public static int Run(string rootArgument,string outputArgument)
    {
        var checks=OwnChecks();var repo=Directory.GetCurrentDirectory();
        if(!File.Exists(Path.Combine(repo,"CelesteDesktopRuntime.sln")))throw new InvalidDataException("M2T_REPO");
        var root=Path.GetFullPath(rootArgument);var output=Path.GetFullPath(outputArgument);
        Inside(Path.Combine(repo,"local-cache","cdr-082-xna"),root);
        Inside(Path.Combine(repo,"local-cache","cdr-082-type-contracts"),output);
        if(Path.GetFileName(root)!="runtime" || Directory.Exists(output)||File.Exists(output))throw new InvalidDataException("M2T_SCOPE");
        var manifest=Path.Combine(Path.GetDirectoryName(root)!,"summary.json");NoLinks(manifest);
        if(new FileInfo(manifest).Length>65536)throw new InvalidDataException("M2T_MANIFEST_BUDGET");
        using(var m=JsonDocument.Parse(File.ReadAllText(manifest)))
            if(m.RootElement.GetProperty("stage").GetString()!="approved-xna-private-cache" || m.RootElement.GetProperty("copiedFiles").GetInt32()!=3 ||
                m.RootElement.GetProperty("assemblyExecuted").GetBoolean())throw new InvalidDataException("M2T_MANIFEST");
        var details=new List<object>();var publicRows=new List<object>();var found=new List<string>();
        int memberCount=0,enumConstants=0,staticConstructors=0,mixed=0,moduleInitializers=0;
        foreach(var pair in Approved)
        {
            var path=Path.Combine(root,pair.Key+".dll");NoLinks(path);
            if(new FileInfo(path).Length>16*1024*1024 || Hash(path)!=pair.Value)throw new InvalidDataException("M2T_BASELINE");
            using var input=File.OpenRead(path);using var pe=new PEReader(input);var r=pe.GetMetadataReader();
            if(r.TypeDefinitions.Count>10000 || r.MethodDefinitions.Count>50000 || r.AssemblyReferences.Count>256)throw new InvalidDataException("M2T_TABLE_BUDGET");
            var a=r.GetAssemblyDefinition();
            if(r.GetString(a.Name)!=pair.Key || a.Version.ToString()!="4.0.0.0" || Token(r.GetBlobBytes(a.PublicKey))!="842cf8be1de50553")throw new InvalidDataException("M2T_IDENTITY");
            var ilOnly=(pe.PEHeaders.CorHeader!.Flags&CorFlags.ILOnly)!=0;if(!ilOnly)mixed++;
            var selectedRows=new List<object>();var modules=0;
            foreach(var h in r.TypeDefinitions)
            {
                var t=r.GetTypeDefinition(h);var full=Name(r,h);
                if(full=="<Module>")modules+=t.GetMethods().Count(m=>r.GetString(r.GetMethodDefinition(m).Name)==".cctor");
                if(!Selected.Contains(full,StringComparer.Ordinal))continue;
                found.Add(full);var decoder=new Names();var fields=new List<object>();var methods=new List<object>();var constants=0;var constructors=0;
                foreach(var fh in t.GetFields())
                {
                    var f=r.GetFieldDefinition(fh);var constant=f.GetDefaultValue();string? hex=null;string? constantType=null;
                    if(!constant.IsNil){var c=r.GetConstant(constant);hex=Convert.ToHexString(r.GetBlobBytes(c.Value));constantType=c.TypeCode.ToString();constants++;}
                    fields.Add(new{name=r.GetString(f.Name),type=f.DecodeSignature(decoder,null),attributes=f.Attributes.ToString(),offset=f.GetOffset(),constantType,constantBytesHex=hex});
                }
                foreach(var mh in t.GetMethods())
                {
                    var m=r.GetMethodDefinition(mh);var sig=m.DecodeSignature(decoder,null);var name=r.GetString(m.Name);
                    if(name==".cctor")constructors++;
                    methods.Add(new{name,returnType=sig.ReturnType,parameters=sig.ParameterTypes,requiredParameters=sig.RequiredParameterCount,
                        header=sig.Header.RawValue,attributes=m.Attributes.ToString(),implementation=m.ImplAttributes.ToString(),bodyPresent=m.RelativeVirtualAddress!=0});
                }
                var properties=t.GetProperties().Select(ph=>{var p=r.GetPropertyDefinition(ph);var s=p.DecodeSignature(decoder,null);return new{name=r.GetString(p.Name),returnType=s.ReturnType,parameters=s.ParameterTypes};}).ToArray();
                var layout=t.GetLayout();
                selectedRows.Add(new{name=full,attributes=t.Attributes.ToString(),baseType=EntityName(r,t.BaseType),packing=layout.PackingSize,size=layout.Size,fields,methods,properties});
                memberCount+=fields.Count+methods.Count+properties.Length;enumConstants+=constants;staticConstructors+=constructors;
            }
            moduleInitializers+=modules;
            var refs=r.AssemblyReferences.Select(h=>{var v=r.GetAssemblyReference(h);return new{name=r.GetString(v.Name),version=v.Version.ToString()};}).ToArray();
            details.Add(new{assembly=pair.Key,sha256=pair.Value,ilOnly,moduleInitializers=modules,references=refs,types=selectedRows});
            publicRows.Add(new{assembly=pair.Key,sha256=pair.Value,ilOnly,moduleInitializerDeclarations=modules,selectedTypes=selectedRows.Count,referenceDeclarations=refs.Length});
            NoLinks(path);if(Hash(path)!=pair.Value)throw new InvalidDataException("M2T_CHANGED");
        }
        if(found.Distinct(StringComparer.Ordinal).Count()!=found.Count)throw new InvalidDataException("M2T_AMBIGUOUS_TYPE");
        var missing=Selected.Except(found,StringComparer.Ordinal).ToArray();
        var ownPaths=new[]{"src/CelesteDesktop.RuntimeIsolation/IsolationSession.cs","src/CelesteDesktop.Contracts/Assets/Bgra32Frame.cs",
            "src/CelesteDesktop.Contracts/Assets/SpritePointDescriptor.cs","src/CelesteDesktop.Contracts/Assets/SpriteOriginDescriptor.cs",
            "src/CelesteDesktop.Contracts/Assets/SpriteFrameMetadataDescriptor.cs"};
        var ownHashes=new Dictionary<string,string>();
        foreach(var p in ownPaths){var path=Path.Combine(repo,p);NoLinks(path);if(new FileInfo(path).Length>1024*1024)throw new InvalidDataException("M2T_OWN_CONTRACT_BUDGET");ownHashes.Add(p,Hash(path));}
        if(AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name?.StartsWith("Microsoft.Xna.Framework",StringComparison.Ordinal)==true || a.GetName().Name is "Celeste" or "Steamworks.NET"))throw new InvalidDataException("M2T_TARGET_LOADED");
        var options=new JsonSerializerOptions{WriteIndented=true};
        var privateText=JsonSerializer.Serialize(new{details,missing,ownContractHashes=ownHashes,method="metadata-only-no-bodies-no-reference-resolution"},options);
        if(Encoding.UTF8.GetByteCount(privateText)>16*1024*1024)throw new InvalidDataException("M2T_DETAIL_BUDGET");
        foreach(var p in ownHashes){NoLinks(Path.Combine(repo,p.Key));if(Hash(Path.Combine(repo,p.Key))!=p.Value)throw new InvalidDataException("M2T_OWN_CONTRACT_CHANGED");}
        Directory.CreateDirectory(output);NoLinks(output);File.WriteAllText(Path.Combine(output,"types-local-only.json"),privateText);
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{schemaVersion=1,taskId="CDR-082-M2-T",stage="cached-type-metadata-review",inspectedUtc=DateTimeOffset.UtcNow,
            inspectedAssemblies=3,requestedTypes=Selected.Length,foundTypes=found.Count,missingTypes=missing.Length,memberDeclarations=memberCount,constantDeclarations=enumConstants,
            selectedTypeStaticConstructors=staticConstructors,mixedModeAssemblies=mixed,moduleInitializerDeclarations=moduleInitializers,rows=publicRows,
            ownContractFiles=ownPaths.Length,ownMetadataChecks=checks,hashesStable=true,metadataSurfaceEstablished=missing.Length==0,behaviorSemanticsEstablished=false,modernCompatibilityEstablished=false,
            sourceEdited=false,originalCompiled=false,originalExecuted=false,targetAssemblyLoaded=false,newDependencyRead=false,assetsRead=false,probeRun=false,guiOpened=false,installationAccessed=false,publicSourceBytes=0},options));
        Console.WriteLine($"TYPE_CONTRACTS_COMPLETED assemblies=3 requestedTypes={Selected.Length} foundTypes={found.Count} missingTypes={missing.Length} memberDeclarations={memberCount} mixedModeAssemblies={mixed} hashesStable=true targetAssemblyLoaded=false modernCompatibilityEstablished=false");
        return 0;
    }
    private static int OwnChecks()
    {
        void Check(bool ok){if(!ok)throw new InvalidDataException("M2T_OWN_CHECK");}
        using var stream=File.OpenRead(typeof(TypeContractAudit).Assembly.Location);using var pe=new PEReader(stream);var r=pe.GetMetadataReader();
        var h=r.TypeDefinitions.Single(h=>r.GetString(r.GetTypeDefinition(h).Name)==nameof(OwnMetadataTrap));var t=r.GetTypeDefinition(h);
        Check(Name(r,h).EndsWith(nameof(OwnMetadataTrap),StringComparison.Ordinal));Check(t.GetMethods().Any(m=>r.GetString(r.GetMethodDefinition(m).Name)==".cctor"));
        var method=t.GetMethods().Single(m=>r.GetString(r.GetMethodDefinition(m).Name)=="Read");var s=r.GetMethodDefinition(method).DecodeSignature(new Names(),null);
        Check(s.ReturnType=="Int32");Check(s.ParameterTypes.SequenceEqual(new[]{"Single","Int32&"}));
        var f=t.GetFields().Single(f=>r.GetString(r.GetFieldDefinition(f).Name)=="Code");var c=r.GetConstant(r.GetFieldDefinition(f).GetDefaultValue());
        Check(c.TypeCode==ConstantTypeCode.Int32);Check(r.GetBlobBytes(c.Value).SequenceEqual(new byte[]{7,0,0,0}));
        var rejected=false;try{Inside("local-cache/cdr-082-xna","artifacts/outside");}catch(ArgumentException){rejected=true;}Check(rejected);
        Check(AppDomain.CurrentDomain.GetAssemblies().All(a=>a.GetName().Name?.StartsWith("Microsoft.Xna.Framework",StringComparison.Ordinal)!=true));return 8;
    }
    private static string Token(byte[] key)=>Convert.ToHexString(SHA1.HashData(key).TakeLast(8).Reverse().ToArray()).ToLowerInvariant();
    private static string Name(MetadataReader r,TypeDefinitionHandle h){var t=r.GetTypeDefinition(h);var name=r.GetString(t.Name);return t.GetDeclaringType().IsNil?Join(r.GetString(t.Namespace),name):Name(r,t.GetDeclaringType())+"+"+name;}
    private static string Join(string ns,string name)=>ns.Length==0?name:ns+"."+name;
    private static string EntityName(MetadataReader r,EntityHandle h)=>h.Kind switch{HandleKind.TypeDefinition=>Name(r,(TypeDefinitionHandle)h),HandleKind.TypeReference=>Join(r.GetString(r.GetTypeReference((TypeReferenceHandle)h).Namespace),r.GetString(r.GetTypeReference((TypeReferenceHandle)h).Name)),_=>h.IsNil?"none":"unresolved-"+h.Kind};
    private static string Hash(string p){using var s=File.OpenRead(p);return Convert.ToHexString(SHA256.HashData(s));}
    private static void Inside(string root,string p){if(!Path.GetFullPath(p).StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("M2T_PATH_SCOPE");NoLinks(p);}
    private static void NoLinks(string p){for(string? c=Path.GetFullPath(p);c!=null;c=Path.GetDirectoryName(c))if((File.Exists(c)||Directory.Exists(c))&&(File.GetAttributes(c)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("M2T_LINK");}
    private sealed class Names:ISignatureTypeProvider<string,object?>
    {
        public string GetArrayType(string e,ArrayShape s)=>e+"[rank="+s.Rank+"]";
        public string GetByReferenceType(string e)=>e+"&";
        public string GetFunctionPointerType(MethodSignature<string> s)=>"fnptr:"+s.ReturnType+"("+string.Join(",",s.ParameterTypes)+")";
        public string GetGenericInstantiation(string t,ImmutableArray<string> a)=>t+"<"+string.Join(",",a)+">";
        public string GetGenericMethodParameter(object? c,int i)=>"!!"+i;
        public string GetGenericTypeParameter(object? c,int i)=>"!"+i;
        public string GetModifiedType(string m,string t,bool required)=>(required?"modreq":"modopt")+"("+m+")"+t;
        public string GetPinnedType(string e)=>e+" pinned";
        public string GetPointerType(string e)=>e+"*";
        public string GetPrimitiveType(PrimitiveTypeCode c)=>c.ToString();
        public string GetSZArrayType(string e)=>e+"[]";
        public string GetTypeFromDefinition(MetadataReader r,TypeDefinitionHandle h,byte k)=>Name(r,h);
        public string GetTypeFromReference(MetadataReader r,TypeReferenceHandle h,byte k)=>EntityName(r,h)+"[scope="+r.GetTypeReference(h).ResolutionScope.Kind+"]";
        public string GetTypeFromSpecification(MetadataReader r,object? c,TypeSpecificationHandle h,byte k)=>r.GetTypeSpecification(h).DecodeSignature(this,c);
    }
    private sealed class OwnMetadataTrap
    {
        public const int Code=7;
        static OwnMetadataTrap()=>throw new InvalidOperationException("METADATA_FIXTURE_MUST_NOT_RUN");
        public int Read(float value,ref int count)=>count;
    }
}
