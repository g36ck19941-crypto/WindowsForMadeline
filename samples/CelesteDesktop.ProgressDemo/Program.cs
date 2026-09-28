using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CelesteDesktop.AssetWorker.Catalog;
using CelesteDesktop.AssetWorker.Data;
using CelesteDesktop.AssetWorker.Meta;
using CelesteDesktop.AssetWorker.SpriteXml;
using CelesteDesktop.Contracts.Assets;
using CelesteDesktop.Simulation.Core;

const int width = 8;
const int height = 6;

var outputDirectory = ResolveOutputDirectory(args);
Directory.CreateDirectory(outputDirectory);

var sourcePixels = BuildSyntheticPixels(width, height);
var metadataBytes = BuildSyntheticMetadata();
var dataBytes = BuildSyntheticData(width, height, sourcePixels);
var spriteXmlBytes = Encoding.UTF8.GetBytes(BuildSyntheticSpriteXml());

AtlasMetadataDescriptor metadata;
Bgra32Frame frame;
SpriteMetadataDescriptor sprites;
using (var metadataStream = new MemoryStream(metadataBytes, writable: false))
{
    metadata = new AtlasMetadataReader().Read(metadataStream);
}

using (var dataStream = new MemoryStream(dataBytes, writable: false))
{
    frame = new AtlasDataDecoder().Decode(dataStream);
}

using (var spriteStream = new MemoryStream(spriteXmlBytes, writable: false))
{
    sprites = new SpriteXmlReader(["player", "spring"]).Read(spriteStream);
}

var sourceFingerprint = new AssetSourceFingerprint("cdr-015-generated-demo", [
    Fingerprint("atlas.meta", metadataBytes),
    Fingerprint("atlas.data", dataBytes),
    Fingerprint("sprites.xml", spriteXmlBytes)]);
var pageSource = new DemoPageSource("demo/page0", dataBytes);
var catalog = new AssetCatalogBuilder().Build(
    metadata,
    sprites,
    sourceFingerprint,
    ["player", "spring"],
    pageSource);
var simulation = RunSyntheticSimulation();

if (!frame.CopyPixels().SequenceEqual(sourcePixels))
{
    throw new InvalidOperationException("The decoded demo pixels differ from the generated source.");
}

var page = metadata.Pages.Single();
if (catalog.Entities.Count != 2 ||
    catalog.Entities.Any(entity => entity.Animations.Count != 1) ||
    catalog.Entities.Sum(entity => entity.Animations.Sum(animation => animation.Frames.Count)) != 2 ||
    catalog.DecodedPageCount != 1 ||
    pageSource.OpenCount != 1)
{
    throw new InvalidOperationException("The generated catalog did not match the allowlisted sprite definitions.");
}
var manifestPath = Path.Combine(outputDirectory, "manifest.json");
var reportPath = Path.Combine(outputDirectory, "index.html");

var manifest = new
{
    schemaVersion = 1,
    demoId = "CDR-020",
    diagnosticPlaceholder = true,
    source = "program-generated",
    persistedCommercialBytes = 0,
    pipeline = new[]
    {
        "CDR-012 parsed generated atlas metadata",
        "CDR-013 decoded generated RLE pixels to immutable BGRA32",
        "CDR-014 parsed generated sprite animation definitions",
        "CDR-015 built isolated entity catalogs and decoded only the required page",
        "CDR-020 advanced generated Actor and Solid geometry at fixed 60 Hz"
    },
    frame = new
    {
        frame.Width,
        frame.Height,
        frame.Stride,
        frame.PixelByteCount,
        frame.ContentSha256
    },
    sprites = sprites.Definitions.Select(definition => new
    {
        definition.Id,
        definition.StartAnimationId,
        animationCount = definition.Animations.Count,
        firstAnimationPath = definition.Animations[0].AtlasPath
    }),
    catalog = new
    {
        entityCount = catalog.Entities.Count,
        animationCount = catalog.Entities.Sum(entity => entity.Animations.Count),
        frameCount = catalog.Entities.Sum(entity => entity.Animations.Sum(animation => animation.Frames.Count)),
        catalog.DecodedPageCount,
        openedPageCount = pageSource.OpenCount,
        catalog.CatalogSha256,
        entities = catalog.Entities.Select(entity => new
        {
            entity.EntityId,
            animations = entity.Animations.Select(animation => new
            {
                animation.Id,
                frames = animation.Frames.Select(catalogFrame => new
                {
                    catalogFrame.AtlasEntryId,
                    catalogFrame.Frame.Width,
                    catalogFrame.Frame.Height,
                    catalogFrame.Frame.ContentSha256
                })
            })
        })
    },
    simulation = new
    {
        tickRate = SimulationConstants.TicksPerSecond,
        tickCount = simulation.Rows.Count,
        simulation.FinalActorX,
        simulation.FinalPlatformX,
        simulation.CarryEventCount,
        simulation.BlockedEventCount,
        deterministicReplay = simulation.DeterministicReplay,
        rows = simulation.Rows
    },
    entries = page.Entries.Select(entry => new
    {
        entry.Id,
        entry.X,
        entry.Y,
        entry.Width,
        entry.Height,
        entry.FrameWidth,
        entry.FrameHeight
    })
};

