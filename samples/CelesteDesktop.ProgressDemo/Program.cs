using System.Net;
using System.Text.Json;

// This report reads names only, never exports cached source or commercial assets.
var output = Path.GetFullPath("artifacts/current-progress-demo");
for (var i = 0; i < args.Length; i++)
{
    if (args[i] != "--output" || ++i >= args.Length)
        throw new ArgumentException("Usage: --output <report directory>");
    output = Path.GetFullPath(args[i]);
}
var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "CelesteDesktopRuntime.sln")))
    throw new InvalidOperationException("Run from the repository root.");
var allowed = Path.GetFullPath(Path.Combine(root, "artifacts")) + Path.DirectorySeparatorChar;
if (!output.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Report output must be under repository artifacts.");
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
var manifest = new
{
    demoId = "CDR-080", source = "summary-only", cacheCount, sourceFileCount = sourceCount,
    modNamedFileCount = modNames, pristineVanillaEstablished = false,
    gameplayImplementationPresent = false, runtimeIntegrated = false, humanVisible = false,
    gameLaunched = false, installationWrites = 0, persistedCommercialBytes = 0
};
Directory.CreateDirectory(output);
File.WriteAllText(Path.Combine(output, "manifest.json"),
    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
var html = $$"""
<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>CDR-080 当前进度</title>
<style>body{background:#111827;color:#e5e7eb;font:20px/1.7 system-ui;max-width:1000px;margin:50px auto;padding:20px}
section{background:#1f2937;padding:20px;margin:20px 0;border-radius:12px}h1,h2{color:#67e8f9}</style>
<h1>当前方向：本地反编译与原版逻辑重组</h1>
<section><h2>这次做了什么</h2>已删除自主实现的角色、八种交互实体、碰撞运动核及旧玩法编排。
旧的移动演示不再运行。保留资源读取、反编译工具、动画呈现、渲染与匿名桌面几何工具。</section>
<section><h2>本机现在有什么</h2>找到 {{cacheCount}} 份缓存，共 {{sourceCount}} 个 C# 文件。
其中 {{modNames}} 个文件名或目录带 Mod/Everest 标记。这是文件目录清点，不是运行或编译验证；
不能据此认定它们是纯原版。没有缓存时这里为零，报告仍然可以生成。</section>
<section><h2>这证明了什么</h2>旧行为已退役，工具层可以独立验证；本报告仅输出统计和说明，
不输出反编译源码、商业图片或安装路径。此前 CDR-016 的安装一致性验证是历史证据，未在这里重跑。</section>
<section><h2>仍未证明什么</h2>目前没有可运行的角色。原版依赖是否齐全、能否独立编译、
动画能否连接、角色是否能在桌面移动都尚未建立。反编译成功不等于游戏已经重组完成。</section>
<section><h2>接下来按什么顺序做</h2>确认原版/Mod 来源 → 找齐角色需要的引擎依赖 →
在仅本地的隔离目录尝试最小编译 → 接入时钟、输入、碰撞环境和资源读取 →
先逐步测试与离屏成帧，再另行授权桌面可见验证。不会为了“能编译”而偷偷编造替代玩法。</section>
<p>游戏未启动；未访问或写入安装目录；未接收实时输入；此页面不是角色演示。</p></html>
""";
File.WriteAllText(Path.Combine(output, "index.html"), html);
Console.WriteLine($"CDR080_REPORT cacheCount={cacheCount} sourceFiles={sourceCount} modNamedFiles={modNames} runtimeIntegrated=false");

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
