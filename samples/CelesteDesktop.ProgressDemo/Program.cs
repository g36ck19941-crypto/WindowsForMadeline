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
using CelesteDesktop.Player;
using CelesteDesktop.Rendering;
using CelesteDesktop.Desktop;
using CelesteDesktop.Animation;
using CelesteDesktop.Entity.Theo;
using CelesteDesktop.Entity.Glider;

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
var player = RunSyntheticPlayer();
var traversal = RunSyntheticTraversal();
var presentation = RunSyntheticPresentation();
var desktop = RunSyntheticDesktop();
var animationPresentation = RunSyntheticAnimationPresentation(catalog);
var theo = RunSyntheticTheo();
var glider = RunSyntheticGlider();

if (!frame.CopyPixels().SequenceEqual(sourcePixels))
{
    throw new InvalidOperationException("The decoded demo pixels differ from the generated source.");
}

var page = metadata.Pages.Single();
if (catalog.Entities.Count != 2 ||
    catalog.Entities.Any(entity => entity.Animations.Count != 1) ||
    catalog.Entities.Sum(entity => entity.Animations.Sum(animation => animation.Frames.Count)) != 3 ||
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
    demoId = "CDR-041",
    diagnosticPlaceholder = true,
    source = "program-generated",
    persistedCommercialBytes = 0,
    pipeline = new[]
    {
        "CDR-012 parsed generated atlas metadata",
        "CDR-013 decoded generated RLE pixels to immutable BGRA32",
        "CDR-014 parsed generated sprite animation definitions",
        "CDR-015 built isolated entity catalogs and decoded only the required page",
        "CDR-020 advanced generated Actor and Solid geometry at fixed 60 Hz",
        "CDR-021 applied generated Normal and Jump input snapshots",
        "CDR-022 applied generated Dash, Wall and Climb state transitions",
        "CDR-030 exercised generated premultiplied frames through the isolated presentation health chain",
        "CDR-031 tracked generated anonymous desktop geometry, DPI, visibility and velocity",
        "CDR-032 resolved generated catalog animations by fixed tick and presented immutable composed frames offline",
        "CDR-040 ran generated Theo pickup carry throw bounce and isolation behavior at fixed tick",
        "CDR-041 ran generated Glider pickup carry fall-limit throw glide bounce and isolation behavior at fixed tick"
    },
    independentValidation = new
    {
        taskId = "CDR-016",
        status = "passed-separately",
        executedByThisDemo = false,
        persistedCommercialBytes = 0
    },
    unassignedTaskIds = new[] { "CDR-017", "CDR-018", "CDR-019" },
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
    player = new
    {
        tickCount = player.Rows.Count,
        player.MaxRunReached,
        player.JumpEventCount,
        player.MinimumY,
        deterministicReplay = player.DeterministicReplay,
        rows = player.Rows
    },
    traversal = new
    {
        tickCount = traversal.Rows.Count,
        traversal.DashStartedCount,
        traversal.WallSlideStartedCount,
        traversal.WallJumpedCount,
        traversal.ClimbStartedCount,
        traversal.MinimumStamina,
        deterministicReplay = traversal.DeterministicReplay,
        rows = traversal.Rows
    },
    presentation = new
    {
        presentation.Geometry.VirtualLeft,
        presentation.Geometry.VirtualTop,
        presentation.Geometry.WidthDips,
        presentation.Geometry.HeightDips,
        presentation.Geometry.DpiX,
        presentation.Geometry.DpiY,
        presentation.Geometry.PixelWidth,
        presentation.Geometry.PixelHeight,
        presentation.PresentCalls,
        presentation.PixelsChangedCount,
        presentation.FirstFingerprint,
        presentation.SecondFingerprint,
        presentation.NativeHiddenSmokeValidatedSeparately,
        humanVisibilityConfirmed = false,
        events = presentation.Events
    },
    desktop = new
    {
        source = "program-generated",
        snapshotCount = desktop.Snapshots.Count,
        finalVisibleSurfaceCount = desktop.Snapshots[^1].Surfaces.Count,
        movedSurfaceCount = desktop.Snapshots[^1].Surfaces.Count(item => item.VelocityX != 0 || item.VelocityY != 0),
        titlesRead = 0,
        contentRead = 0,
        screenshotsRead = 0,
        inputRead = 0,
        snapshots = desktop.Snapshots
    },
    animationPresentation = new
    {
        source = "program-generated-validated-catalog",
        tickCount = animationPresentation.Rows.Count,
        animationPresentation.PresentedCount,
        animationPresentation.FrameChangedCount,
        animationPresentation.DeterministicReplay,
        humanVisibilityConfirmed = false,
        rows = animationPresentation.Rows,
        events = animationPresentation.AnimationEvents
    },
    theo = new
    {
        source = "program-generated-geometry-and-input",
        fidelity = "partial",
        tickCount = theo.Rows.Count,
        theo.PickupCount,
        theo.ThrowCount,
        theo.HorizontalBounceCount,
        theo.LandingCount,
        theo.SquishCount,
        theo.DeterministicReplay,
        rows = theo.Rows
    },
    glider = new
    {
        source = "program-generated-geometry-and-input",
        fidelity = "partial",
        tickCount = glider.Rows.Count,
        glider.PickupCount,
        glider.ThrowCount,
        glider.HolderFallLimitedCount,
        glider.HorizontalBounceCount,
        glider.LandingCount,
        glider.DestroyCount,
        glider.PlayerFallLimitAppliedCount,
        glider.DeterministicReplay,
        rows = glider.Rows
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
    BuildHtml(frame, page, sprites, catalog, simulation, player, traversal, presentation, desktop, animationPresentation, theo, glider),
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine("DEMO CDR-041 cumulative deterministic Glider pipeline");
Console.WriteLine($"RESULT metadata_pages=1 metadata_entries=3 decoded_pixels=48 sprite_definitions=2 animations=2 catalog_entities=2 catalog_frames=3 decoded_pages=1 simulation_ticks={simulation.Rows.Count} carry_events={simulation.CarryEventCount} blocked_events={simulation.BlockedEventCount} player_ticks={player.Rows.Count} max_run_reached={player.MaxRunReached.ToString().ToLowerInvariant()} jump_events={player.JumpEventCount} traversal_ticks={traversal.Rows.Count} dash_started={traversal.DashStartedCount} wall_slide_started={traversal.WallSlideStartedCount} wall_jumped={traversal.WallJumpedCount} climb_started={traversal.ClimbStartedCount} traversal_replay={traversal.DeterministicReplay.ToString().ToLowerInvariant()} present_calls={presentation.PresentCalls} pixels_changed={presentation.PixelsChangedCount} desktop_snapshots={desktop.Snapshots.Count} visible_surfaces={desktop.Snapshots[^1].Surfaces.Count} moved_surfaces={desktop.Snapshots[^1].Surfaces.Count(item => item.VelocityX != 0 || item.VelocityY != 0)} animation_ticks={animationPresentation.Rows.Count} animation_presented={animationPresentation.PresentedCount} animation_frame_changes={animationPresentation.FrameChangedCount} animation_replay={animationPresentation.DeterministicReplay.ToString().ToLowerInvariant()} theo_ticks={theo.Rows.Count} theo_pickups={theo.PickupCount} theo_throws={theo.ThrowCount} theo_horizontal_bounces={theo.HorizontalBounceCount} theo_landings={theo.LandingCount} theo_replay={theo.DeterministicReplay.ToString().ToLowerInvariant()} glider_ticks={glider.Rows.Count} glider_pickups={glider.PickupCount} glider_throws={glider.ThrowCount} glider_fall_limits={glider.HolderFallLimitedCount} glider_player_fall_limits_applied={glider.PlayerFallLimitAppliedCount} glider_horizontal_bounces={glider.HorizontalBounceCount} glider_landings={glider.LandingCount} glider_replay={glider.DeterministicReplay.ToString().ToLowerInvariant()} human_visible=false commercial_bytes=0");
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
        return Path.GetFullPath(Path.Combine("artifacts", "cdr-041-demo"));
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

    SetPixel(pixels, width, 4, 0, orange);
    SetPixel(pixels, width, 3, 1, cyan);
    SetPixel(pixels, width, 4, 1, orange);
    SetPixel(pixels, width, 5, 1, cyan);
    SetPixel(pixels, width, 4, 2, orange);

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
        writer.Write((short)3);
        WriteEntry(writer, "demo/player/idle00", 0, 0, 3, 5);
        WriteEntry(writer, "demo/player/idle01", 3, 0, 3, 3);
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
    "<Loop id=\"idle\" path=\"idle\" frames=\"0-1\" delay=\"0.05\"/>" +
    "<Metadata><Frames path=\"idle\" hair=\"0,-2|0,-2\"/></Metadata>" +
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
    DemoSimulation simulation,
    DemoPlayer player,
    DemoTraversal traversal,
    DemoPresentation presentation,
    DemoDesktop desktop,
    DemoAnimationPresentation animationPresentation,
    DemoTheo theo,
    DemoGlider glider)
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

    var playerRows = new StringBuilder();
    foreach (var row in player.Rows)
    {
        playerRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(row.X).Append(',').Append(row.Y)
            .Append("</td><td>").Append(decimal.Round(row.SpeedX, 3)).Append(',').Append(decimal.Round(row.SpeedY, 3))
            .Append("</td><td>").Append(row.Grounded)
            .Append("</td><td>").Append(row.CoyoteTicks).Append('/').Append(row.BufferTicks).Append('/').Append(row.VariableTicks)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var traversalRows = new StringBuilder();
    foreach (var row in traversal.Rows)
    {
        traversalRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(row.X).Append(',').Append(row.Y)
            .Append("</td><td>").Append(decimal.Round(row.SpeedX, 3)).Append(',').Append(decimal.Round(row.SpeedY, 3))
            .Append("</td><td>").Append(row.Dashes)
            .Append("</td><td>").Append(decimal.Round(row.Stamina, 3))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var presentationCells = new StringBuilder();
    var presentationPixels = presentation.SecondFrame.CopyPixels();
    for (var y = 0; y < presentation.SecondFrame.Height; y++)
    {
        for (var x = 0; x < presentation.SecondFrame.Width; x++)
        {
            var offset = (y * presentation.SecondFrame.Stride) + x * 4;
            var alpha = presentationPixels[offset + 3] / 255.0;
            presentationCells.Append("<div class=\"present-pixel\" style=\"background:rgba(")
                .Append(presentationPixels[offset + 2]).Append(',')
                .Append(presentationPixels[offset + 1]).Append(',')
                .Append(presentationPixels[offset]).Append(',')
                .Append(alpha.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))
                .Append(")\"></div>");
        }
    }

    var presentationRows = new StringBuilder();
    foreach (var item in presentation.Events)
    {
        presentationRows.Append("<tr><td>").Append(item.Sequence)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(item.EventId))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(item.Stage))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(item.Outcome))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(item.Backend))
            .Append("</td></tr>");
    }

    var desktopRows = new StringBuilder();
    foreach (var snapshot in desktop.Snapshots)
    {
        foreach (var surface in snapshot.Surfaces)
        {
            desktopRows.Append("<tr><td>").Append(snapshot.Sequence)
                .Append("</td><td>").Append(WebUtility.HtmlEncode(surface.AnonymousId))
                .Append("</td><td>").Append(surface.Bounds.Left).Append(',').Append(surface.Bounds.Top)
                .Append(" / ").Append(surface.Bounds.Width).Append('x').Append(surface.Bounds.Height)
                .Append("</td><td>").Append(surface.Dpi)
                .Append("</td><td>").Append(surface.VelocityX).Append(',').Append(surface.VelocityY)
                .Append("</td></tr>");
        }
    }

    var animationRows = new StringBuilder();
    foreach (var row in animationPresentation.Rows)
    {
        animationRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.AnimationId))
            .Append("</td><td>").Append(row.FrameIndex)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.AtlasEntryId))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.FrameFingerprint))
            .Append("</td><td>").Append(row.PixelsChanged)
            .Append("</td></tr>");
    }

    var theoRows = new StringBuilder();
    foreach (var row in theo.Rows)
    {
        theoRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(row.X).Append(',').Append(row.Y)
            .Append("</td><td>").Append(row.SpeedX).Append(',').Append(row.SpeedY)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.HolderId ?? "-"))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var gliderRows = new StringBuilder();
    foreach (var row in glider.Rows)
    {
        gliderRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(row.X).Append(',').Append(row.Y)
            .Append("</td><td>").Append(row.SpeedX).Append(',').Append(row.SpeedY)
            .Append("</td><td>").Append(row.IsOpen)
            .Append("</td><td>").Append(row.HolderFallLimitRequired)
            .Append("</td><td>").Append(row.PlayerSpeedY)
            .Append("</td><td>").Append(row.PlayerFallLimitApplied)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    return $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>CelesteDesktopRuntime CDR-041 进度演示</title>
          <style>
            :root{color-scheme:dark;font-family:"Segoe UI","Microsoft YaHei",sans-serif;background:#111827;color:#e5e7eb}
            body{margin:0;padding:32px;max-width:1100px;margin-inline:auto}
            h1{margin:0 0 8px;font-size:30px}.sub{color:#9ca3af;margin-bottom:24px}
            .warning{background:#422006;border:1px solid #f59e0b;padding:12px 16px;border-radius:10px;color:#fde68a}
            .pipeline{display:grid;grid-template-columns:repeat(auto-fit,minmax(155px,1fr));gap:10px;margin:24px 0}
            .stage{background:#1f2937;border:1px solid #374151;padding:14px;border-radius:10px}.stage b{display:block;color:#67e8f9;margin-bottom:5px}
            .layout{display:flex;gap:28px;align-items:flex-start;flex-wrap:wrap}.canvas{position:relative;width:{{frame.Width * scale}}px;height:{{frame.Height * scale}}px;display:grid;grid-template-columns:repeat({{frame.Width}},{{scale}}px);background:repeating-conic-gradient(#273244 0 25%,#182131 0 50%) 0/24px 24px;box-shadow:0 0 0 1px #64748b}
            .pixel{width:{{scale}}px;height:{{scale}}px;box-shadow:inset 0 0 0 1px rgba(255,255,255,.08)}
            .entry{position:absolute;box-sizing:border-box;border:3px solid #f8fafc;pointer-events:none}.entry span{position:absolute;left:2px;top:2px;background:#020617d9;color:white;font:11px Consolas;padding:2px 4px;white-space:nowrap}
            .facts{min-width:280px;background:#1f2937;border-radius:12px;padding:18px}.facts dt{color:#94a3b8}.facts dd{margin:3px 0 14px;font-family:Consolas,monospace;overflow-wrap:anywhere}
            .ok{color:#86efac}.limits{margin-top:26px;color:#cbd5e1}.limits h2{color:#f8fafc;margin-top:28px}.limits .plain{font-size:18px;color:#e2e8f0}.limits li{margin:9px 0;line-height:1.65}.limits strong{color:#fde68a}code{color:#67e8f9}
            .present-canvas{width:288px;height:216px;display:grid;grid-template-columns:repeat({{presentation.SecondFrame.Width}},1fr);background:#0f172a;border:1px solid #64748b;image-rendering:pixelated}.present-pixel{min-width:0;min-height:0}
            .gap-note{background:#172033;border:1px dashed #64748b;padding:12px 16px;border-radius:10px;color:#cbd5e1;margin:-10px 0 24px}
            table{width:100%;border-collapse:collapse;margin-top:16px;background:#1f2937}th,td{padding:9px 12px;border-bottom:1px solid #374151;text-align:left;font-family:Consolas,monospace}th{color:#67e8f9}
          </style>
        </head>
        <body>
          <h1>CDR-041 累计项目进度演示</h1>
          <div class="sub">程序生成素材目录、确定性角色/实体模拟、离线动画呈现与匿名桌面几何</div>
          <div class="warning"><b>diagnostic_placeholder=true</b>：图像完全由程序生成，不是 Celeste 素材。本演示自身不读取真实安装；真实格式兼容性已由独立的 CDR-016 只读验证完成。</div>
          <div class="pipeline">
            <div class="stage"><b>CDR-010</b>安装结构验证合同</div>
            <div class="stage"><b>CDR-011</b>隔离 Worker 生命周期</div>
            <div class="stage"><b>CDR-012</b>解析 1 页 / 2 个条目</div>
            <div class="stage"><b>CDR-013</b>解码 48 个 BGRA32 像素</div>
            <div class="stage"><b>CDR-014</b>解析 2 个精灵 / 2 个动画定义</div>
            <div class="stage"><b>CDR-015</b>构建 2 个实体目录 / 2 个帧</div>
            <div class="stage"><b>CDR-016</b>独立只读验证已通过，本演示不重复读取安装</div>
            <div class="stage"><b>CDR-020</b>Actor / Solid 固定步进、携带与碰撞</div>
            <div class="stage"><b>CDR-021</b>跑动、重力、跳跃与计时窗口</div>
            <div class="stage"><b>CDR-022</b>冲刺、墙滑/墙跳、攀爬与体力</div>
            <div class="stage"><b>CDR-030</b>棋盘格上传、提交、完成等待与像素变化诊断</div>
            <div class="stage"><b>CDR-031</b>匿名表面几何、DPI、可见性与速度</div>
            <div class="stage"><b>CDR-032</b>目录动画逐 tick 选帧、透明画布合成与离线 Present</div>
            <div class="stage"><b>CDR-040</b>Theo 拿起、携带、投掷、碰撞反弹与故障隔离</div>
            <div class="stage"><b>CDR-041</b>Glider 拿起、缓降请求、投掷、滑落反弹与故障隔离</div>
          </div>
          <div class="gap-note"><b>编号说明：</b>CDR-017、CDR-018、CDR-019 当前未分配，是阶段间保留编号，不代表任务或成果丢失。</div>
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
          <h2>CDR-021 Normal / Jump 轨迹</h2>
          <p>合成输入先向右加速 6 tick，再起跳并先长按后释放。最大跑速到达：<span class="ok">{{player.MaxRunReached}}</span>；Jumped 事件：{{player.JumpEventCount}}；重复运行：<span class="ok">{{(player.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Player x,y</th><th>Speed x,y</th><th>Grounded</th><th>Coyote/Buffer/Variable</th><th>事件</th></tr></thead><tbody>{{playerRows}}</tbody></table>
          <h2>CDR-022 Dash / Wall / Climb 轨迹</h2>
          <p>合成角色先贴右墙下滑并蹬墙，随后向右冲刺撞墙，再抓墙向上攀爬。DashStarted={{traversal.DashStartedCount}}，WallSlideStarted={{traversal.WallSlideStartedCount}}，WallJumped={{traversal.WallJumpedCount}}，ClimbStarted={{traversal.ClimbStartedCount}}；重复运行：<span class="ok">{{(traversal.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>State</th><th>Player x,y</th><th>Speed x,y</th><th>Dashes</th><th>Stamina</th><th>事件</th></tr></thead><tbody>{{traversalRows}}</tbody></table>
          <h2>CDR-030 合成呈现健康链</h2>
          <div class="layout">
            <div class="present-canvas">{{presentationCells}}</div>
            <dl class="facts">
              <dt>输入</dt><dd>程序生成 premultiplied BGRA32 棋盘格</dd>
              <dt>匿名虚拟坐标</dt><dd>{{presentation.Geometry.VirtualLeft}}, {{presentation.Geometry.VirtualTop}}</dd>
              <dt>DPI / 输出像素</dt><dd>{{presentation.Geometry.DpiX}} × {{presentation.Geometry.DpiY}} / {{presentation.Geometry.PixelWidth}} × {{presentation.Geometry.PixelHeight}}</dd>
              <dt>Present 调用</dt><dd>{{presentation.PresentCalls}}</dd>
              <dt>检测到像素变化</dt><dd class="ok">{{presentation.PixelsChangedCount}}</dd>
              <dt>隐藏原生冒烟测试</dt><dd class="ok">{{(presentation.NativeHiddenSmokeValidatedSeparately ? "由验证脚本单独执行" : "未执行")}}</dd>
              <dt>人眼可见</dt><dd>未声明；human_visible=false</dd>
            </dl>
          </div>
          <table><thead><tr><th>Sequence</th><th>Event ID</th><th>Stage</th><th>Outcome</th><th>Backend</th></tr></thead><tbody>{{presentationRows}}</tbody></table>
          <h2>CDR-031 匿名桌面几何轨迹（程序生成）</h2>
          <p>这里的两个表面完全由程序生成，用来演示负坐标、DPI、隐藏过滤和速度计算；不包含真实窗口标题、内容、截图、输入或句柄。</p>
          <table><thead><tr><th>Snapshot</th><th>匿名 ID</th><th>位置 / 大小</th><th>DPI</th><th>速度 px/s</th></tr></thead><tbody>{{desktopRows}}</tbody></table>
          <h2>CDR-032 已验证目录 → 离线动画呈现</h2>
          <p>player/idle 的 2 个程序生成帧来自本页同一套 Atlas/XML 解析与规范化目录。以固定 60 Hz 连续输入 {{animationPresentation.Rows.Count}} 个 tick，实际 Present {{animationPresentation.PresentedCount}} 次，检测到 {{animationPresentation.FrameChangedCount}} 次合成帧变化；重复运行：<span class="ok">{{(animationPresentation.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Animation</th><th>Frame</th><th>Atlas entry</th><th>合成帧 SHA-256</th><th>Pixels changed</th></tr></thead><tbody>{{animationRows}}</tbody></table>
          <h2>CDR-040 Theo Crystal 纯离线交互轨迹</h2>
          <p>Theo 使用程序生成的持有者快照和墙/地面：先被拿起并跟随移动，再继承持有者的 LiftSpeed 向右投掷，撞墙反弹并最终落地。共 {{theo.Rows.Count}} 个固定 tick；Pickup={{theo.PickupCount}}，Throw={{theo.ThrowCount}}，水平反弹={{theo.HorizontalBounceCount}}，落地/落地反弹={{theo.LandingCount}}；重复运行：<span class="ok">{{(theo.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Theo state</th><th>位置 x,y</th><th>速度 x,y</th><th>持有者</th><th>事件</th></tr></thead><tbody>{{theoRows}}</tbody></table>
          <h2>CDR-041 Glider 纯离线交互轨迹</h2>
          <p>Glider 使用程序生成的 Player、持有者快照和墙/地面：被拿起后对过快下落给出受限请求，下一 tick 由 Player 的通用外部效果入口实际应用，再被投掷、展开缓慢下落、撞墙反弹并落地。共 {{glider.Rows.Count}} 个固定 tick；Pickup={{glider.PickupCount}}，Throw={{glider.ThrowCount}}，缓降请求={{glider.HolderFallLimitedCount}}，Player 实际应用={{glider.PlayerFallLimitAppliedCount}}，水平反弹={{glider.HorizontalBounceCount}}，落地/落地反弹={{glider.LandingCount}}；重复运行：<span class="ok">{{(glider.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Glider state</th><th>位置 x,y</th><th>速度 x,y</th><th>展开</th><th>缓降请求</th><th>Player Y 速度</th><th>Player 已应用</th><th>事件</th></tr></thead><tbody>{{gliderRows}}</tbody></table>
          <div class="limits" id="plain-language-proof">
            <h2>这证明了什么</h2>
            <p class="plain"><strong>简单说：</strong>项目现在已经能把“解析出来的多张角色图片”按照规定的时间顺序连续播放，并把每一张准备好的画面交给渲染模块。</p>
            <ul>
              <li><strong>会按节奏换图：</strong>上面的 8 行表格就是 8 个连续时间点。程序在第 0–2 个时间点使用第一帧，第 3–5 个时间点换成第二帧，第 6–7 个时间点再循环回第一帧。</li>
              <li><strong>会把图片摆正：</strong>程序会读取图片的原点和位置，把它放进透明画布，也支持左右翻转，避免换帧时角色位置无故跳动。</li>
              <li><strong>结果可以重复：</strong>同样的素材目录和时间点会得到同样的帧、同样的像素指纹，不会因为电脑快慢而随机改变。</li>
              <li><strong>不会破坏角色手感：</strong>动画和渲染只接收角色状态的副本。即使显示变慢或失败，也不会反过来改变角色位置、速度、碰撞或体力。</li>
              <li><strong>Theo 已有独立实体逻辑：</strong>上面的轨迹真实运行了拿起、跟随、投掷、重力、摩擦、墙面反弹和落地；Theo 失败不会直接关闭 Player 或其他实体。</li>
              <li><strong>Glider 已有独立实体逻辑：</strong>上面的轨迹真实运行了拿起、携带者缓降请求、投掷、展开、缓慢下落、墙面反弹和落地；Glider 失败不会直接关闭 Player、Theo 或另一个 Glider。</li>
            </ul>
            <p class="plain"><strong>它在项目里的作用：</strong>以前项目只是“已经拿到安全的动画图片”，现在已经接通到“知道当前应该显示哪一张，并把它送去呈现”。这是以后让 Madeline 和交互物品真正动起来所必需的中间环节。</p>
          </div>
          <div class="limits" id="plain-language-limits">
            <h2>仍未证明什么</h2>
            <p class="plain"><strong>简单说：</strong>动画流水线已经会工作，但这还不是“桌面上已经出现原版 Madeline”。</p>
            <ul>
              <li><strong>这次使用的是程序生成测试图：</strong>不是 Celeste 的原版角色图片，也没有把任何商业素材保存进项目。</li>
              <li><strong>Present 不等于肉眼可见：</strong>它只说明渲染后端接收并提交了画面；当前走的是隐藏、离线验证路径，没有把角色窗口显示到真实桌面。</li>
              <li><strong>还没有完整 App 组装：</strong>素材、角色模拟、动画、桌面位置和可见窗口尚未由正式应用统一启动和管理。</li>
              <li><strong>还没有真实交互验收：</strong>没有实时键盘输入、真实桌面观察，也没有检查角色是否能在桌面上持续生成、移动和保持可见。</li>
              <li><strong>CDR-016 只证明格式兼容：</strong>它说明指定正版安装中的素材能被只读解析，不等于这些原版素材已经在本次演示里被连接和显示。</li>
              <li><strong>Theo 手感仍是 partial：</strong>官方公开仓库没有 TheoCrystal 实体源码，因此当前数值是独立设计并由逐 tick 测试固定的离线基线，不宣称与商业发行版逐项完全一致。</li>
              <li><strong>Glider 手感仍是 partial：</strong>官方公开仓库没有发布商业版 Glider 实体行为，因此缓降、投掷和反弹数值是独立设计的确定性基线；Player 已能通过通用效果入口应用缓降，但正式 App 尚未负责持续组装这个交互。</li>
            </ul>
            <p class="plain"><strong>因此当前准确结论是：</strong>项目已经能离线生成并提交正确的动画帧，但尚未取得 <code>HUMAN_VISIBILITY_CONFIRMED</code>，还不能声称角色已经在人眼可见的真实桌面上运行。</p>
          </div>
        </body>
        </html>
        """;
}

static DemoTheo RunSyntheticTheo()
{
    static DemoTheo RunOnce()
    {
        var world = new SimulationWorld();
        var actor = new Actor("demo-theo", 0, 0, 2, 2);
        world.Add(actor);
        world.Add(new Solid("demo-theo-floor", -40, 12, 80, 2));
        world.Add(new Solid("demo-theo-wall", 22, -20, 2, 32));
        var controller = new TheoCrystalController(actor);
        var rows = new List<DemoTheoRow>();

        for (var tick = 0; tick < 36; tick++)
        {
            var input = tick switch
            {
                0 => new TheoCrystalInput(TheoCrystalAction.Pickup, new TheoHolderSnapshot("demo-player", new SimPoint(2, 1), 1)),
                1 => new TheoCrystalInput(TheoCrystalAction.None, new TheoHolderSnapshot("demo-player", new SimPoint(4, 0), 1)),
                2 => new TheoCrystalInput(TheoCrystalAction.None, new TheoHolderSnapshot("demo-player", new SimPoint(6, 0), 1)),
                3 => new TheoCrystalInput(TheoCrystalAction.Throw, new TheoHolderSnapshot("demo-player", new SimPoint(6, 0), 1, new SimVector(30m, -20m))),
                _ => TheoCrystalInput.None
            };
            var snapshot = controller.Step(input, world);
            rows.Add(new DemoTheoRow(
                snapshot.Tick,
                snapshot.State.ToString(),
                snapshot.Position.X,
                snapshot.Position.Y,
                snapshot.Speed.X,
                snapshot.Speed.Y,
                snapshot.HolderId,
                string.Join(", ", snapshot.Events.Select(item => item.EventId))));
        }

        return new DemoTheo(
            rows.AsReadOnly(),
            rows.Count(row => row.Events.Contains(TheoCrystalEventIds.PickedUp, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(TheoCrystalEventIds.Thrown, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(TheoCrystalEventIds.HorizontalBounced, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(TheoCrystalEventIds.Landed, StringComparison.Ordinal) || row.Events.Contains(TheoCrystalEventIds.Bounced, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(TheoCrystalEventIds.Squished, StringComparison.Ordinal)),
            false);
    }

    var first = RunOnce();
    var second = RunOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first.Rows) == JsonSerializer.Serialize(second.Rows) };
}

static DemoGlider RunSyntheticGlider()
{
    static DemoGlider RunOnce()
    {
        var world = new SimulationWorld();
        var playerActor = new Actor("demo-player", 0, 0, 1, 2);
        var actor = new Actor("demo-glider", 0, 0, 2, 2);
        world.Add(playerActor);
        world.Add(actor);
        world.Add(new Solid("demo-glider-floor", -40, 12, 80, 2));
        world.Add(new Solid("demo-glider-wall", 16, -20, 2, 32));
        var controller = new GliderController(actor);
        var player = new PlayerNormalController(playerActor, initialSpeed: new SimVector(0m, 100m));
        var rows = new List<DemoGliderRow>();
        GliderHolderEffect? previousEffect = null;

        for (var tick = 0; tick < 48; tick++)
        {
            GliderSnapshot? snapshot = null;
            PlayerNormalSnapshot? playerSnapshot = null;
            world.Step(current =>
            {
                var effects = previousEffect is null
                    ? PlayerExternalEffects.None
                    : new PlayerExternalEffects(previousEffect.MaximumFallSpeed);
                playerSnapshot = player.Update(new PlayerInput(0, 0, false, false), effects, current);
                var holder = new GliderHolderSnapshot(
                    "demo-player",
                    new SimPoint(playerSnapshot.Position.X + 2, playerSnapshot.Position.Y),
                    playerSnapshot.Facing,
                    tick == 2 ? new SimVector(20m, -10m) : SimVector.Zero,
                    playerSnapshot.Speed.Y);
                var input = tick switch
                {
                    0 => new GliderInput(GliderAction.Pickup, holder),
                    1 => new GliderInput(GliderAction.None, holder),
                    2 => new GliderInput(GliderAction.Throw, holder),
                    _ => GliderInput.None
                };
                snapshot = controller.Update(input, current);
            });
            previousEffect = snapshot!.HolderEffect;
            rows.Add(new DemoGliderRow(
                snapshot.Tick,
                snapshot.State.ToString(),
                snapshot.Position.X,
                snapshot.Position.Y,
                snapshot.Speed.X,
                snapshot.Speed.Y,
                snapshot.IsOpen,
                snapshot.HolderEffect?.LimitRequired ?? false,
                playerSnapshot!.Speed.Y,
                playerSnapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalFallSpeedLimited),
                string.Join(", ", snapshot.Events.Select(item => item.EventId))));
        }

        return new DemoGlider(
            rows.AsReadOnly(),
            rows.Count(row => row.Events.Contains(GliderEventIds.PickedUp, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(GliderEventIds.Thrown, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(GliderEventIds.HolderFallLimited, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(GliderEventIds.HorizontalBounced, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(GliderEventIds.Landed, StringComparison.Ordinal) || row.Events.Contains(GliderEventIds.Bounced, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(GliderEventIds.Destroyed, StringComparison.Ordinal)),
            rows.Count(row => row.PlayerFallLimitApplied),
            false);
    }

    var first = RunOnce();
    var second = RunOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first.Rows) == JsonSerializer.Serialize(second.Rows) };
}

static DemoAnimationPresentation RunSyntheticAnimationPresentation(NormalizedAssetCatalog catalog)
{
    static DemoAnimationPresentation RunOnce(NormalizedAssetCatalog source)
    {
        var entity = source.Entities.Single(item => item.EntityId == "player");
        var animationEvents = new List<AnimationDiagnosticEvent>();
        var renderEvents = new List<RenderDiagnosticEvent>();
        using var presenter = new RenderPresenter(new DemoPresenterBackendFactory(), renderEvents.Add, Guid.Empty);
        presenter.Initialize(new PresentationGeometry(0, 0, 12, 12, 96, 96));
        var offline = new OfflineAnimationPresenter(entity, presenter, 12, 12, animationEvents.Add);
        var rows = new List<DemoAnimationRow>();
        for (var tick = 0; tick < 8; tick++)
        {
            var result = offline.Present(new AnimationTickInput(tick, "idle", 6, 10));
            if (!result.Presentation.Succeeded) throw new InvalidOperationException("Offline generated animation presentation failed.");
            rows.Add(new DemoAnimationRow(
                tick,
                result.Animation.AnimationId,
                result.Animation.FrameIndex,
                result.Animation.AtlasEntryId,
                result.ComposedFrame.ContentSha256,
                result.Presentation.PixelsChanged));
        }
        return new DemoAnimationPresentation(
            rows.AsReadOnly(),
            renderEvents.Count(item => item.EventId == "PRESENTER_PRESENTED"),
            renderEvents.Count(item => item.EventId == "PRESENTED_PIXELS_CHANGED"),
            false,
            animationEvents.AsReadOnly());
    }

    var first = RunOnce(catalog);
    var second = RunOnce(catalog);
    var deterministic = JsonSerializer.Serialize(first.Rows) == JsonSerializer.Serialize(second.Rows);
    return first with { DeterministicReplay = deterministic };
}

static DemoDesktop RunSyntheticDesktop()
{
    var first = new[]
    {
        new DesktopSurfaceCandidate(new Guid("11111111-1111-1111-1111-111111111111"), new DesktopRect(-900, 80, 640, 480), 144, true, false),
        new DesktopSurfaceCandidate(new Guid("22222222-2222-2222-2222-222222222222"), new DesktopRect(200, 120, 500, 360), 96, true, false),
        new DesktopSurfaceCandidate(new Guid("33333333-3333-3333-3333-333333333333"), new DesktopRect(0, 0, 100, 100), 96, false, false)
    };
    var second = new[]
    {
        new DesktopSurfaceCandidate(first[0].SessionToken, new DesktopRect(-840, 100, 640, 480), first[0].Dpi, true, false),
        first[1]
    };
    var tracker = new DesktopTracker(new DemoDesktopProvider(first, second));
    return new DemoDesktop([
        tracker.Capture(TimeSpan.FromSeconds(1)),
        tracker.Capture(TimeSpan.FromSeconds(1.5))]);
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

static DemoPlayer RunSyntheticPlayer()
{
    var first = RunPlayerOnce();
    var second = RunPlayerOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second) };
}

static DemoPlayer RunPlayerOnce()
{
    var world = new SimulationWorld();
    var floor = new Solid("player-floor", -32, 11, 160, 4);
    var actor = new Actor("player", 0, 0, 8, 11);
    world.Add(floor);
    world.Add(actor);
    var controller = new PlayerNormalController(actor);
    var rows = new List<DemoPlayerRow>();
    for (var index = 0; index < 24; index++)
    {
        var jumpPressed = index == 6;
        var jumpHeld = index is >= 6 and <= 11;
        var input = new PlayerInput(1, 0, jumpPressed, jumpHeld);
        var snapshot = controller.Step(input, world);
        rows.Add(new DemoPlayerRow(
            snapshot.Tick,
            snapshot.Position.X,
            snapshot.Position.Y,
            snapshot.Speed.X,
            snapshot.Speed.Y,
            snapshot.Grounded,
            snapshot.CoyoteTicks,
            snapshot.JumpBufferTicks,
            snapshot.VariableJumpTicks,
            string.Join(", ", snapshot.Events.Select(item => item.Kind.ToString()))));
    }

    return new DemoPlayer(
        rows.AsReadOnly(),
        rows.Any(row => row.SpeedX == NormalJumpTuning.ReferencePartial.MaxRun),
        rows.Count(row => row.Events.Contains(nameof(PlayerNormalEventKind.Jumped), StringComparison.Ordinal)),
        rows.Min(row => row.Y),
        false);
}

static DemoTraversal RunSyntheticTraversal()
{
    var first = RunTraversalOnce();
    var second = RunTraversalOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second) };
}

static DemoTraversal RunTraversalOnce()
{
    var world = new SimulationWorld();
    var actor = new Actor("traversal-player", 0, 0, 8, 11);
    var wall = new Solid("traversal-wall", 8, -200, 4, 400);
    world.Add(actor);
    world.Add(wall);
    var controller = new PlayerTraversalController(actor, initialSpeed: new SimVector(0m, 60m));
    var rows = new List<DemoTraversalRow>();

    for (var index = 0; index < 24; index++)
    {
        var input = index switch
        {
            0 => new PlayerInput(1, 0, false, false),
            1 => new PlayerInput(1, 0, true, true),
            2 => new PlayerInput(1, 0, false, false, dashPressed: true),
            >= 11 => new PlayerInput(1, -1, false, false, grabHeld: true),
            _ => new PlayerInput(0, 0, false, false)
        };
        var snapshot = controller.Step(input, world);
        rows.Add(new DemoTraversalRow(
            snapshot.Tick,
            snapshot.State.ToString(),
            snapshot.Position.X,
            snapshot.Position.Y,
            snapshot.Speed.X,
            snapshot.Speed.Y,
            snapshot.Dashes,
            snapshot.Stamina,
            string.Join(", ", snapshot.Events.Select(item => item.Kind.ToString()))));
    }

    return new DemoTraversal(
        rows.AsReadOnly(),
        rows.Count(row => row.Events.Contains(nameof(PlayerTraversalEventKind.DashStarted), StringComparison.Ordinal)),
        rows.Count(row => row.Events.Contains(nameof(PlayerTraversalEventKind.WallSlideStarted), StringComparison.Ordinal)),
        rows.Count(row => row.Events.Contains(nameof(PlayerTraversalEventKind.WallJumped), StringComparison.Ordinal)),
        rows.Count(row => row.Events.Contains(nameof(PlayerTraversalEventKind.ClimbStarted), StringComparison.Ordinal)),
        rows.Min(row => row.Stamina),
        false);
}

static DemoPresentation RunSyntheticPresentation()
{
    var geometry = new PresentationGeometry(-1600, -120, 16, 12, 144, 144);
    var first = CheckerboardFrameFactory.Create(
        geometry.PixelWidth,
        geometry.PixelHeight,
        3,
        Bgra32Color.FromStraightAlpha(220, 70, 45, 208),
        Bgra32Color.FromStraightAlpha(25, 145, 235, 160));
    var second = CheckerboardFrameFactory.Create(
        geometry.PixelWidth,
        geometry.PixelHeight,
        3,
        Bgra32Color.FromStraightAlpha(35, 205, 125, 192),
        Bgra32Color.FromStraightAlpha(215, 65, 190, 144));
    var events = new List<RenderDiagnosticEvent>();
    using var presenter = new RenderPresenter(new DemoPresenterBackendFactory(), events.Add, Guid.Empty);
    presenter.Initialize(geometry);
    var firstResult = presenter.Present(first);
    var secondResult = presenter.Present(second);
    var repeatedResult = presenter.Present(second);
    if (!firstResult.Succeeded || !secondResult.Succeeded || !repeatedResult.Succeeded ||
        !firstResult.PixelsChanged || !secondResult.PixelsChanged || repeatedResult.PixelsChanged)
    {
        throw new InvalidOperationException("The generated presentation health chain did not classify pixel changes correctly.");
    }

    return new DemoPresentation(
        geometry,
        second,
        events.AsReadOnly(),
        3,
        events.Count(item => item.EventId == "PRESENTED_PIXELS_CHANGED"),
        first.ContentSha256,
        second.ContentSha256,
        NativeHiddenSmokeValidatedSeparately: true);
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

internal sealed record DemoPlayer(
    IReadOnlyList<DemoPlayerRow> Rows,
    bool MaxRunReached,
    int JumpEventCount,
    int MinimumY,
    bool DeterministicReplay);

internal sealed record DemoPlayerRow(
    long Tick,
    int X,
    int Y,
    decimal SpeedX,
    decimal SpeedY,
    bool Grounded,
    int CoyoteTicks,
    int BufferTicks,
    int VariableTicks,
    string Events);

internal sealed record DemoTraversal(
    IReadOnlyList<DemoTraversalRow> Rows,
    int DashStartedCount,
    int WallSlideStartedCount,
    int WallJumpedCount,
    int ClimbStartedCount,
    decimal MinimumStamina,
    bool DeterministicReplay);

internal sealed record DemoTraversalRow(
    long Tick,
    string State,
    int X,
    int Y,
    decimal SpeedX,
    decimal SpeedY,
    int Dashes,
    decimal Stamina,
    string Events);

internal sealed record DemoPresentation(
    PresentationGeometry Geometry,
    Bgra32Frame SecondFrame,
    IReadOnlyList<RenderDiagnosticEvent> Events,
    int PresentCalls,
    int PixelsChangedCount,
    string FirstFingerprint,
    string SecondFingerprint,
    bool NativeHiddenSmokeValidatedSeparately);

internal sealed record DemoDesktop(IReadOnlyList<DesktopSnapshot> Snapshots);

internal sealed record DemoAnimationPresentation(
    IReadOnlyList<DemoAnimationRow> Rows,
    int PresentedCount,
    int FrameChangedCount,
    bool DeterministicReplay,
    IReadOnlyList<AnimationDiagnosticEvent> AnimationEvents);

internal sealed record DemoAnimationRow(
    long Tick,
    string AnimationId,
    int FrameIndex,
    string AtlasEntryId,
    string FrameFingerprint,
    bool PixelsChanged);

internal sealed record DemoTheo(
    IReadOnlyList<DemoTheoRow> Rows,
    int PickupCount,
    int ThrowCount,
    int HorizontalBounceCount,
    int LandingCount,
    int SquishCount,
    bool DeterministicReplay);

internal sealed record DemoTheoRow(
    long Tick,
    string State,
    int X,
    int Y,
    decimal SpeedX,
    decimal SpeedY,
    string? HolderId,
    string Events);

internal sealed record DemoGlider(
    IReadOnlyList<DemoGliderRow> Rows,
    int PickupCount,
    int ThrowCount,
    int HolderFallLimitedCount,
    int HorizontalBounceCount,
    int LandingCount,
    int DestroyCount,
    int PlayerFallLimitAppliedCount,
    bool DeterministicReplay);

internal sealed record DemoGliderRow(
    long Tick,
    string State,
    int X,
    int Y,
    decimal SpeedX,
    decimal SpeedY,
    bool IsOpen,
    bool HolderFallLimitRequired,
    decimal PlayerSpeedY,
    bool PlayerFallLimitApplied,
    string Events);

internal sealed class DemoDesktopProvider(params IReadOnlyList<DesktopSurfaceCandidate>[] captures) : IDesktopSurfaceProvider
{
    private readonly Queue<IReadOnlyList<DesktopSurfaceCandidate>> _captures = new(captures);
    public IReadOnlyList<DesktopSurfaceCandidate> Capture() => _captures.Dequeue();
}

internal sealed class DemoPresenterBackendFactory : IRenderPresenterBackendFactory
{
    public IRenderPresenterBackend Create() => new DemoPresenterBackend();
}

internal sealed class DemoPresenterBackend : IRenderPresenterBackend
{
    private bool _initialized;
    private bool _uploaded;
    private bool _submitted;

    public string Name => "GeneratedFrameContract";

    public void Initialize(PresentationGeometry geometry)
    {
        _initialized = true;
    }

    public void Upload(Bgra32Frame frame)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Demo backend is not initialized.");
        }
        _uploaded = true;
    }

    public void Submit()
    {
        if (!_uploaded)
        {
            throw new InvalidOperationException("Demo frame was not uploaded.");
        }
        _submitted = true;
    }

    public void WaitForPresented()
    {
        if (!_submitted)
        {
            throw new InvalidOperationException("Demo frame was not submitted.");
        }
        _submitted = false;
    }

    public void Dispose()
    {
        _initialized = false;
    }
}