File.WriteAllText(
    manifestPath,
    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

File.WriteAllText(
    reportPath,
    BuildHtml(frame, page, sprites, catalog, simulation),
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine("DEMO CDR-020 cumulative synthetic asset and simulation pipeline");
Console.WriteLine($"RESULT metadata_pages=1 metadata_entries=2 decoded_pixels=48 sprite_definitions=2 animations=2 catalog_entities=2 catalog_frames=2 decoded_pages=1 simulation_ticks={simulation.Rows.Count} carry_events={simulation.CarryEventCount} blocked_events={simulation.BlockedEventCount} deterministic_replay={simulation.DeterministicReplay.ToString().ToLowerInvariant()} commercial_bytes=0");
Console.WriteLine($"FRAME width={frame.Width} height={frame.Height} stride={frame.Stride}");
Console.WriteLine($"SHA256 {frame.ContentSha256}");
Console.WriteLine($"CATALOG_SHA256 {catalog.CatalogSha256}");
Console.WriteLine($"REPORT {reportPath}");
Console.WriteLine($"MANIFEST {manifestPath}");
return 0;

static string ResolveOutputDirectory(string[] arguments)
{
    if (arguments.Length == 0)
    {
        return Path.GetFullPath(Path.Combine("artifacts", "cdr-020-demo"));
    }

    if (arguments.Length == 2 &&
        string.Equals(arguments[0], "--output", StringComparison.Ordinal))
    {
        return Path.GetFullPath(arguments[1]);
    }

    throw new ArgumentException("Usage: [--output <directory>]");
}

static AssetFileFingerprint Fingerprint(string logicalPath, byte[] bytes) =>
    new(
        logicalPath,
        bytes.LongLength,
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());

static byte[] BuildSyntheticPixels(int width, int height)
{
    var pixels = new byte[checked(width * height * 4)];

    var cyan = new Bgra(220, 175, 55, 255);
    var orange = new Bgra(35, 115, 245, 255);
    var green = new Bgra(90, 210, 110, 255);
    var red = new Bgra(75, 65, 230, 255);

    SetPixel(pixels, width, 1, 0, cyan);
    SetPixel(pixels, width, 0, 1, cyan);
    SetPixel(pixels, width, 1, 1, cyan);
    SetPixel(pixels, width, 2, 1, cyan);
    SetPixel(pixels, width, 1, 2, cyan);
    SetPixel(pixels, width, 0, 3, orange);
    SetPixel(pixels, width, 1, 3, cyan);
    SetPixel(pixels, width, 2, 3, orange);
    SetPixel(pixels, width, 0, 4, orange);
    SetPixel(pixels, width, 2, 4, orange);

    SetPixel(pixels, width, 5, 3, green);
    SetPixel(pixels, width, 6, 3, green);
    SetPixel(pixels, width, 4, 4, red);
    SetPixel(pixels, width, 5, 4, red);
    SetPixel(pixels, width, 6, 4, red);
    SetPixel(pixels, width, 7, 4, red);
    SetPixel(pixels, width, 5, 5, red);
    SetPixel(pixels, width, 6, 5, red);

    return pixels;
}

static void SetPixel(byte[] pixels, int width, int x, int y, Bgra color)
{
    var offset = checked(((y * width) + x) * 4);
    pixels[offset] = color.Blue;
    pixels[offset + 1] = color.Green;
    pixels[offset + 2] = color.Red;
    pixels[offset + 3] = color.Alpha;
}

static byte[] BuildSyntheticMetadata()
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
        writer.Write(1);
        writer.Write("CDR synthetic demo");
        writer.Write(1);
        writer.Write((short)1);
        writer.Write("demo/page0");
        writer.Write((short)2);
        WriteEntry(writer, "demo/player/idle00", 0, 0, 3, 5);
        WriteEntry(writer, "demo/spring/idle00", 4, 3, 4, 3);
    }

    return stream.ToArray();
}

