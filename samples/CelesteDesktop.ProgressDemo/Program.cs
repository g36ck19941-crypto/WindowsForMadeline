using System.Net;
using System.Text;
using System.Text.Json;
using CelesteDesktop.AssetWorker.Data;
using CelesteDesktop.AssetWorker.Meta;
using CelesteDesktop.AssetWorker.SpriteXml;
using CelesteDesktop.Contracts.Assets;

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

if (!frame.CopyPixels().SequenceEqual(sourcePixels))
{
    throw new InvalidOperationException("The decoded demo pixels differ from the generated source.");
}

var page = metadata.Pages.Single();
if (sprites.Definitions.Count != 2 ||
    sprites.Definitions.Any(definition => definition.Animations.Count != 1) ||
    sprites.Definitions.Any(definition => !page.Entries.Any(entry =>
        string.Equals(
            entry.Id,
            definition.Animations[0].AtlasPath + "00",
            StringComparison.Ordinal))))
{
    throw new InvalidOperationException("The generated sprite definitions did not match the generated atlas index.");
}
var manifestPath = Path.Combine(outputDirectory, "manifest.json");
var reportPath = Path.Combine(outputDirectory, "index.html");

var manifest = new
{
    schemaVersion = 1,
    demoId = "CDR-014",
    diagnosticPlaceholder = true,
    source = "program-generated",
    persistedCommercialBytes = 0,
    pipeline = new[]
    {
        "CDR-012 parsed generated atlas metadata",
        "CDR-013 decoded generated RLE pixels to immutable BGRA32",
        "CDR-014 parsed generated sprite animation definitions"
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
    BuildHtml(frame, page, sprites),
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine("DEMO CDR-014 cumulative synthetic asset pipeline");
Console.WriteLine("RESULT metadata_pages=1 metadata_entries=2 decoded_pixels=48 sprite_definitions=2 animations=2 commercial_bytes=0");
Console.WriteLine($"FRAME width={frame.Width} height={frame.Height} stride={frame.Stride}");
Console.WriteLine($"SHA256 {frame.ContentSha256}");
Console.WriteLine($"REPORT {reportPath}");
Console.WriteLine($"MANIFEST {manifestPath}");
return 0;

static string ResolveOutputDirectory(string[] arguments)
{
    if (arguments.Length == 0)
    {
        return Path.GetFullPath(Path.Combine("artifacts", "cdr-013-demo"));
    }

    if (arguments.Length == 2 &&
        string.Equals(arguments[0], "--output", StringComparison.Ordinal))
    {
        return Path.GetFullPath(arguments[1]);
    }

    throw new ArgumentException("Usage: [--output <directory>]");
}

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
    SpriteMetadataDescriptor sprites)
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

    return $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>CelesteDesktopRuntime CDR-014 进度演示</title>
          <style>
            :root{color-scheme:dark;font-family:"Segoe UI","Microsoft YaHei",sans-serif;background:#111827;color:#e5e7eb}
            body{margin:0;padding:32px;max-width:1100px;margin-inline:auto}
            h1{margin:0 0 8px;font-size:30px}.sub{color:#9ca3af;margin-bottom:24px}
            .warning{background:#422006;border:1px solid #f59e0b;padding:12px 16px;border-radius:10px;color:#fde68a}
            .pipeline{display:grid;grid-template-columns:repeat(5,1fr);gap:10px;margin:24px 0}
            .stage{background:#1f2937;border:1px solid #374151;padding:14px;border-radius:10px}.stage b{display:block;color:#67e8f9;margin-bottom:5px}
            .layout{display:flex;gap:28px;align-items:flex-start;flex-wrap:wrap}.canvas{position:relative;width:{{frame.Width * scale}}px;height:{{frame.Height * scale}}px;display:grid;grid-template-columns:repeat({{frame.Width}},{{scale}}px);background:repeating-conic-gradient(#273244 0 25%,#182131 0 50%) 0/24px 24px;box-shadow:0 0 0 1px #64748b}
            .pixel{width:{{scale}}px;height:{{scale}}px;box-shadow:inset 0 0 0 1px rgba(255,255,255,.08)}
            .entry{position:absolute;box-sizing:border-box;border:3px solid #f8fafc;pointer-events:none}.entry span{position:absolute;left:2px;top:2px;background:#020617d9;color:white;font:11px Consolas;padding:2px 4px;white-space:nowrap}
            .facts{min-width:280px;background:#1f2937;border-radius:12px;padding:18px}.facts dt{color:#94a3b8}.facts dd{margin:3px 0 14px;font-family:Consolas,monospace;overflow-wrap:anywhere}
            .ok{color:#86efac}.limits{margin-top:26px;color:#cbd5e1}code{color:#67e8f9}
          </style>
        </head>
        <body>
          <h1>CDR-014 累计项目进度演示</h1>
          <div class="sub">程序生成的 Atlas 目录、RLE 像素与精灵动画定义</div>
          <div class="warning"><b>diagnostic_placeholder=true</b>：下图完全由程序生成，不是 Celeste 素材，也不证明真实安装兼容。</div>
          <div class="pipeline">
            <div class="stage"><b>CDR-010</b>安装结构验证合同</div>
            <div class="stage"><b>CDR-011</b>隔离 Worker 生命周期</div>
            <div class="stage"><b>CDR-012</b>解析 1 页 / 2 个条目</div>
            <div class="stage"><b>CDR-013</b>解码 48 个 BGRA32 像素</div>
            <div class="stage"><b>CDR-014</b>解析 2 个精灵 / 2 个动画定义</div>
          </div>
          <div class="layout">
            <div class="canvas">{{cells}}{{overlays}}</div>
            <dl class="facts">
              <dt>解码结果</dt><dd class="ok">成功，像素与生成源逐字节一致</dd>
              <dt>图页</dt><dd>{{frame.Width}} × {{frame.Height}}，stride={{frame.Stride}}</dd>
              <dt>目录条目</dt><dd>{{page.Entries.Count}}</dd>
              <dt>精灵动画</dt><dd class="ok">{{sprites.Definitions.Count}} 个定义，{{sprites.Definitions.Sum(definition => definition.Animations.Count)}} 个动画；引用与目录相符</dd>
              <dt>SHA-256</dt><dd>{{frame.ContentSha256}}</dd>
              <dt>商业素材字节</dt><dd class="ok">0</dd>
            </dl>
          </div>
          <div class="limits">
            <h2>这证明了什么</h2>
            <p>当前项目已经能把生成的 <code>.meta</code> 目录和 <code>.data</code> 压缩图页转换成可定位、可核验的内存像素，并读取生成的 <code>Sprites.xml</code> 动画定义。</p>
            <h2>仍未证明什么</h2>
            <p>尚未裁出动画帧、显示角色或验证真实游戏安装；这些仍受后续任务门禁限制。</p>
          </div>
        </body>
        </html>
        """;
}

internal readonly record struct Bgra(byte Blue, byte Green, byte Red, byte Alpha);
