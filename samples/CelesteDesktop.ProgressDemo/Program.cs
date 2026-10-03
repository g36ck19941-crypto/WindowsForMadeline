using System.Net;
using System.Text.Json;

try { return Run(args); }
catch (Exception ex)
{
    var detail = ex is ArgumentException or InvalidDataException ? ex.Message : "Cannot generate summary; inspect local inputs and permissions.";
    Console.Error.WriteLine($"PROGRESS_REPORT_FAILED phase=summary exceptionType={ex.GetType().Name} detail={detail}");
    return 2;
}

static int Run(string[] args)
{
    // This report reads names only, never exports cached source or commercial assets.
    var output = Path.GetFullPath("artifacts/current-progress-demo");
    var identityReportPath = Path.GetFullPath("artifacts/cdr-081-real/inventory.json");
    var compileReportPath = Path.GetFullPath("artifacts/cdr-082-real/summary.json");
    var specified = new HashSet<string>(StringComparer.Ordinal);
    for (var i = 0; i < args.Length; i++)
    {
        var key = args[i];
        if (!specified.Add(key) || (key != "--output" && key != "--identity-report" && key != "--compile-report") || ++i >= args.Length)
            throw new ArgumentException("Usage: --output <directory> [--identity-report <local report>] [--compile-report <local summary>]");
        if (key == "--output") output = Path.GetFullPath(args[i]);
        else if (key == "--identity-report") identityReportPath = Path.GetFullPath(args[i]);
        else compileReportPath = Path.GetFullPath(args[i]);
    }
    var root = Directory.GetCurrentDirectory();
    if (!File.Exists(Path.Combine(root, "CelesteDesktopRuntime.sln")))
        throw new InvalidOperationException("Run from the repository root.");
    var allowed = Path.GetFullPath(Path.Combine(root, "artifacts")) + Path.DirectorySeparatorChar;
    if (!output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Report output must be under repository artifacts.");
    if (!identityReportPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Identity report must be under repository artifacts.");
    if (!compileReportPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Compile report must be under repository artifacts.");
    RejectLinks(root, output);
    var cacheRoot = Path.Combine(root, "local-cache", "celeste-reference");
    var sourceCount = 0;
    var modNames = 0;
    var cacheCount = 0;
    if (Directory.Exists(cacheRoot))
    {
        RejectLinks(root, cacheRoot);
        foreach (var directory in Directory.EnumerateDirectories(cacheRoot))
        {
            RejectLinks(root, directory);
            var source = Path.Combine(directory, "source");
            if (!Directory.Exists(source)) continue;
            RejectLinks(root, source);
            cacheCount++;
            var pending = new Stack<string>();
            pending.Push(source);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                foreach (var child in Directory.EnumerateDirectories(current))
                {
                    RejectLinks(root, child);
                    pending.Push(child);
                }
                foreach (var file in Directory.EnumerateFiles(current, "*.cs"))
                {
                    RejectLinks(root, file);
                    if (++sourceCount > 50000) throw new InvalidOperationException("Cache inventory limit exceeded.");
                    var relative = Path.GetRelativePath(source, file);
                    if (relative.Contains("Celeste.Mod", StringComparison.OrdinalIgnoreCase) ||
                        relative.Contains("Everest", StringComparison.OrdinalIgnoreCase) ||
                        relative.Contains("MonoModRules", StringComparison.OrdinalIgnoreCase)) modNames++;
                }
            }
        }
    }
    var identitySummary = "尚未读取授权的程序集检查摘要；不会自动访问安装目录。";
    var identityRows = "";
    var dependencyExplanation = "尚无原版候选的 XNA 等依赖检查摘要；不能判断是否能编译或运行。";
    var originalMissingXnaCount = 0;
    var identityAvailable = File.Exists(identityReportPath);
    string? inspectedUtc = null;
    if (identityAvailable)
    {
        RejectLinks(root, identityReportPath);
        if (new FileInfo(identityReportPath).Length > 1024 * 1024) throw new IOException("Identity report limit exceeded.");
        using var document = JsonDocument.Parse(File.ReadAllText(identityReportPath));
        var report = document.RootElement;
        if (report.GetProperty("schemaVersion").GetInt32() != 1 || report.GetProperty("taskId").GetString() != "CDR-081" ||
            report.GetProperty("assemblyExecuted").GetBoolean() || report.GetProperty("gameLaunched").GetBoolean() ||
            report.GetProperty("installationWrites").GetInt32() != 0 || report.GetProperty("persistedCommercialBytes").GetInt32() != 0)
            throw new InvalidDataException("Identity summary safety contract failed.");
        inspectedUtc = report.GetProperty("inspectedUtc").GetString();
        if (!DateTimeOffset.TryParse(inspectedUtc, out _)) throw new InvalidDataException("Invalid inspection timestamp.");
        identitySummary = "下表是此前授权检查的摘要，时间：" + WebUtility.HtmlEncode(inspectedUtc) +
            "。本页面只读本地报告，没有再次访问游戏安装，也没有运行程序集。";
        var candidateSlots = new HashSet<string>(StringComparer.Ordinal);
        if (report.GetProperty("candidates").GetArrayLength() != 4) throw new InvalidDataException("Expected four identity slots.");
        foreach (var candidate in report.GetProperty("candidates").EnumerateArray())
        {
            var slot = candidate.GetProperty("slot").GetString();
            if (slot is not ("Celeste.dll" or "Celeste.exe" or "orig/Celeste.dll" or "orig/Celeste.exe") ||
                !candidateSlots.Add(slot))
                throw new InvalidDataException("Unexpected candidate slot.");
            var status = candidate.GetProperty("status").GetString();
            var classification = candidate.GetProperty("classification").GetString();
            var frameworkMeaning = candidate.GetProperty("targetFramework").GetString() switch
            {
                ".NETCoreApp,Version=v8.0" => ".NET 8（新框架）",
                ".NETFramework,Version=v4.5" => ".NET Framework 4.5（旧框架）",
                null => "不适用或未建立",
                _ => "其它框架，未校准"
            };
            var mods = candidate.GetProperty("modTypeCount").GetInt32();
            if (mods is < 0 or > 100000) throw new InvalidDataException("Invalid metadata count.");
            var meaning = status == "missing" ? "没有这个文件" : status == "native-pe" ? "启动外壳，不是托管角色代码" :
                classification == "mod-bearing" ? $"发现 Mod 标记（{mods} 个类型）；不是纯原版来源" :
                classification == "unmodified-candidate-not-authenticated" ? "未发现所检查的 Mod 标记；原版候选，尚无官方哈希认证" :
                "未建立身份，请查看检查错误";
            identityRows += $"<tr><td>{WebUtility.HtmlEncode(slot)}</td><td>{frameworkMeaning}</td><td>{WebUtility.HtmlEncode(meaning)}</td></tr>";
        }
        foreach (var node in report.GetProperty("dependencies").EnumerateArray())
        {
            if (node.GetProperty("slot").GetString() != "orig/Celeste.exe") continue;
            var missing = new HashSet<string>(StringComparer.Ordinal);
            foreach (var reference in node.GetProperty("references").EnumerateArray())
            {
                if (reference.GetProperty("status").GetString() != "missing-or-unreadable") continue;
                var name = reference.GetProperty("identity").GetProperty("name").GetString();
                if (name is "Microsoft.Xna.Framework" or "Microsoft.Xna.Framework.Graphics" or "Microsoft.Xna.Framework.Game")
                    missing.Add(name);
            }
            originalMissingXnaCount = missing.Count;
            dependencyExplanation = missing.Count > 0
                ? $"原版候选需要的 XNA 基础/绘图/游戏组件，本次在允许检查的目录内有 {missing.Count} 项未找到：" +
                  WebUtility.HtmlEncode(string.Join("、", missing.OrderBy(n => n))) +
                  "。这不代表整台电脑没装，也不能把 FNA 当成已验证兼容的替代品；后续编译必须先处理这个问题。"
                : "这份报告没有列出原版候选的 XNA 缺项，但依赖闭合和编译仍未证明，不能因此称为可以运行。";
        }
    }
    var compileExplanation = "尚无 CDR-082 本地恢复/编译摘要；不会自动恢复或编译。";
    var compileAvailable = false;
    var compileStatus = "uninspected";
    if (File.Exists(compileReportPath))
    {
        RejectLinks(root, compileReportPath);
        if (new FileInfo(compileReportPath).Length > 65536) throw new InvalidDataException("Compile summary limit exceeded.");
        using var compileDocument = JsonDocument.Parse(File.ReadAllText(compileReportPath));
        var probe = compileDocument.RootElement;
        if (probe.GetProperty("schemaVersion").GetInt32() != 1 || probe.GetProperty("taskId").GetString() != "CDR-082" ||
            probe.GetProperty("candidate").GetString() != "orig/Celeste.exe" ||
            probe.GetProperty("recoveredCodeExecuted").GetBoolean() || probe.GetProperty("gameLaunched").GetBoolean() ||
            probe.GetProperty("guiOpened").GetBoolean() || probe.GetProperty("installationWrites").GetInt32() != 0 ||
            probe.GetProperty("dependencyDownloads").GetInt32() != 0 || !probe.GetProperty("commercialMaterialLocalOnly").GetBoolean() ||
            probe.GetProperty("runtimeIntegrated").GetBoolean()) throw new InvalidDataException("Compile summary safety contract failed.");
        var recovered = probe.GetProperty("recoveredSourceFiles").GetInt32();
        var selected = probe.GetProperty("selectedCoreFiles").GetInt32();
        if (recovered is < 1 or > 10000 || selected is < 1 or > 10000 || selected > recovered)
            throw new InvalidDataException("Compile summary count invalid.");
        var compiled = probe.GetProperty("compileEstablished").GetBoolean();
        var attempted = probe.GetProperty("compileAttempted").GetBoolean();
        if (compiled && (!attempted || probe.GetProperty("compileExitCode").GetInt32() != 0))
            throw new InvalidDataException("Compile outcome inconsistent.");
        compileAvailable = true;
        compileStatus = compiled ? "compiled-not-executed" : attempted ? "compile-blocked" : "not-attempted";
        compileExplanation = $"CDR-082 已把选定的 orig/Celeste.exe 恢复成 {recovered} 个源码文件，全部仅存在本机忽略目录。" +
            $"编译探针只选取了含 Player 的 {selected} 个核心文件，没有运行反编译生成的工程。" +
            (compiled ? "本次编译通过，但没有运行，也不证明角色可以显示或移动。" :
             attempted ? "编译实际未通过：缺少依赖/相关类型。源码已恢复，不等于已拼成可运行角色。" : "尚未进入编译，前置离线准备失败。") +
            "旧的 .NET Framework 4.5 代码被试编译为 .NET 8，这是实验，不是已验证兼容；没有用 FNA 悄悄替代 XNA。";
    }
    var manifest = new
    {
        demoId = "CDR-081",
        source = "summary-only",
        cacheCount,
        sourceFileCount = sourceCount,
        modNamedFileCount = modNames,
        pristineVanillaEstablished = false,
        gameplayImplementationPresent = false,
        runtimeIntegrated = false,
        humanVisible = false,
        gameLaunched = false,
        installationWrites = 0,
        persistedCommercialBytes = 0,
        identityReportAvailable = identityAvailable,
        identityInspectedUtc = inspectedUtc,
        originalMissingXnaCount,
        compileReportAvailable = compileAvailable,
        compileStatus
    };
    var referenceExplanation = "尚无系统编译引用的只读检查摘要。";
    var referenceSummaryPath = Path.Combine(root, "artifacts", "cdr-082-reference-search", "summary.json");
    if (File.Exists(referenceSummaryPath))
    {
        RejectLinks(root, referenceSummaryPath);
        if (new FileInfo(referenceSummaryPath).Length > 65536) throw new InvalidDataException("Reference summary limit exceeded.");
        using var referenceDocument = JsonDocument.Parse(File.ReadAllText(referenceSummaryPath));
        var report = referenceDocument.RootElement;
        if (report.GetProperty("taskId").GetString() != "CDR-082" ||
            report.GetProperty("assemblyExecuted").GetBoolean() || report.GetProperty("gameLaunched").GetBoolean() ||
            report.GetProperty("guiOpened").GetBoolean() || report.GetProperty("installationWrites").GetInt32() != 0 ||
            report.GetProperty("dependencyDownloads").GetInt32() != 0 || report.GetProperty("compilationRetried").GetBoolean())
            throw new InvalidDataException("Reference summary safety contract failed.");
        var rows = report.GetProperty("referenceRows");
        if (rows.GetArrayLength() > 64) throw new InvalidDataException("Reference summary row limit exceeded.");
        var xnaMatches = rows.EnumerateArray().Count(row => row.GetProperty("scope").GetString() == "gac32-xna" &&
            row.GetProperty("exactOriginalReferenceMatch").GetBoolean());
        var frameworkMatches = rows.EnumerateArray().Count(row => row.GetProperty("scope").GetString() == "reference-framework-v4.7.2" &&
            row.GetProperty("exactOriginalReferenceMatch").GetBoolean());
        referenceExplanation = $"后续授权只读检查发现：32 位程序集缓存中 {xnaMatches} 个 XNA 组件身份匹配；" +
            $"旧框架 v4.7.2 中 {frameworkMatches} 个核心引用身份匹配。v4.0/v4.5 所查核心 DLL 未找到。" +
            "之前“安装目录内缺少”仍是当时正确的范围结论，不代表本机缺少。版本身份匹配不证明 API 或运行兼容；那次只读检查没有重新编译，后续编译另列在本页上方，角色没有运行。";
    }
    var closureExplanation = "尚未读取使用已有引用的本地编译摘要。";
    var closureSummaryPath = Path.Combine(root, "artifacts", "cdr-082-closure-real", "summary.json");
    if (File.Exists(closureSummaryPath))
    {
        RejectLinks(root, closureSummaryPath);
        if (new FileInfo(closureSummaryPath).Length > 65536) throw new InvalidDataException("Closure summary limit exceeded.");
        using var closureDocument = JsonDocument.Parse(File.ReadAllText(closureSummaryPath));
        var report = closureDocument.RootElement;
        if (report.GetProperty("taskId").GetString() != "CDR-082" || report.GetProperty("stage").GetString() != "xna-net472-original-closure" ||
            report.GetProperty("recoveredCodeExecuted").GetBoolean() || report.GetProperty("guiOpened").GetBoolean() ||
            report.GetProperty("gameLaunched").GetBoolean() || report.GetProperty("sourceEdited").GetBoolean() ||
            report.GetProperty("inventedStubs").GetBoolean() || report.GetProperty("runtimeIntegrated").GetBoolean() ||
            report.GetProperty("steamworksApiCalled").GetBoolean() || !report.GetProperty("steamworksMetadataOnly").GetBoolean() ||
            report.GetProperty("dependencyDownloads").GetInt32() != 0 || report.GetProperty("installationWrites").GetInt32() != 0 ||
            !report.GetProperty("commercialMaterialLocalOnly").GetBoolean()) throw new InvalidDataException("Closure summary safety contract failed.");
        var selected = report.GetProperty("selectedSourceFiles").GetInt32();
        if (selected is < 3 or > 10000) throw new InvalidDataException("Closure count invalid.");
        var errorCount = report.GetProperty("errorCounts").EnumerateObject().Sum(property => property.Value.GetInt32());
        if (errorCount < 0 || (report.GetProperty("emitSucceeded").GetBoolean() && errorCount != 0))
            throw new InvalidDataException("Closure emission outcome inconsistent.");
        closureExplanation = $"已经使用找到的 XNA 和旧框架引用，并在额外授权后加入 Steamworks.NET 编译元数据，把 Player/Actor/Solid 相关依赖扩展到 {selected} 个原版文件。" +
            (report.GetProperty("emitSucceeded").GetBoolean() ? "本次只编译生成成功，代码未运行。" : $"本次仍有 {errorCount} 条编译错误，没有生成成功的程序集。") +
            "不会自写替身来凑通过，没有改原版源码或执行反编译工程。文件数量来自保守依赖扫描，不保证最小；编译仍是旧框架兼容性实验。";
    }
    Directory.CreateDirectory(output);
    File.WriteAllText(Path.Combine(output, "manifest.json"),
        JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    var html = $$"""
<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>CDR-082 当前进度</title>
<style>body{background:#111827;color:#e5e7eb;font:20px/1.7 system-ui;max-width:1000px;margin:50px auto;padding:20px}
section{background:#1f2937;padding:20px;margin:20px 0;border-radius:12px}h1,h2{color:#67e8f9}
table{width:100%;border-collapse:collapse;font-size:17px}th,td{text-align:left;padding:10px;border-bottom:1px solid #475569}</style>
<h1>当前方向：本地反编译与原版逻辑重组</h1>
<section><h2>使用找到的零件，拼装到哪一步？</h2>{{closureExplanation}}</section>
<section><h2>编译零件找到了吗？</h2>{{referenceExplanation}}</section>
<section data-compile-status="{{compileStatus}}"><h2>历史：第一次少量文件编译探针</h2>{{compileExplanation}}
<p>简单说：已拿到拼装说明和零件清单，但还缺引擎零件，尚未拼成能工作的角色。
程序只编译、不运行；此页面不输出源码，不会再次读取安装。商业源码和派生构建永不上传。</p></section>
<section><h2>这次做了什么</h2>已删除自主实现的角色、八种交互实体、碰撞运动核及旧玩法编排。
旧的移动演示不再运行。保留资源读取、反编译工具、动画呈现、渲染与匿名桌面几何工具。
新增了只读程序集检查器，用来辨别“拿到的是哪个版本、需要哪些依赖”，不会运行游戏代码。</section>
<section><h2>CDR-081 历史：原版还是 Mod 的检查结果</h2>{{identitySummary}}
<table><thead><tr><th>检查位置</th><th>底层框架</th><th>说明</th></tr></thead><tbody>{{identityRows}}</tbody></table>
<p>{{dependencyExplanation}}</p>
<p>没有报告时表格为空，这是未检查，不是失败或成功。无 Mod 标记不等于官方认证的纯原版。
依赖报告会区分找到匹配版本、版本不符、缺少依赖和未检查的系统框架；不会把“文件在”当作“能运行”。</p></section>
<section><h2>本机现在有什么</h2>找到 {{cacheCount}} 份缓存，共 {{sourceCount}} 个 C# 文件。
其中 {{modNames}} 个文件名或目录带 Mod/Everest 标记。这是文件目录清点，不是运行或编译验证；
不能据此认定它们是纯原版。没有缓存时这里为零，报告仍然可以生成。</section>
<section><h2>这证明了什么</h2>旧行为已退役，工具层可以独立验证；新增探针能区分“源码恢复成功”与“编译通过”，不会把编译失败报成成功。本报告仅输出统计和说明，
不输出反编译源码、商业图片或安装路径。此前 CDR-016 的安装一致性验证是历史证据，未在这里重跑。</section>
<section><h2>仍未证明什么</h2>目前没有可运行的角色。原版依赖是否齐全、能否独立编译、
动画能否连接、角色是否能在桌面移动都尚未建立。反编译成功不等于游戏已经重组完成。</section>
<section><h2>接下来按什么顺序做</h2>确认原版/Mod 来源 → 找齐角色需要的引擎依赖 →
在仅本地的隔离目录尝试最小编译 → 接入时钟、输入、碰撞环境和资源读取 →
先逐步测试与离屏成帧，再另行授权桌面可见验证。不会为了“能编译”而偷偷编造替代玩法。</section>
<p>游戏未启动；未访问或写入安装目录；未接收实时输入；此页面不是角色演示。</p></html>
""";
    File.WriteAllText(Path.Combine(output, "index.html"), html);
    Console.WriteLine($"CDR081_REPORT cacheCount={cacheCount} sourceFiles={sourceCount} modNamedFiles={modNames} identityReportAvailable={identityAvailable} runtimeIntegrated=false");
    return 0;
}

static void RejectLinks(string root, string path)
{
    for (var current = Path.GetFullPath(path); current.Length >= root.Length;)
    {
        if ((Directory.Exists(current) || File.Exists(current)) &&
            (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Reparse points are not allowed in inventory/report paths.");
        var parent = Path.GetDirectoryName(current);
        if (parent is null || parent == current) break;
        current = parent;
    }
}