static void WriteEntry(
    BinaryWriter writer,
    string id,
    short x,
    short y,
    short width,
    short height)
{
    writer.Write(id);
    writer.Write(x);
    writer.Write(y);
    writer.Write(width);
    writer.Write(height);
    writer.Write((short)0);
    writer.Write((short)0);
    writer.Write(width);
    writer.Write(height);
}

static byte[] BuildSyntheticData(int width, int height, byte[] pixels)
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
        writer.Write(width);
        writer.Write(height);
        writer.Write((byte)1);

        var pixelIndex = 0;
        var pixelCount = width * height;
        while (pixelIndex < pixelCount)
        {
            var offset = pixelIndex * 4;
            var color = new Bgra(
                pixels[offset],
                pixels[offset + 1],
                pixels[offset + 2],
                pixels[offset + 3]);
            var runLength = 1;
            while (pixelIndex + runLength < pixelCount && runLength < byte.MaxValue)
            {
                var nextOffset = (pixelIndex + runLength) * 4;
                if (!color.Equals(new Bgra(
                    pixels[nextOffset],
                    pixels[nextOffset + 1],
                    pixels[nextOffset + 2],
                    pixels[nextOffset + 3])))
                {
                    break;
                }

                runLength++;
            }

            writer.Write((byte)runLength);
            writer.Write(color.Alpha);
            if (color.Alpha != 0)
            {
                writer.Write(color.Blue);
                writer.Write(color.Green);
                writer.Write(color.Red);
            }

            pixelIndex += runLength;
        }
    }

    return stream.ToArray();
}

static string BuildSyntheticSpriteXml() =>
    "<Sprites>" +
    "<player path=\"demo/player/\" start=\"idle\">" +
    "<Justify x=\"0.5\" y=\"1\"/>" +
    "<Loop id=\"idle\" path=\"idle\" frames=\"0\"/>" +
    "<Metadata><Frames path=\"idle\" hair=\"0,-2\"/></Metadata>" +
    "</player>" +
    "<spring path=\"demo/spring/\" start=\"idle\">" +
    "<Center/><Loop id=\"idle\" path=\"idle\" frames=\"0\"/>" +
    "</spring>" +
    "</Sprites>";

