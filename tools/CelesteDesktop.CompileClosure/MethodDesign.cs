using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Own text analysis only. No compilation, references, semantic model, emit or target execution.
internal static class MethodDesign
{
    private static readonly string[] Files = ["Celeste/Input.cs", "Celeste/Settings.cs", "Celeste/Player.cs",
        "Monocle/MInput.cs", "Monocle/Binding.cs", "Monocle/VirtualInput.cs", "Monocle/VirtualButton.cs",
        "Monocle/VirtualIntegerAxis.cs", "Monocle/VirtualJoystick.cs", "Monocle/Engine.cs", "Monocle/Scene.cs"];
    public static int Run(string sourceArgument, string outputArgument)
    {
        var checks = CheckOwnSyntax();
        var repo = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(repo,"CelesteDesktopRuntime.sln"))) throw new InvalidDataException("M2_REPO");
        var source = Path.GetFullPath(sourceArgument); var output = Path.GetFullPath(outputArgument);
        Inside(Path.Combine(repo,"local-cache","cdr-082"),source);
        Inside(Path.Combine(repo,"local-cache","cdr-082-method-design"),output);
        if (Path.GetFileName(source)!="source" || Directory.Exists(output) || File.Exists(output)) throw new InvalidDataException("M2_FRESH_OUTPUT");
        var manifest=Path.Combine(Path.GetDirectoryName(source)!,"summary.json"); NoLinks(manifest);
        if(new FileInfo(manifest).Length>65536)throw new InvalidDataException("M2_MANIFEST_BUDGET");
        using(var doc=JsonDocument.Parse(File.ReadAllText(manifest)))
            if(doc.RootElement.GetProperty("assemblySha256").GetString()!="1A1E117ADD967C0F26AD470A49D4FF442435209265BF1FDDA623821D797E80B5" ||
                doc.RootElement.GetProperty("recoveredSourceFiles").GetInt32()!=918 || doc.RootElement.GetProperty("recoveredCodeExecuted").GetBoolean())
                throw new InvalidDataException("M2_BASELINE");
        var records=new List<object>(); var hashes=new SortedDictionary<string,string>(StringComparer.Ordinal);
        int methods=0, properties=0, initializers=0, calls=0, deferred=0; long bytes=0;
        foreach(var relative in Files)
        {
            var path=Path.Combine(source,relative); NoLinks(path);
            var size=new FileInfo(path).Length; bytes+=size;
            if(size>4*1024*1024 || bytes>16*1024*1024)throw new InvalidDataException("M2_SOURCE_BUDGET");
            var raw=File.ReadAllBytes(path); hashes.Add(relative,Convert.ToHexString(SHA256.HashData(raw)));
            var tree=CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(raw));
            if(tree.GetDiagnostics().Any(d=>d.Severity==DiagnosticSeverity.Error))throw new InvalidDataException("M2_SYNTAX");
            foreach(var node in Members(tree.GetRoot()))
            {
                var kind=node switch {BaseMethodDeclarationSyntax=>"method-or-constructor",PropertyDeclarationSyntax=>"property",_=>"field-initializer"};
                if(kind=="method-or-constructor")methods++; else if(kind=="property")properties++; else initializers++;
                var owner=string.Join(".",node.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(t=>t.Identifier.ValueText));
                var signature=node switch {
                    MethodDeclarationSyntax m=>m.ReturnType+" "+m.Identifier.ValueText+m.TypeParameterList+m.ParameterList,
                    ConstructorDeclarationSyntax c=>c.Modifiers+" "+c.Identifier.ValueText+c.ParameterList,
                    OperatorDeclarationSyntax o=>o.ReturnType+" operator "+o.OperatorToken+o.ParameterList,
                    ConversionOperatorDeclarationSyntax c=>c.ImplicitOrExplicitKeyword+" operator "+c.Type+c.ParameterList,
                    DestructorDeclarationSyntax d=>"~"+d.Identifier.ValueText+d.ParameterList,
                    PropertyDeclarationSyntax p=>p.Type+" "+p.Identifier.ValueText,
                    VariableDeclaratorSyntax v=>v.Identifier.ValueText,
                    _=>node.Kind().ToString()};
                var sites=node.DescendantNodes().Where(n=>n is InvocationExpressionSyntax or ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax)
                    .Select(n=>new {line=Line(n),kind=n.Kind().ToString(),expression=n switch {
                        InvocationExpressionSyntax c=>c.Expression.ToString(),ObjectCreationExpressionSyntax c=>c.Type.ToString(),_=>"implicit-new-unresolved"},
                        argumentCount=n switch {InvocationExpressionSyntax c=>c.ArgumentList.Arguments.Count,ObjectCreationExpressionSyntax c=>c.ArgumentList?.Arguments.Count??0,_=>0},
                        deferred=n.Ancestors().TakeWhile(a=>a!=node).Any(a=>a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax),
                        calleeResolved=false}).ToArray();
                calls+=sites.Length; deferred+=sites.Count(s=>s.deferred);
                var assignments=node.DescendantNodes().OfType<AssignmentExpressionSyntax>().Select(a=>new{line=Line(a),target=a.Left.ToString(),operation=a.Kind().ToString()}).ToArray();
                var references=node.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Select(a=>new{line=Line(a),expression=a.ToString()}).ToArray();
                var ctor=node as ConstructorDeclarationSyntax;
                records.Add(new{file=relative,owner,signature,kind,startLine=Line(node),endLine=node.GetLocation().GetLineSpan().EndLinePosition.Line+1,
                    constructorInitializer=ctor?.Initializer?.ToString(),sites,assignments,references});
            }
        }
        foreach(var entry in hashes){NoLinks(Path.Combine(source,entry.Key));if(Hash(Path.Combine(source,entry.Key))!=entry.Value)throw new InvalidDataException("M2_SOURCE_CHANGED");}
        if(AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name is "Celeste" or "OriginalCoreCompileProbe" or "Steamworks.NET" || a.GetName().Name?.StartsWith("Microsoft.Xna.Framework",StringComparison.Ordinal)==true))
            throw new InvalidDataException("M2_TARGET_LOADED");
        var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",hashes.Select(p=>p.Key+":"+p.Value)))));
        var options=new JsonSerializerOptions{WriteIndented=true};
        var details=JsonSerializer.Serialize(new{schemaVersion=1,method="exact-syntax-locations-not-semantic-binding",files=hashes,records},options);
        if(Encoding.UTF8.GetByteCount(details)>32*1024*1024)throw new InvalidDataException("M2_DETAILS_BUDGET");
        NoLinks(output);Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output,"methods-local-only.json"),details);
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{
            schemaVersion=1,taskId="CDR-082-M2",stage="method-boundary-design",inspectedUtc=DateTimeOffset.UtcNow,
            selectedFiles=Files.Length,methods,properties,initializers,callSites=calls,deferredCallSites=deferred,sourceDigest=digest,
            sourceHashesStable=true,ownSyntaxChecks=checks,semanticBindingEstablished=false,transitiveCallGraphEstablished=false,
            modernCompatibilityEstablished=false,sourceEdited=false,originalCompiled=false,originalExecuted=false,
            targetAssemblyLoaded=false,newDependencyRead=false,assetsRead=false,probeRun=false,guiOpened=false,installationAccessed=false,publicSourceBytes=0},options));
        Console.WriteLine($"METHOD_DESIGN_COMPLETED selectedFiles={Files.Length} methods={methods} properties={properties} initializers={initializers} callSites={calls} deferredCallSites={deferred} sourceHashesStable=true semanticBindingEstablished=false originalExecuted=false");
        return 0;
    }
    private static IEnumerable<SyntaxNode> Members(SyntaxNode root)=>root.DescendantNodes().Where(n=>n is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax ||
        n is VariableDeclaratorSyntax v && v.Initializer!=null && v.Parent?.Parent is FieldDeclarationSyntax);
    private static int CheckOwnSyntax()
    {
        var root=CSharpSyntaxTree.ParseText("class Own { static int f=Make(); static Own():base(){} int P=>Read(); void M(int x) { System.Action a=()=>Late(); Now(); } void M(){} }").GetRoot();
        void Check(bool ok){if(!ok)throw new InvalidDataException("M2_OWN_SYNTAX");}
        var nodes=Members(root).ToArray();
        Check(nodes.Length==5);Check(nodes.OfType<MethodDeclarationSyntax>().Count()==2);
        Check(nodes.OfType<PropertyDeclarationSyntax>().Count()==1);Check(nodes.OfType<VariableDeclaratorSyntax>().Count()==1);
        Check(nodes.OfType<ConstructorDeclarationSyntax>().Single().Initializer!=null);
        Check(root.DescendantNodes().OfType<InvocationExpressionSyntax>().Count()==4);
        Check(root.DescendantNodes().OfType<InvocationExpressionSyntax>().Count(c=>c.Ancestors().Any(a=>a is AnonymousFunctionExpressionSyntax))==1);
        Check(nodes.All(n=>Line(n)==1));return 8;
    }
    private static int Line(SyntaxNode n)=>n.GetLocation().GetLineSpan().StartLinePosition.Line+1;
    private static void Inside(string root,string value){if(!value.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("M2_PATH_SCOPE");NoLinks(value);}
    private static void NoLinks(string value){for(string? p=Path.GetFullPath(value);p!=null;p=Path.GetDirectoryName(p))if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("M2_LINK");}
    private static string Hash(string path){using var stream=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(stream));}
}
