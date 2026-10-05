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
    var isolationSummaryPath = Path.GetFullPath("artifacts/cdr-082-isolation/summary.json");
    var frameworkSummaryPath = Path.GetFullPath("artifacts/cdr-082-isolation-framework/summary.json");
    var restrictedSummaryPath = Path.GetFullPath("artifacts/cdr-082-restricted-process/summary.json");
    var specified = new HashSet<string>(StringComparer.Ordinal);
    for (var i = 0; i < args.Length; i++)
    {
        var key = args[i];
        if (!specified.Add(key) || (key != "--output" && key != "--identity-report" && key != "--compile-report" && key != "--isolation-report" && key != "--framework-report" && key != "--restricted-report") || ++i >= args.Length)
            throw new ArgumentException("Usage: --output <directory> [--identity-report <local report>] [--compile-report <local summary>]");
        if (key == "--output") output = Path.GetFullPath(args[i]);
        else if (key == "--identity-report") identityReportPath = Path.GetFullPath(args[i]);
        else if (key == "--isolation-report") isolationSummaryPath = Path.GetFullPath(args[i]);
        else if (key == "--framework-report") frameworkSummaryPath = Path.GetFullPath(args[i]);
        else if (key == "--restricted-report") restrictedSummaryPath = Path.GetFullPath(args[i]);
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
    var isolationExplanation = "尚未生成隔离适配的测试摘要；这不是通过或失败。";
    var isolationStatus = "not-inspected";
    if (!isolationSummaryPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Isolation summary must be under repository artifacts.");
    if (File.Exists(isolationSummaryPath))
    {
        if (new FileInfo(isolationSummaryPath).Length > 16384) throw new InvalidDataException("Isolation summary budget exceeded.");
        using var isolationDocument = JsonDocument.Parse(File.ReadAllText(isolationSummaryPath));
        var isolation = isolationDocument.RootElement;
        if (isolation.GetProperty("schemaVersion").GetInt32() != 1 ||
            isolation.GetProperty("taskId").GetString() != "CDR-082" ||
            isolation.GetProperty("stage").GetString() != "managed-isolation-adapter" ||
            isolation.GetProperty("checksPassed").GetInt32() != 45 || isolation.GetProperty("checksFailed").GetInt32() != 0 ||
            isolation.GetProperty("fixedHz").GetInt32() != 60 || isolation.GetProperty("deniedServiceCount").GetInt32() != 8 ||
            isolation.GetProperty("demoFrames").GetInt32() != 5 || isolation.GetProperty("installationWrites").GetInt32() != 0)
            throw new InvalidDataException("Isolation summary result invalid.");
        foreach (var flag in new[] { "originalBound", "recoveredCodeExecuted", "originalSourceModified", "newAssetsRead",
            "guiOpened", "steamApiCalled", "gameLaunched", "processSandboxEstablished", "originalFrameworkBridgeEstablished" })
            if (isolation.GetProperty(flag).GetBoolean()) throw new InvalidDataException("Isolation summary scope invalid.");
        isolationExplanation = "我们新写的外围适配已通过45项合成检查：每一步固定1/60秒，只吃程序预设输入，能区分按下、持续和松开。" +
            "Steam、音频、窗口、绘图设备、真实输入、素材读取、文件写入和原版执行这8类请求全部明确拒绝。" +
            "双击“验证本地隔离适配.cmd”可看到5步输入时间记录及拒绝结果，不会打开窗口或读取安装。" +
            "它还没有接入原版静态时间/输入/场景，不是角色动画，不是系统安全沙箱；net8工具通过也不证明原版旧框架桥接通过。";
        isolationStatus = "managed-only-original-unbound";
    }
    var frameworkExplanation = "尚未进行隔离模块的旧框架编译检查，不是兼容成功。";
    var frameworkStatus = "not-inspected";
    if (!frameworkSummaryPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Framework summary must be under repository artifacts.");
    if (File.Exists(frameworkSummaryPath))
    {
        if (new FileInfo(frameworkSummaryPath).Length > 16384) throw new InvalidDataException("Framework summary budget exceeded.");
        using var document = JsonDocument.Parse(File.ReadAllText(frameworkSummaryPath));
        var report = document.RootElement;
        if (report.GetProperty("taskId").GetString() != "CDR-082" || report.GetProperty("stage").GetString() != "own-adapter-net472-compile" ||
            !report.GetProperty("compileSucceeded").GetBoolean() || !report.GetProperty("metadataAuditPassed").GetBoolean() ||
            report.GetProperty("targetFramework").GetString() != ".NETFramework,Version=v4.7.2" || report.GetProperty("sourceFiles").GetInt32() != 2 ||
            report.GetProperty("assemblyReferenceCount").GetInt32() != 1 || report.GetProperty("embeddedResources").GetInt32() != 0 ||
            report.GetProperty("pinvokeMethods").GetInt32() != 0 || report.GetProperty("installationWrites").GetInt32() != 0 || report.GetProperty("downloads").GetInt32() != 0)
            throw new InvalidDataException("Framework summary outcome invalid.");
        foreach (var flag in new[] { "originalBound", "originalSourceModified", "recoveredCodeExecuted", "net472OutputExecuted", "newAssetsRead", "guiOpened", "runtimeCompatibilityEstablished" })
            if (report.GetProperty(flag).GetBoolean()) throw new InvalidDataException("Framework summary exceeds scope.");
        frameworkStatus = "compiled-only-not-run";
        frameworkExplanation = "我们的隔离模块现在也能编译成原版恢复库所用的4.7.2旧框架库。静态检查只有基础库引用，没有嵌入素材或原生调用导入。" +
            "这解决的是编译接口兼容准备，不是运行兼容或原版接入。生成库没有运行，也没有修改原版源码。" +
            "可用“检查隔离适配旧框架编译.cmd”复核，只读取已授权系统引用，不读取游戏安装。";
    }
    var environmentStatus = "not-inspected";
    var environmentExplanation = "尚无受限环境只读核对摘要；不能因此判断本机支持或不支持隔离。生成本页不会自动检查系统。";
    var environmentSummaryPath = Path.Combine(root, "artifacts", "cdr-082-environment", "summary.json");
    if (File.Exists(environmentSummaryPath))
    {
        RejectLinks(root, environmentSummaryPath);
        if (new FileInfo(environmentSummaryPath).Length > 16384) throw new InvalidDataException("Environment summary budget exceeded.");
        using var environmentDocument = JsonDocument.Parse(File.ReadAllText(environmentSummaryPath));
        var report = environmentDocument.RootElement;
        if (report.GetProperty("schemaVersion").GetInt32() != 1 || report.GetProperty("taskId").GetString() != "CDR-082" ||
            report.GetProperty("stage").GetString() != "restricted-environment-readonly-inventory" ||
            report.GetProperty("registryKeysInspected").GetInt32() != 3 || report.GetProperty("fileSlotsInspected").GetInt32() != 8 ||
            report.GetProperty("fixedFiles").GetArrayLength() != 8 || report.GetProperty("framework").GetArrayLength() != 2 ||
            report.GetProperty("downloads").GetInt32() != 0)
            throw new InvalidDataException("Environment summary invalid.");
        foreach (var flag in new[] { "targetLoaded", "xnaLoaded", "gameDirectoryAccessed", "guiOpened", "systemConfigurationChanged",
            "isolationEstablished", "originalRuntimeCompatibilityEstablished" })
            if (report.GetProperty(flag).GetBoolean()) throw new InvalidDataException("Environment summary exceeds scope.");
        var os = report.GetProperty("windows");
        var build = os.GetProperty("build").GetInt32();
        if (build is < 1 or > 1000000) throw new InvalidDataException("Environment build invalid.");
        var present = report.GetProperty("fixedFiles").EnumerateArray().Count(row => row.GetProperty("present").GetBoolean());
        var releases = report.GetProperty("framework").EnumerateArray().Select(row => row.GetProperty("release").GetInt32()).ToArray();
        if (releases.Any(release => release is < 0 or > 10000000)) throw new InvalidDataException("Environment release invalid.");
        environmentStatus = "inventory-only-enforcement-unknown";
        environmentExplanation = $"已有只读核对记录：系统构建号{build}，系统为{(os.GetProperty("is64Bit").GetBoolean() ? "64" : "32")}位；" +
            $"32/64位框架安装记录Release分别为{releases[0]}/{releases[1]}，8个固定文件位置中找到{present}个。" +
            "这说明哪些基础文件和安装记录存在，不是限制已经生效的测试。未创建隔离身份、修改权限或启动受限子进程。" +
            "文件、网络、GUI、真实输入、音频、Steam、子进程限制及混合原生XNA兼容性仍需分别验证；未知项不会自动放行。" +
            "可用“检查受限环境可行性.cmd”重新生成只读摘要；本页只展示已有结果，不代替执行检查。";
    }
    var restrictedStatus = "not-inspected";
    var startupDiagnosticsStatus = "not-recorded";
    var restrictedExplanation = "尚无自有受限进程原型摘要；不能认为启动限制或退出清理已经通过。生成本页不会启动测试进程。";
    if (!restrictedSummaryPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Restricted summary must be under repository artifacts.");
    if (File.Exists(restrictedSummaryPath))
    {
        RejectLinks(root, restrictedSummaryPath);
        if (new FileInfo(restrictedSummaryPath).Length > 16384) throw new InvalidDataException("Restricted summary budget exceeded.");
        using var restrictedDocument = JsonDocument.Parse(File.ReadAllText(restrictedSummaryPath));
        var report = restrictedDocument.RootElement;
        if (report.GetProperty("schemaVersion").GetInt32() != 1 || report.GetProperty("taskId").GetString() != "CDR-082" ||
            report.GetProperty("stage").GetString() != "own-restricted-process-prototype" ||
            report.GetProperty("nativeControlPassed").GetInt32() != 6 || !report.GetProperty("allObservedOwnedChildrenExited").GetBoolean() ||
            !report.GetProperty("guiPolicyQueriedBeforeResume").GetBoolean() || report.GetProperty("nativeImportModules").GetInt32() != 1 ||
            report.GetProperty("nativeImportMethods").GetInt32() != 11 || report.GetProperty("activeProcessLimit").GetInt32() != 1 ||
            report.GetProperty("commitMemoryLimitBytes").GetInt32() != 536870912 || report.GetProperty("cpuHardCapPercent").GetInt32() != 20)
            throw new InvalidDataException("Restricted summary inconsistent.");
        foreach (var flag in new[] { "fullSandboxEstablished", "appContainerEstablished", "originalLoaded", "xnaLoaded", "steamLoaded",
            "realInputUsed", "audioUsed", "networkUsed", "guiOpened", "aclChanged", "systemConfigurationChanged", "gameDirectoryAccessed",
            "originalCompatibilityEstablished", "memoryCpuStressTested", "guiCreationProbeAttempted" })
            if (report.GetProperty(flag).GetBoolean()) throw new InvalidDataException("Restricted summary exceeds scope.");
        var status = report.GetProperty("status").GetString();
        var managed = report.GetProperty("managedStartup").GetString();
        var managedExit = report.GetProperty("managedProbeExitCode").GetInt32();
        if (status == "partial-managed-startup-blocked" && managed == "own-net8-startup-blocked" && managedExit == 2)
        {
            restrictedStatus = "partial-managed-startup-blocked";
            restrictedExplanation = "已实现自有进程启动前限制：创建时禁止Win32k GUI系统调用，主线程先挂起，确认Job归属和策略后才恢复。" +
                "Job配置为单个进程、总提交内存512MiB、CPU硬上限20%；这些配置已向操作系统查询核对，但尚未做内存/CPU压力测试。" +
                "只依赖必要Windows API的自有小探针6项通过：正常结束、提前退出、卡死超时、执行前中止、关闭Job、父侧异常清理。所有观察到的自有进程均已退出。" +
                "但是，自有.NET 8进程在同一限制组合下，尚未发出就绪通知就异常退出。当前结果是部分完成，专用入口退出码2；不是原版失败，因为原版没有加载。" +
                "它的作用是确认基本进程控制有效，并把.NET启动兼容问题单独暴露出来。小探针通过不能代替.NET或XNA兼容验证，也不是完整沙箱。";
        }
        else if (status == "own-controls-verified" && managed == "own-net8-verified" && managedExit == 0)
        {
            restrictedStatus = "own-controls-only-verified";
            restrictedExplanation = "已有自有小探针及.NET 8受限控制检查通过摘要。它仅验证启动前策略、Job配置和退出清理；没有文件/网络/设备隔离或原版兼容证据。";
        }
        else throw new InvalidDataException("Restricted managed result inconsistent.");
        if (report.TryGetProperty("startupDiagnosticsVersion", out var diagnosticVersion))
        {
            var count = report.GetProperty("loadedImageCount").GetInt32();
            var ownPhase = report.GetProperty("lastOwnPhase").GetString();
            if (diagnosticVersion.GetInt32() != 1 || report.GetProperty("phaseProtocolPassed").GetInt32() != 8 || count is < 0 or > 128 ||
                (ownPhase is not null && ownPhase is not ("own-entry-file-written" or "own-assembly-check-completed" or "gui-policy-query-returned" or "job-query-verified")))
                throw new InvalidDataException("Restricted startup diagnostics inconsistent.");
            var coreClrObserved = report.GetProperty("observedCoreClrImage").GetBoolean();
            if (coreClrObserved && count == 0) throw new InvalidDataException("Restricted image count inconsistent.");
            startupDiagnosticsStatus = "recorded";
            restrictedExplanation += "最新启动诊断：记录到" + count + "个映像名称；" +
                (coreClrObserved ? ".NET核心运行库已加载。" : "未观察到.NET核心运行库加载事件。") +
                (ownPhase is null ? "没有读到第一条自有阶段记录；这还不能证明入口完全没执行，因为第一条记录的文件操作也可能失败。" : "最后自有阶段为" + ownPhase + "。") +
                "阶段记录的8项校验通过。加载系统绘图库不等于创建了窗口；这里只记录名称，不读取内存内容。异常根因仍未定位，没有放宽限制。";
        }
    }
    Directory.CreateDirectory(output);
    File.WriteAllText(Path.Combine(output, "manifest.json"),
        JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    var html = $$"""
<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>CDR-082 当前进度</title>
<style>body{background:#111827;color:#e5e7eb;font:20px/1.7 system-ui;max-width:1000px;margin:50px auto;padding:20px}
section{background:#1f2937;padding:20px;margin:20px 0;border-radius:12px}h1,h2,a{color:#67e8f9}
table{width:100%;border-collapse:collapse;font-size:17px}th,td{text-align:left;padding:10px;border-bottom:1px solid #475569}</style>
<h1>当前方向：本地反编译与原版逻辑重组</h1>
<section data-restricted-status="{{restrictedStatus}}" data-startup-diagnostics="{{startupDiagnosticsStatus}}"><h2>最新：受限进程原型完成了哪些，卡在哪里？</h2><p>{{restrictedExplanation}}</p>
<p>没有尝试创建GUI，没有调用真实输入、音频、网络或设备，没有改系统配置或ACL，也没有读取游戏目录。
仍未建立AppContainer身份、文件或设备的完整访问限制；原版角色未生成。
验收入口为“验证受限进程原型.cmd”，详细说明见<a href="../../docs/zh-CN/CDR-082-RESTRICTED-PROCESS.md">中文报告</a>。
本页面只读报告，不自动运行该入口。</p></section>
<section data-environment-status="{{environmentStatus}}"><h2>最新：这台电脑具备哪些环境基础？</h2>{{environmentExplanation}}</section>
<section data-progress-revision="2026-10-04"><h2>先看这里：项目现在走到哪里？</h2>
<p>我们的目标是：不启动完整游戏，让本机恢复的原版角色逻辑在自己的桌面程序里工作，之后再逐个接入交互物品。
旧的自写玩法已经退役，不会拿之前的模拟移动来冒充原版运行。</p>
<p>现在仍在 CDR-082：把原版代码和它需要的运行环境准备齐。已有本地编译成功记录，但尚未加载运行原版，也没有生成角色。
最近的工作集中在运行前的依赖风险、异常监控和隔离方案，不是新的角色动作。</p>
<p>阅读时请区分两类内容：下面的编译和适配结果来自本机已有摘要，缺少摘要就显示未检查；
近期工作说明来自截至2026-10-04的项目交付文档，不是本次重新执行检查的结果。生成本页不会重新检查系统库或游戏目录。</p></section>
<section><h2>为什么编译成功了，还不能直接放出角色？</h2>
<p>编译成功表示选中的代码和引用能组成一个库，并不表示这个库脱离原游戏后能正常初始化。
原版角色还依赖场景、时间、输入、图像以及XNA运行组件。某些库在加载时就会执行初始化，早于我们打算测试的角色方法。</p>
<p>因此先要确认这些初始化不会越过本次权限去访问窗口、真实输入、音频或其他服务。
下一步不是重新编写角色规则，而是把原版代码需要的环境接起来，并在受控范围内证明它能工作。</p></section>
<section data-xna-review-status="documented-runtime-unverified"><h2>近期完成：检查XNA启动时可能碰到什么</h2>
<p>XNA是原版使用的一组基础、绘图和游戏组件。此前已把获准的3个库放进本机忽略缓存，并核对身份和文件哈希。
检查发现，基础库和绘图库带有原生初始化，不能只用普通C#异常处理来判断安全。</p>
<p>已有静态交付记录显示：两个混合库各有9个潜在原生调用边界、11处间接调用；检查的6个原生入口中，4个匹配系统导入短跳转，2个函数体仍未分类。
这些数字是代码中可能经过的位置，不是实际运行次数，也不表示已经调用了设备。</p>
<p>它在项目中的作用：提前发现加载风险，避免“尚未创建角色，进程就退出或卡住”时无从排查。
仍未证明：完整初始化的行为、原版运行兼容性和所有副作用是否被限制。这些库没有在本页被加载或执行。</p></section>
<section data-system-review-status="documented-readonly-inventory"><h2>近期完成：确认限定的系统依赖是否存在</h2>
<p>此前授权检查的范围是8个固定托管组件位置和4个32位系统库位置。12项都找到，检查前后的哈希保持一致。
那次只读取文件结构与依赖声明，没有复制或执行这些系统库，也没有继续递归扫描所有Windows依赖。</p>
<p>它在项目中的作用：把“需要的文件没找到”和“文件存在但可能不能安全运行”分开。
现在不能再简单地说所有依赖都缺失；但文件存在、版本匹配，不等于运行时没有设备访问或其他副作用。</p>
<p>此处是此前交付的范围结论，不是今天对系统状态的重新核验；本页不会替你运行系统依赖检查入口。</p></section>
<section data-process-review-status="documented-own-only"><h2>近期完成：测试出问题时，留下最后阶段和退出原因</h2>
<p>已建立自有测试进程监控。父进程负责等待和记录，子进程只运行我们编写的预设测试情景，不运行原版或XNA。</p>
<p>此前8种测试覆盖：正常结束、提前退出、没发完成通知、消息格式错误、轮次不匹配、大量标准输出、大量错误输出以及卡死。
监控能记录最后阶段、退出码、耗时、输出超限和清理结果；此前这些情景均得到预期结果，自有子进程全部退出。</p>
<p>它在项目中的作用：以后最小测试异常退出时，帮助区分“在哪一步停了”和“为什么结束”。
它不能阻止文件、网络或设备访问，所以不是安全沙箱；进程活着也不能证明角色已经生成、正在动或已经显示。</p>
<p>这8种情景不是本次重新运行的结果，也没有模拟真实原版原生崩溃。专用入口是“验证测试进程监控.cmd”，不由本页自动执行。</p></section>
<section data-restricted-plan-status="design-only"><h2>最新完成：受限执行环境方案，尚未实施</h2>
<p>这份方案要解决的是：在原版库开始加载以前，先限制它能访问什么；不能只等访问发生后再写日志。
候选组合是AppContainer权限隔离、启动前限制以及Job进程管理，而不是把普通子进程称为沙箱。</p>
<p>目前没有验证完整的禁真实输入、禁音频配置，也没有证明旧版XNA能在这些限制下工作。
任一必需限制无法落实就停止，不改用无限制进程偷偷重试，不修改原版行为来凑通过。</p>
<p>它在项目中的作用：明确原版试运行前必须补齐哪些保护和证据。当前仅有设计文档，没有建立沙箱、修改系统权限或执行原版。
方案之后已新增限定的只读环境核对，结果见页面顶部；这仍不是直接运行角色的许可。</p>
<p><a href="../../docs/zh-CN/CDR-082-RESTRICTED-EXECUTION-PLAN.md">查看中文方案、限制清单与授权门禁</a>。
本页只提供文档链接，不自动打开工具或执行测试。</p></section>
<section data-isolation-status="{{isolationStatus}}"><h2>运行之前，隔离准备做到哪一步？</h2>{{isolationExplanation}}</section>
<section data-framework-status="{{frameworkStatus}}"><h2>隔离模块能用原版恢复库采用的旧框架编译吗？</h2>{{frameworkExplanation}}</section>
<section><h2>使用找到的零件，拼装到哪一步？</h2>{{closureExplanation}}</section>
<section><h2>编译零件找到了吗？</h2>{{referenceExplanation}}</section>
<section data-compile-status="{{compileStatus}}"><h2>历史：第一次少量文件编译探针</h2>{{compileExplanation}}
<p>这是早期少量文件的尝试，不能代表后续编译结果。请结合“使用找到的零件，拼装到哪一步”阅读，避免把旧失败当成最新状态。
程序只编译、不运行；此页面不输出源码，不会再次读取安装。商业源码和派生构建永不上传。</p></section>
<section><h2>方向调整时保留和移除了什么？</h2>已删除自主实现的角色、八种交互实体、碰撞运动核及旧玩法编排。
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
<section><h2>这些结果已经说明了什么？</h2>旧行为已退役；现有工具分别记录来源、源码恢复、编译、外围适配和进程监控。
每项结果只证明自己的范围：例如编译成功不等于运行成功，自有监控通过不等于原版初始化安全。本报告仅输出统计和说明，
不输出反编译源码、商业图片或安装路径。此前 CDR-016 的安装一致性验证是历史证据，未在这里重跑。</section>
<section><h2>还没有做到什么？</h2><p>目前没有已验证可运行的原版角色。已有编译成功记录不应被抹掉，但仍未证明运行依赖完整、
初始化安全、原版输入与时间连接、角色创建、原版动画输出以及桌面移动。</p>
<p>本页没有角色画面不是显示失败：这阶段尚未生成角色，本页只展示进度和已有证据，不是角色演示。</p></section>
<section><h2>后面按什么顺序推进？</h2>
<ol><li>CDR-082已完成限定的环境基础只读核对，具体本机摘要见顶部；实际访问限制仍未验证，不启用系统功能。</li>
<li>自有小探针已验证基础启动限制与清理；自有.NET 8启动仍失败。先定位这个独立问题，再补文件/网络/设备隔离证据，不放宽策略或拿小探针代替原版运行。</li>
<li>再申请原版最小加载及预设输入/时间测试，先不创建角色、不读取新素材、不进行正常游戏启动。</li>
<li>CDR-083：在前面通过后，连接原版角色依赖、动画与本地资源读取，验证逐步运行和离屏图像。范围及资源访问另行确认。</li>
<li>CDR-084及后续：连接桌面环境，再逐个整合原版交互物品；可见桌面和实时输入验收需要单独授权。</li></ol>
<p>这些是工作顺序，不是已经完成的功能，也不是本页面提供的执行授权。不会为了“能运行”重新编造替代玩法。</p></section>
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