static string BuildHtml(
    Bgra32Frame frame,
    AtlasPageDescriptor page,
    SpriteMetadataDescriptor sprites,
    NormalizedAssetCatalog catalog,
    DemoSimulation simulation)
{
    const int scale = 52;
    var pixels = frame.CopyPixels();
    var cells = new StringBuilder();
    for (var y = 0; y < frame.Height; y++)
    {
        for (var x = 0; x < frame.Width; x++)
        {
            var offset = ((y * frame.Width) + x) * 4;
            var alpha = pixels[offset + 3] / 255.0;
            cells.Append("<div class=\"pixel\" style=\"background:rgba(")
                .Append(pixels[offset + 2]).Append(',')
                .Append(pixels[offset + 1]).Append(',')
                .Append(pixels[offset]).Append(',')
                .Append(alpha.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))
                .Append(")\" title=\"x=").Append(x)
                .Append(" y=").Append(y).Append("\"></div>");
        }
    }

    var overlays = new StringBuilder();
    foreach (var entry in page.Entries)
    {
        overlays.Append("<div class=\"entry\" style=\"left:")
            .Append(entry.X * scale).Append("px;top:")
            .Append(entry.Y * scale).Append("px;width:")
            .Append(entry.Width * scale).Append("px;height:")
            .Append(entry.Height * scale).Append("px\"><span>")
            .Append(WebUtility.HtmlEncode(entry.Id))
            .Append("</span></div>");
    }

    var simulationRows = new StringBuilder();
    foreach (var row in simulation.Rows)
    {
        simulationRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(row.ActorX).Append(',').Append(row.ActorY)
            .Append("</td><td>").Append(row.PlatformX).Append(',').Append(row.PlatformY)
            .Append("</td><td>").Append(row.ActorXSubpixel)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    return $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>CelesteDesktopRuntime CDR-020 进度演示</title>
          <style>
            :root{color-scheme:dark;font-family:"Segoe UI","Microsoft YaHei",sans-serif;background:#111827;color:#e5e7eb}
            body{margin:0;padding:32px;max-width:1100px;margin-inline:auto}
            h1{margin:0 0 8px;font-size:30px}.sub{color:#9ca3af;margin-bottom:24px}
            .warning{background:#422006;border:1px solid #f59e0b;padding:12px 16px;border-radius:10px;color:#fde68a}
            .pipeline{display:grid;grid-template-columns:repeat(7,1fr);gap:10px;margin:24px 0}
            .stage{background:#1f2937;border:1px solid #374151;padding:14px;border-radius:10px}.stage b{display:block;color:#67e8f9;margin-bottom:5px}
            .layout{display:flex;gap:28px;align-items:flex-start;flex-wrap:wrap}.canvas{position:relative;width:{{frame.Width * scale}}px;height:{{frame.Height * scale}}px;display:grid;grid-template-columns:repeat({{frame.Width}},{{scale}}px);background:repeating-conic-gradient(#273244 0 25%,#182131 0 50%) 0/24px 24px;box-shadow:0 0 0 1px #64748b}
            .pixel{width:{{scale}}px;height:{{scale}}px;box-shadow:inset 0 0 0 1px rgba(255,255,255,.08)}
            .entry{position:absolute;box-sizing:border-box;border:3px solid #f8fafc;pointer-events:none}.entry span{position:absolute;left:2px;top:2px;background:#020617d9;color:white;font:11px Consolas;padding:2px 4px;white-space:nowrap}
            .facts{min-width:280px;background:#1f2937;border-radius:12px;padding:18px}.facts dt{color:#94a3b8}.facts dd{margin:3px 0 14px;font-family:Consolas,monospace;overflow-wrap:anywhere}
            .ok{color:#86efac}.limits{margin-top:26px;color:#cbd5e1}code{color:#67e8f9}
            table{width:100%;border-collapse:collapse;margin-top:16px;background:#1f2937}th,td{padding:9px 12px;border-bottom:1px solid #374151;text-align:left;font-family:Consolas,monospace}th{color:#67e8f9}
          </style>
        </head>
        <body>
          <h1>CDR-020 累计项目进度演示</h1>
          <div class="sub">程序生成的素材管线，以及固定 60 Hz 的 Actor / Solid 逐 tick 模拟</div>
          <div class="warning"><b>diagnostic_placeholder=true</b>：下图完全由程序生成，不是 Celeste 素材，也不证明真实安装兼容。</div>
          <div class="pipeline">
            <div class="stage"><b>CDR-010</b>安装结构验证合同</div>
            <div class="stage"><b>CDR-011</b>隔离 Worker 生命周期</div>
            <div class="stage"><b>CDR-012</b>解析 1 页 / 2 个条目</div>
            <div class="stage"><b>CDR-013</b>解码 48 个 BGRA32 像素</div>
            <div class="stage"><b>CDR-014</b>解析 2 个精灵 / 2 个动画定义</div>
            <div class="stage"><b>CDR-015</b>构建 2 个实体目录 / 2 个帧</div>
            <div class="stage"><b>CDR-020</b>Actor / Solid 固定步进、携带与碰撞</div>
          </div>
          <div class="layout">
            <div class="canvas">{{cells}}{{overlays}}</div>
            <dl class="facts">
              <dt>解码结果</dt><dd class="ok">成功，像素与生成源逐字节一致</dd>
              <dt>图页</dt><dd>{{frame.Width}} × {{frame.Height}}，stride={{frame.Stride}}</dd>
              <dt>目录条目</dt><dd>{{page.Entries.Count}}</dd>
              <dt>精灵动画</dt><dd class="ok">{{sprites.Definitions.Count}} 个定义，{{sprites.Definitions.Sum(definition => definition.Animations.Count)}} 个动画；引用与目录相符</dd>
              <dt>规范目录</dt><dd class="ok">{{catalog.Entities.Count}} 个实体，{{catalog.Entities.Sum(entity => entity.Animations.Sum(animation => animation.Frames.Count))}} 个帧；只解码 {{catalog.DecodedPageCount}} 页</dd>
              <dt>目录 SHA-256</dt><dd>{{catalog.CatalogSha256}}</dd>
              <dt>SHA-256</dt><dd>{{frame.ContentSha256}}</dd>
              <dt>商业素材字节</dt><dd class="ok">0</dd>
            </dl>
          </div>
          <h2>CDR-020 逐 tick 模拟轨迹</h2>
          <p>程序生成一个 Actor、一个移动平台和一面静态墙，连续运行 {{simulation.Rows.Count}} 个固定 tick。平台累计移动到 x={{simulation.FinalPlatformX}}，Actor 在平台携带与自身亚像素移动后到 x={{simulation.FinalActorX}}；重复运行结果 <span class="ok">{{(simulation.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Actor x,y</th><th>Solid x,y</th><th>Actor X 余量</th><th>事件</th></tr></thead><tbody>{{simulationRows}}</tbody></table>
          <div class="limits">
            <h2>这证明了什么</h2>
            <p>当前项目既能把生成的 <code>.meta</code>、<code>.data</code> 与 <code>Sprites.xml</code> 组合成动画帧目录，也能让生成的 Actor 与 Solid 在固定 60 Hz 下按整像素碰撞和亚像素余量推进，并记录携带、阻挡等事件。</p>
            <h2>仍未证明什么</h2>
            <p>CDR-016 已证明指定安装的素材格式兼容，但本演示仍不读取商业素材。尚未实现 Madeline 的跑跳参数、冲刺/攀爬、渲染或桌面交互，因此不证明角色可见或手感还原。</p>
          </div>
        </body>
        </html>
        """;
}

static DemoSimulation RunSyntheticSimulation()
{
    var first = RunSimulationOnce();
    var second = RunSimulationOnce();
    var deterministic = JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second);
    return first with { DeterministicReplay = deterministic };
}

static DemoSimulation RunSimulationOnce()
{
    var world = new SimulationWorld();
    var platform = new Solid("demo-platform", 0, 10, 8, 2);
    var actor = new Actor("demo-actor", 1, 8, 2, 2);
    var wall = new Solid("demo-wall", 10, 0, 2, 12);
    world.Add(platform);
    world.Add(actor);
    world.Add(wall);

    var rows = new List<DemoSimulationRow>();
    for (var index = 0; index < 13; index++)
    {
        world.Step(_ =>
        {
            platform.Move(0.25m, 0m, world);
            actor.MoveX(0.35m, world);
        });
        rows.Add(new DemoSimulationRow(
            world.Tick,
            actor.X,
            actor.Y,
            platform.X,
            platform.Y,
            actor.XSubpixel,
            string.Join(", ", world.Events.Select(item => item.Kind.ToString()))));
    }

    return new DemoSimulation(
        rows.AsReadOnly(),
        actor.X,
        platform.X,
        rows.Count(row => row.Events.Contains(nameof(SimulationEventKind.ActorCarried), StringComparison.Ordinal)),
        rows.Count(row => row.Events.Contains(nameof(SimulationEventKind.ActorBlocked), StringComparison.Ordinal)),
        false);
}

internal readonly record struct Bgra(byte Blue, byte Green, byte Red, byte Alpha);

internal sealed class DemoPageSource : IAtlasPageStreamSource
{
    private readonly string _logicalPath;
    private readonly byte[] _bytes;

    public DemoPageSource(string logicalPath, byte[] bytes)
    {
        _logicalPath = logicalPath;
        _bytes = (byte[])bytes.Clone();
    }

    public int OpenCount { get; private set; }

    public Stream? OpenPage(string logicalPath)
    {
        if (!string.Equals(logicalPath, _logicalPath, StringComparison.Ordinal))
        {
            return null;
        }

        OpenCount++;
        return new MemoryStream(_bytes, writable: false);
    }
}

internal sealed record DemoSimulation(
    IReadOnlyList<DemoSimulationRow> Rows,
    int FinalActorX,
    int FinalPlatformX,
    int CarryEventCount,
    int BlockedEventCount,
    bool DeterministicReplay);

internal sealed record DemoSimulationRow(
    long Tick,
    int ActorX,
    int ActorY,
    int PlatformX,
    int PlatformY,
    decimal ActorXSubpixel,
    string Events);
