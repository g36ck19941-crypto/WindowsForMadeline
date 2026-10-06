using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Syntax-only inventory. No compilation, metadata references, emit or target loading.
internal static class MigrationInventory
{
    private const string Baseline = "1A1E117ADD967C0F26AD470A49D4FF442435209265BF1FDDA623821D797E80B5";
    public static int Run(string sourceArgument, string outputArgument)
    {
        var ownSyntaxChecks=VerifySyntax();
        var repo = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(repo, "CelesteDesktopRuntime.sln"))) throw new InvalidDataException("M1_REPO_REQUIRED");
        var source = Path.GetFullPath(sourceArgument); var output = Path.GetFullPath(outputArgument);
        Inside(Path.Combine(repo,"local-cache","cdr-082"), source);
        Inside(Path.Combine(repo,"local-cache","cdr-082-migration-inventory"), output);
        if (Path.GetFileName(source) != "source" || Directory.Exists(output) || File.Exists(output)) throw new InvalidDataException("M1_FRESH_OUTPUT_SOURCE_REQUIRED");
        var manifest = Path.Combine(Path.GetDirectoryName(source)!, "summary.json"); NoLinks(manifest);
        if (new FileInfo(manifest).Length > 65536) throw new InvalidDataException("M1_MANIFEST_BUDGET");
        using (var doc=JsonDocument.Parse(File.ReadAllText(manifest)))
            if (doc.RootElement.GetProperty("assemblySha256").GetString()!=Baseline || doc.RootElement.GetProperty("recoveredSourceFiles").GetInt32()!=918 ||
                doc.RootElement.GetProperty("recoveredCodeExecuted").GetBoolean()) throw new InvalidDataException("M1_ORIGINAL_CACHE_BASELINE");
        var rows=new SortedDictionary<string,SourceRow>(StringComparer.Ordinal);
        var pending=new Stack<string>(); pending.Push(source); long bytes=0;
        while(pending.TryPop(out var directory))
        {
            NoLinks(directory);
            foreach(var child in Directory.EnumerateDirectories(directory).Order(StringComparer.Ordinal)) {NoLinks(child);pending.Push(child);}
            foreach(var path in Directory.EnumerateFiles(directory,"*.cs").Order(StringComparer.Ordinal))
            {
                NoLinks(path); var length=new FileInfo(path).Length; bytes+=length;
                if(length>4*1024*1024 || bytes>64*1024*1024 || rows.Count>=10000) throw new InvalidDataException("M1_SOURCE_BUDGET");
                var raw=File.ReadAllBytes(path); var hash=Convert.ToHexString(SHA256.HashData(raw));
                var tree=CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(raw));
                var syntax=tree.GetRoot();
                var declarations=syntax.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Select(t=>new TypeRow(t.Identifier.ValueText,
                    t.Kind().ToString(),Line(t), t.BaseList?.Types.Select(b=>b.Type.ToString()).ToArray() ?? []))
                    .Concat(syntax.DescendantNodes().OfType<DelegateDeclarationSyntax>().Select(t=>new TypeRow(t.Identifier.ValueText,t.Kind().ToString(),Line(t),[]))).ToArray();
                // Deliberately overinclusive: covers generic/delegate/alias tokens without semantic resolution.
                var tokens=syntax.DescendantTokens().Where(t=>t.IsKind(SyntaxKind.IdentifierToken)).Select(t=>t.ValueText).Distinct().Order().ToArray();
                var uses=syntax.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u=>new UseRow(u.Name?.ToString()??"",u.Alias?.Name.Identifier.ValueText,Line(u))).ToArray();
                var edges=new List<CallRow>();
                foreach(var node in syntax.DescendantNodes())
                {
                    SyntaxNode? target=node switch {InvocationExpressionSyntax call=>call.Expression,ObjectCreationExpressionSyntax creation=>creation.Type,_=>null};
                    if(target is null) continue;
                    var names=target.DescendantTokens().Where(t=>t.IsKind(SyntaxKind.IdentifierToken)).Select(t=>t.ValueText).ToArray();
                    var context=node.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
                    var owner=node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText;
                    var contextName=context switch {MethodDeclarationSyntax m=>m.Identifier.ValueText,ConstructorDeclarationSyntax c=>c.Identifier.ValueText,_=>null};
                    var property=node.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                    var field=node.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
                    var origin=context is ConstructorDeclarationSyntax ctor && ctor.Modifiers.Any(SyntaxKind.StaticKeyword) ? "static-constructor" :
                        context is not null ? "method-or-constructor" : field is not null && node.Ancestors().OfType<EqualsValueClauseSyntax>().Any() ?
                        field.Modifiers.Any(SyntaxKind.StaticKeyword)?"static-field-initializer":"instance-field-initializer" : property is not null?"property-or-initializer":"other";
                    edges.Add(new CallRow(node is InvocationExpressionSyntax?"invocation":"construction",Line(node),owner,contextName??property?.Identifier.ValueText,
                        origin,string.Join(".",names),Categories(names)));
                }
                var initializers=syntax.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Where(c=>c.Modifiers.Any(SyntaxKind.StaticKeyword)).Select(c=>Line(c)).ToArray();
                var interop=syntax.DescendantNodes().OfType<AttributeSyntax>().Where(a=>a.Name.ToString().EndsWith("DllImport",StringComparison.Ordinal)||a.Name.ToString().EndsWith("DllImportAttribute",StringComparison.Ordinal)).Select(a=>Line(a)).ToArray();
                rows.Add(Path.GetRelativePath(source,path).Replace('\\','/'),new SourceRow(hash,length,declarations,uses,tokens,edges.ToArray(),initializers,interop,
                    tree.GetDiagnostics().Count(d=>d.Severity==DiagnosticSeverity.Error)));
            }
        }
        if(rows.Count!=918) throw new InvalidDataException("M1_CACHE_FILE_COUNT_CHANGED");
        var typeIndex=rows.SelectMany(p=>p.Value.Types.Select(t=>(Name:t.Name,File:p.Key))).GroupBy(t=>t.Name,StringComparer.Ordinal)
            .ToDictionary(g=>g.Key,g=>g.Select(x=>x.File).Distinct().Order().ToArray(),StringComparer.Ordinal);
        var dependencies=rows.ToDictionary(p=>p.Key,p=>p.Value.Identifiers.Where(typeIndex.ContainsKey).SelectMany(n=>typeIndex[n])
            .Where(file=>file!=p.Key).Distinct().Order().ToArray(),StringComparer.Ordinal);
        var coreSeeds=new[]{"Celeste/Player.cs","Celeste/Actor.cs","Celeste/Solid.cs"};
        if(coreSeeds.Any(s=>!rows.ContainsKey(s))) throw new InvalidDataException("M1_CORE_SEED_MISSING");
        var inputNames=new[]{"Input","MInput","Binding","VirtualButton","VirtualIntegerAxis","Engine","Scene","Level"};
        if(inputNames.Any(n=>!typeIndex.ContainsKey(n))) throw new InvalidDataException("M1_INPUT_SEED_MISSING");
        var inputSeeds=inputNames.SelectMany(n=>typeIndex[n]).Distinct().Order().ToArray();
        var core=Close(dependencies,coreSeeds); var input=Close(dependencies,inputSeeds);
        var categories=new[]{"input-device","audio","steam","file","graphics-window","process-thread","numeric-type"};
        var counts=categories.ToDictionary(c=>c,c=>rows.Values.Sum(r=>r.Calls.Count(call=>call.Categories.Contains(c))));
        var xnaUsingFiles=rows.Count(p=>p.Value.Usings.Any(u=>u.Name.StartsWith("Microsoft.Xna.Framework",StringComparison.Ordinal)));
        var staticConstructors=rows.Values.Sum(r=>r.StaticConstructors.Length);
        var initializerCalls=rows.Values.Sum(r=>r.Calls.Count(c=>c.Context is "static-constructor" or "static-field-initializer"));
        var syntaxErrors=rows.Values.Sum(r=>r.SyntaxErrors);
        // Verify every input remains identical before publishing any result. Never write to source.
        foreach(var row in rows)
            if(Hash(Path.Combine(source,row.Key))!=row.Value.Hash) throw new InvalidDataException("M1_SOURCE_CHANGED_DURING_READ");
        var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",rows.Select(r=>r.Key+":"+r.Value.Hash)))));
        var coreDigest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",core.Order().Select(key=>key+":"+rows[key].Hash)))));
        if(AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name is "Celeste" or "OriginalCoreCompileProbe" or "Steamworks.NET" ||
            a.GetName().Name?.StartsWith("Microsoft.Xna.Framework",StringComparison.Ordinal)==true)) throw new InvalidDataException("M1_TARGET_LOADED");
        Directory.CreateDirectory(output); NoLinks(output);
        var options=new JsonSerializerOptions{WriteIndented=true};
        var privateDetails=JsonSerializer.Serialize(new {files=rows,dependencies,coreSeeds,inputSeeds,
            coreClosure=core.Order().ToArray(),inputClosure=input.Order().ToArray(),ambiguousTypeNames=typeIndex.Where(p=>p.Value.Length>1).ToDictionary(p=>p.Key,p=>p.Value)},options);
        if(Encoding.UTF8.GetByteCount(privateDetails)>32*1024*1024)throw new InvalidDataException("M1_DETAILS_BUDGET");
        File.WriteAllText(Path.Combine(output,"details-local-only.json"),privateDetails);
        var summary=new {schemaVersion=1,taskId="CDR-082-M1",stage="syntax-only-migration-inventory",inspectedUtc=DateTimeOffset.UtcNow,
            sourceFiles=rows.Count,totalSourceBytes=bytes,sourceDigest=digest,sourceHashesStable=true,ownSyntaxChecks,declaredTypeOccurrences=rows.Values.Sum(r=>r.Types.Length),
            ambiguousTypeNameCount=typeIndex.Count(p=>p.Value.Length>1),dependencyFileEdges=dependencies.Sum(p=>p.Value.Length),
            coreSeedFiles=coreSeeds.Length,coreCandidateFiles=core.Count,selectedCoreSourceDigest=coreDigest,
            selectedCoreDigestMatchesPriorClosure=coreDigest=="89EF35562F630A23B2663E15C96678F1326521AD43B0A2D23156029E1A5FFC51",inputSeedFiles=inputSeeds.Length,inputCandidateFiles=input.Count,
            syntaxErrorCount=syntaxErrors,xnaUsingFiles,staticConstructorOccurrences=staticConstructors,staticInitializerCallOccurrences=initializerCalls,
            dllImportAttributeOccurrences=rows.Values.Sum(r=>r.InteropAttributes.Length),platformCandidateOccurrences=counts,
            unclassifiedCallOccurrences=rows.Values.Sum(r=>r.Calls.Count(c=>c.Categories.Length==0)),
            dependencyMethod="conservative-simple-identifier-file-edges",callMethod="syntax-token-category-candidates-not-bound-callees",
            transitiveCallGraphEstablished=false,minimalityEstablished=false,semanticBindingEstablished=false,modernCompatibilityEstablished=false,
            sourceEdited=false,originalCompiled=false,originalExecuted=false,targetAssemblyLoaded=false,newDependencyRead=false,assetsRead=false,
            probeRun=false,guiOpened=false,installationAccessed=false,publicSourceBytes=0};
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(summary,options));
        Console.WriteLine(JsonSerializer.Serialize(new{eventId="MIGRATION_INVENTORY_COMPLETED",sourceFiles=rows.Count,coreCandidateFiles=core.Count,inputCandidateFiles=input.Count,
            syntaxErrorCount=syntaxErrors,sourceHashesStable=true,originalCompiled=false,originalExecuted=false,semanticBindingEstablished=false}));
        return 0;
    }
    private static int VerifySyntax()
    {
        void Check(bool value){if(!value)throw new InvalidDataException("M1_OWN_SYNTAX_CHECK");}
        Check(Categories(new[]{"Keyboard","GetState"}).SequenceEqual(new[]{"input-device"}));
        Check(Categories(new[]{"Audio","Play"}).SequenceEqual(new[]{"audio"}));
        Check(Categories(new[]{"SteamAPI","Init"}).SequenceEqual(new[]{"steam"}));
        Check(Categories(new[]{"Directory","CreateDirectory"}).SequenceEqual(new[]{"file"}));
        Check(Categories(new[]{"Texture2D","FromStream"}).SequenceEqual(new[]{"graphics-window"}));
        Check(Categories(new[]{"OwnedUnknown","Step"}).Length==0);
        var syntax=CSharpSyntaxTree.ParseText("class OwnA<T>{static OwnA(){} static OwnB field=new OwnB();} class OwnB{} delegate void OwnD();").GetRoot();
        Check(syntax.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Count(c=>c.Modifiers.Any(SyntaxKind.StaticKeyword))==1);
        Check(syntax.DescendantNodes().OfType<DelegateDeclarationSyntax>().Count()==1);
        var graph=new Dictionary<string,string[]>{{"a",new[]{"b"}},{"b",new[]{"a","c"}},{"c",Array.Empty<string>()}};
        Check(Close(graph,new[]{"a"}).SetEquals(new[]{"a","b","c"}));
        Check(syntax.DescendantTokens().Any(t=>t.IsKind(SyntaxKind.IdentifierToken)&&t.ValueText=="T"));
        return 10;
    }
    private static int Line(SyntaxNode node)=>node.GetLocation().GetLineSpan().StartLinePosition.Line+1;
    private static string[] Categories(string[] names)
    {
        var set=names.ToHashSet(StringComparer.Ordinal); var found=new List<string>();
        if(set.Overlaps(new[]{"Keyboard","Mouse","GamePad","TouchPanel"}))found.Add("input-device");
        if(set.Overlaps(new[]{"Audio","FMOD","SoundEffect","SoundEffectInstance"}))found.Add("audio");
        if(names.Any(n=>n.StartsWith("Steam",StringComparison.Ordinal)))found.Add("steam");
        if(set.Overlaps(new[]{"File","Directory","FileStream","StreamReader","StreamWriter","XmlDocument"}))found.Add("file");
        if(set.Overlaps(new[]{"GraphicsDevice","GraphicsDeviceManager","SpriteBatch","Texture2D","RenderTarget2D","Window","GameWindow"}))found.Add("graphics-window");
        if(set.Overlaps(new[]{"Process","Thread","Task"}))found.Add("process-thread");
        if(set.Overlaps(new[]{"Vector2","Vector3","Matrix","Rectangle","Color","MathHelper"}))found.Add("numeric-type");
        return found.ToArray();
    }
    private static HashSet<string> Close(Dictionary<string,string[]> graph,IEnumerable<string> seeds)
    {var selected=new HashSet<string>(StringComparer.Ordinal);var queue=new Queue<string>(seeds);while(queue.TryDequeue(out var name)){if(!selected.Add(name))continue;foreach(var next in graph[name])queue.Enqueue(next);}return selected;}
    private static void Inside(string root,string value){var prefix=Path.GetFullPath(root)+Path.DirectorySeparatorChar;if(!value.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("M1_PATH_SCOPE");NoLinks(value);}
    private static void NoLinks(string value){for(string? path=Path.GetFullPath(value);path is not null;path=Path.GetDirectoryName(path))if((File.Exists(path)||Directory.Exists(path))&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("M1_LINK");}
    private static string Hash(string path){NoLinks(path);using var stream=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(stream));}
    private sealed record TypeRow(string Name,string Kind,int Line,string[] Bases);
    private sealed record UseRow(string Name,string? Alias,int Line);
    private sealed record CallRow(string Kind,int Line,string? Owner,string? Member,string Context,string TargetTokens,string[] Categories);
    private sealed record SourceRow(string Hash,long Bytes,TypeRow[] Types,UseRow[] Usings,string[] Identifiers,CallRow[] Calls,int[] StaticConstructors,int[] InteropAttributes,int SyntaxErrors);
}
