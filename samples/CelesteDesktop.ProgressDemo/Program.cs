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
using CelesteDesktop.Entity.Spring;
using CelesteDesktop.Entity.Refill;
using CelesteDesktop.Entity.Water;
using CelesteDesktop.Entity.Bumper;
using CelesteDesktop.Entity.Puffer;
using CelesteDesktop.Entity.Seeker;

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
var spring = RunSyntheticSpring();
var refill = RunSyntheticRefill();
var water = RunSyntheticWater();
var bumper = RunSyntheticBumper();
var puffer = RunSyntheticPuffer();
var seeker = RunSyntheticSeeker();
var app = Cdr050Demo.Run();
var host = Cdr051Demo.Run();
var extensionContracts = Cdr060Demo.Run();
var ducking = Cdr075Demo.Run();

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
    demoId = "CDR-075",
    ducking,
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
        "CDR-041 ran generated Glider pickup carry fall-limit throw glide bounce and isolation behavior at fixed tick",
        "CDR-042 ran generated Spring activation retract cooldown reset and target launch effects at fixed tick",
        "CDR-043 ran generated Refill collection resource restoration cooldown respawn and isolation at fixed tick",
        "CDR-044 ran generated Water enter submerged drag buoyancy swim limit exit and isolation behavior at fixed tick",
        "CDR-045 ran generated Bumper circular contact radial launch cooldown rearm fallback and isolation behavior at fixed tick",
        "CDR-046 ran generated Puffer bounded swim warning explosion launch respawn fallback and isolation behavior at fixed tick",
        "CDR-047 ran generated Seeker patrol alert chase windup dash hit wall-stun recovery and isolation behavior at fixed tick",
        "CDR-050 orchestrated lifecycle, one simulation step per App tick, effect routing and presentation isolation offline",
        "CDR-051 scheduled the App at fixed 60 Hz with bounded catch-up, generated input and graceful stop",
        "CDR-060 exchanged bounded generated assets and immutable room descriptions through data-only provider contracts",
        "CDR-071 applied bounded moving-Solid lift velocity to an ordinary generated Player jump",
        "CDR-072 retained and restored generated horizontal wall speed through a fixed four-tick window",
        "CDR-073 corrected a generated upward corner collision by an explicit bounded integer offset",
        "CDR-074 passed upward through generated one-way geometry, landed from above and completed an explicit bounded drop-through"
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
        player.AppliedLiftX,
        player.AppliedLiftY,
        player.LiftEventCount,
        player.RetainedWallSpeed,
        player.InitialWallRetentionTicks,
        player.RestoredWallSpeed,
        player.WallRetainedEventCount,
        player.WallRestoredEventCount,
        player.UpwardCornerCorrectionX,
        player.CornerStartX,
        player.CornerFinalX,
        player.CornerStartY,
        player.CornerFinalY,
        player.CornerCorrectionEventCount,
        player.CornerVerticalSpeedPreserved,
        player.OneWayPassedUpward,
        player.OneWayDropStartY,
        player.OneWayLandingY,
        player.OneWayLandedPlatformId,
        player.OneWayDropStartedCount,
        player.OneWayDropCompletedCount,
        player.OneWayLandingCount,
        player.OneWayDropRearmed,
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
    spring = new
    {
        source = "program-generated-geometry-and-input",
        fidelity = "partial",
        tickCount = spring.Rows.Count,
        spring.ActivationCount,
        spring.LaunchCount,
        spring.ReadyCount,
        spring.PlayerApplicationCount,
        spring.TheoApplicationCount,
        spring.GliderApplicationCount,
        spring.DeterministicReplay,
        rows = spring.Rows
    },
    refill = new
    {
        source = "program-generated-geometry-and-input",
        fidelity = "partial",
        tickCount = refill.Rows.Count,
        refill.CollectionCount,
        refill.RestoreCount,
        refill.RespawnCount,
        refill.PlayerApplicationCount,
        refill.DeterministicReplay,
        rows = refill.Rows
    },
    water = new
    {
        source = "program-generated-geometry-and-input",
        fidelity = "partial",
        tickCount = water.Rows.Count,
        water.EnteredCount,
        water.SubmergedCount,
        water.MotionIssuedCount,
        water.ExitedCount,
        water.PlayerApplicationCount,
        water.DeterministicReplay,
        rows = water.Rows
    },
    bumper = new
    {
        source = "program-generated-geometry-and-input",
        fidelity = "partial",
        tickCount = bumper.Rows.Count,
        bumper.ActivationCount,
        bumper.LaunchCount,
        bumper.ReadyCount,
        bumper.CenterFallbackCount,
        bumper.IgnoredCount,
        bumper.PlayerApplicationCount,
        bumper.DeterministicReplay,
        rows = bumper.Rows
    },
    puffer = new
    {
        source = "program-generated-geometry-and-input",
        fidelity = "partial",
        tickCount = puffer.Rows.Count,
        puffer.SwamCount,
        puffer.TurnedCount,
        puffer.WarningCount,
        puffer.ExplosionCount,
        puffer.LaunchCount,
        puffer.RespawnCount,
        puffer.CenterFallbackCount,
        puffer.IgnoredCount,
        puffer.PlayerApplicationCount,
        puffer.DeterministicReplay,
        rows = puffer.Rows
    },
    seeker = new
    {
        source = "program-generated-geometry-and-input",
        fidelity = "partial",
        tickCount = seeker.Rows.Count,
        seeker.PatrolCount,
        seeker.AlertCount,
        seeker.ChaseStartCount,
        seeker.ChasedCount,
        seeker.WindupCount,
        seeker.DashStartCount,
        seeker.DashedCount,
        seeker.TargetHitCount,
        seeker.WallHitCount,
        seeker.StunnedCount,
        seeker.RecoveredCount,
        seeker.TargetLostCount,
        seeker.DeterministicReplay,
        rows = seeker.Rows
    },
    app = new
    {
        source = "program-generated-input-and-existing-verified-contracts",
        fidelity = "partial",
        tickCount = app.Rows.Count,
        app.SimulationCompletedCount,
        app.EffectRoutedCount,
        app.PresentationCalls,
        app.ComponentDisabledCount,
        app.Paused,
        app.Resumed,
        app.FinalLifecycle,
        app.DeterministicReplay,
        rows = app.Rows
    },
    host = new
    {
        source = "program-generated-clock-and-input",
        fidelity = "offline-host-only",
        normalExecutedTicks = host.NormalCadence.ExecutedTicks,
        normalDroppedIntervals = host.NormalCadence.DroppedIntervals,
        normalDelayCalls = host.NormalCadence.DelayCalls,
        normalFinalLifecycle = host.NormalCadence.FinalLifecycle,
        backlogExecutedTicks = host.BacklogCadence.ExecutedTicks,
        backlogDroppedIntervals = host.BacklogCadence.DroppedIntervals,
        backlogDelayCalls = host.BacklogCadence.DelayCalls,
        backlogFinalLifecycle = host.BacklogCadence.FinalLifecycle,
        backlogEventIds = host.BacklogCadence.EventIds,
        host.DeterministicReplay
    },
    extensionContracts = new
    {
        taskId = "CDR-060",
        source = "synthetic-providers-only",
        fidelity = "contract-only",
        extensionContracts.AssetProviderId,
        extensionContracts.AssetSourceKind,
        extensionContracts.AssetResolutionStatus,
        extensionContracts.AssetBytes,
        extensionContracts.AssetBudgetRejected,
        extensionContracts.WorldProviderId,
        extensionContracts.WorldCount,
        extensionContracts.RoomCount,
        extensionContracts.SolidCount,
        extensionContracts.SpawnCount,
        extensionContracts.EntityCount,
        extensionContracts.EntityKinds,
        extensionContracts.WorldBudgetRejected,
        extensionContracts.DeterministicReplay
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
    BuildHtml(frame, page, sprites, catalog, simulation, player, traversal, presentation, desktop, animationPresentation, theo, glider, spring, refill, water, bumper, puffer, seeker, app, host, extensionContracts, ducking),
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine("DEMO CDR-075 cumulative offline project progress");
Console.WriteLine($"RESULT metadata_pages=1 metadata_entries=3 decoded_pixels=48 sprite_definitions=2 animations=2 catalog_entities=2 catalog_frames=3 decoded_pages=1 simulation_ticks={simulation.Rows.Count} player_ticks={player.Rows.Count} traversal_ticks={traversal.Rows.Count} animation_ticks={animationPresentation.Rows.Count} theo_ticks={theo.Rows.Count} glider_ticks={glider.Rows.Count} spring_ticks={spring.Rows.Count} refill_ticks={refill.Rows.Count} water_ticks={water.Rows.Count} bumper_ticks={bumper.Rows.Count} puffer_ticks={puffer.Rows.Count} seeker_ticks={seeker.Rows.Count} app_ticks={app.Rows.Count} app_simulation_completed={app.SimulationCompletedCount} app_effects_routed={app.EffectRoutedCount} app_component_disabled={app.ComponentDisabledCount} app_replay={app.DeterministicReplay.ToString().ToLowerInvariant()} host_normal_ticks={host.NormalCadence.ExecutedTicks} host_normal_dropped={host.NormalCadence.DroppedIntervals} host_backlog_ticks={host.BacklogCadence.ExecutedTicks} host_backlog_dropped={host.BacklogCadence.DroppedIntervals} host_backlog_event={host.BacklogCadence.EventIds.Contains("APP_HOST_BACKLOG_DROPPED", StringComparison.Ordinal).ToString().ToLowerInvariant()} host_stopped={(host.NormalCadence.FinalLifecycle == "Stopped" && host.BacklogCadence.FinalLifecycle == "Stopped").ToString().ToLowerInvariant()} host_replay={host.DeterministicReplay.ToString().ToLowerInvariant()} provider_asset_bytes={extensionContracts.AssetBytes} provider_asset_budget_rejected={extensionContracts.AssetBudgetRejected.ToString().ToLowerInvariant()} provider_worlds={extensionContracts.WorldCount} provider_rooms={extensionContracts.RoomCount} provider_solids={extensionContracts.SolidCount} provider_spawns={extensionContracts.SpawnCount} provider_entities={extensionContracts.EntityCount} provider_world_budget_rejected={extensionContracts.WorldBudgetRejected.ToString().ToLowerInvariant()} provider_replay={extensionContracts.DeterministicReplay.ToString().ToLowerInvariant()} human_visible=false commercial_bytes=0");
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
        return Path.GetFullPath(Path.Combine("artifacts", "cdr-060-demo"));
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
    DemoGlider glider,
    DemoSpring spring,
    DemoRefill refill,
    DemoWater water,
    DemoBumper bumper,
    DemoPuffer puffer,
    DemoSeeker seeker,
    DemoApp app,
    DemoHeadlessHost host,
    DemoExtensionContracts extensionContracts,
    DuckingDemoResult ducking)
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

    var duckRows = new StringBuilder();
    foreach (var row in ducking.Rows)
    {
        duckRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(row.X).Append(',').Append(row.Y)
            .Append("</td><td>").Append(row.Height)
            .Append("</td><td>").Append(row.Bottom)
            .Append("</td><td>").Append(row.Ducking)
            .Append("</td><td>").Append(decimal.Round(row.SpeedX, 3))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.BlockingSolidId ?? "-"))
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

    var springRows = new StringBuilder();
    foreach (var row in spring.Rows)
    {
        springRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(row.RetractedTicks).Append('/').Append(row.CooldownTicks)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.TargetId ?? "-"))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.TargetKind ?? "-"))
            .Append("</td><td>").Append(row.PlayerSpeedX).Append(',').Append(row.PlayerSpeedY)
            .Append("</td><td>").Append(row.TheoSpeedX).Append(',').Append(row.TheoSpeedY)
            .Append("</td><td>").Append(row.GliderSpeedX).Append(',').Append(row.GliderSpeedY)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var refillRows = new StringBuilder();
    foreach (var row in refill.Rows)
    {
        refillRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(row.RespawnTicks)
            .Append("</td><td>").Append(row.PlayerDashes)
            .Append("</td><td>").Append(row.PlayerStamina)
            .Append("</td><td>").Append(row.PlayerApplied)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var waterRows = new StringBuilder();
    foreach (var row in water.Rows)
    {
        waterRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Occupants))
            .Append("</td><td>").Append(row.MoveX).Append(',').Append(row.MoveY)
            .Append("</td><td>").Append(decimal.Round(row.PlayerSpeedX, 3)).Append(',').Append(decimal.Round(row.PlayerSpeedY, 3))
            .Append("</td><td>").Append(row.PlayerApplied)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var bumperRows = new StringBuilder();
    foreach (var row in bumper.Rows)
    {
        bumperRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(row.CooldownTicks)
            .Append("</td><td>").Append(row.ContactX?.ToString() ?? "-").Append(',').Append(row.ContactY?.ToString() ?? "-")
            .Append("</td><td>").Append(decimal.Round(row.DirectionX ?? 0m, 3)).Append(',').Append(decimal.Round(row.DirectionY ?? 0m, 3))
            .Append("</td><td>").Append(decimal.Round(row.PlayerSpeedX, 3)).Append(',').Append(decimal.Round(row.PlayerSpeedY, 3))
            .Append("</td><td>").Append(row.PlayerApplied)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.OtherBumperState))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var pufferRows = new StringBuilder();
    foreach (var row in puffer.Rows)
    {
        pufferRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(decimal.Round(row.CenterX, 3)).Append(',').Append(decimal.Round(row.CenterY, 3))
            .Append("</td><td>").Append(row.Direction)
            .Append("</td><td>").Append(row.WarningTicks).Append('/').Append(row.RespawnTicks)
            .Append("</td><td>").Append(row.ContactX?.ToString() ?? "-").Append(',').Append(row.ContactY?.ToString() ?? "-")
            .Append("</td><td>").Append(decimal.Round(row.PlayerSpeedX, 3)).Append(',').Append(decimal.Round(row.PlayerSpeedY, 3))
            .Append("</td><td>").Append(row.PlayerApplied)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.OtherPufferState))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var seekerRows = new StringBuilder();
    foreach (var row in seeker.Rows)
    {
        seekerRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.State))
            .Append("</td><td>").Append(decimal.Round(row.CenterX, 3)).Append(',').Append(decimal.Round(row.CenterY, 3))
            .Append("</td><td>").Append(row.StateTicks).Append('/').Append(row.LostSightTicks)
            .Append("</td><td>").Append(row.TargetX?.ToString() ?? "-").Append(',').Append(row.TargetY?.ToString() ?? "-")
            .Append("</td><td>").Append(row.WallCollision)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.HitTargetId ?? "-"))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.OtherSeekerState))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.Events))
            .Append("</td></tr>");
    }

    var appRows = new StringBuilder();
    foreach (var row in app.Rows)
    {
        appRows.Append("<tr><td>").Append(row.Tick)
            .Append("</td><td>").Append(row.WorldTick)
            .Append("</td><td>").Append(row.PlayerX).Append(',').Append(row.PlayerY)
            .Append("</td><td>").Append(row.PlayerSpeedX).Append(',').Append(row.PlayerSpeedY)
            .Append("</td><td>").Append(WebUtility.HtmlEncode(row.EventIds))
            .Append("</td><td>").Append(WebUtility.HtmlEncode(string.IsNullOrEmpty(row.DisabledComponents) ? "-" : row.DisabledComponents))
            .Append("</td></tr>");
    }

    return $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>CelesteDesktopRuntime CDR-075 进度演示</title>
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
          <h1>CDR-075 累计项目进度演示</h1>
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
            <div class="stage"><b>CDR-042</b>Spring 激活、压缩、冷却、复位与目标发射</div>
            <div class="stage"><b>CDR-043</b>Refill 收集、冲刺/体力恢复、冷却与重生</div>
            <div class="stage"><b>CDR-044</b>Water 进入/浸没/离开、阻力、浮力、游动与限速</div>
            <div class="stage"><b>CDR-045</b>Bumper 圆形接触、径向弹飞、冷却、重新武装与隔离</div>
            <div class="stage"><b>CDR-046</b>Puffer 游动、预警、爆炸弹射、冷却重生与隔离</div>
            <div class="stage"><b>CDR-047</b>Seeker 巡逻、发现、追逐、冲刺、撞墙眩晕与恢复</div>
            <div class="stage"><b>CDR-050</b>App 统一启动、逐 tick 编排、效果路由、暂停恢复与故障隔离</div>
            <div class="stage"><b>CDR-051</b>无界面宿主固定 60 Hz 调度、有限追赶、取消与可靠退出</div>
            <div class="stage"><b>CDR-060</b>纯数据素材提供器与不可变世界内容提供器合同</div>
            <div class="stage"><b>CDR-070</b>本地行为参考建立器；反编译内容只留在 Git 忽略缓存，不进入本演示或产品运行时</div>
            <div class="stage"><b>CDR-071</b>移动 Solid 起跳时的有界水平/向上速度继承与诊断</div>
            <div class="stage"><b>CDR-072</b>横向撞墙速度的 4 tick 保留、恢复、取消与过期</div>
            <div class="stage"><b>CDR-073</b>向上擦碰平台边角时，在 4 像素内做可诊断的横向修正</div>
            <div class="stage"><b>CDR-074</b>单向平台从下穿过、从上落地和明确的向下穿透状态</div>
            <div class="stage"><b>CDR-075</b>角色下蹲滑进矮处，头顶没空间时保持蹲下，空间恢复后安全起身</div>
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
          <p>合成输入先向右加速 6 tick，再起跳并先长按后释放。最大跑速到达：<span class="ok">{{player.MaxRunReached}}</span>；Jumped 事件：{{player.JumpEventCount}}；重复运行：<span class="ok">{{(player.DeterministicReplay ? "完全一致" : "不一致")}}</span>。CDR-071 另用高速右移并上升的平台执行一次普通跳跃，实际继承速度为 ({{player.AppliedLiftX}}, {{player.AppliedLiftY}})，LiftVelocityApplied={{player.LiftEventCount}}。CDR-072 再让角色以 {{player.RetainedWallSpeed}} 的速度撞墙，保留窗口={{player.InitialWallRetentionTicks}} tick；墙移开后恢复速度={{player.RestoredWallSpeed}}，WallSpeedRetained/Restored={{player.WallRetainedEventCount}}/{{player.WallRestoredEventCount}}。CDR-073 用程序生成的平台边角阻挡上升路径；角色从 ({{player.CornerStartX}},{{player.CornerStartY}}) 自动横移 {{player.UpwardCornerCorrectionX}} 像素到 ({{player.CornerFinalX}},{{player.CornerFinalY}})，UpwardCornerCorrected={{player.CornerCorrectionEventCount}}，上升速度继续保留={{player.CornerVerticalSpeedPreserved}}。CDR-074 另用两层程序生成单向平台：从下方上升可穿过={{player.OneWayPassedUpward}}；角色从 y={{player.OneWayDropStartY}} 明确向下穿透上层，在 y={{player.OneWayLandingY}} 落到 {{player.OneWayLandedPlatformId}}；开始/完成/落地事件={{player.OneWayDropStartedCount}}/{{player.OneWayDropCompletedCount}}/{{player.OneWayLandingCount}}，到下一层后可再次穿透={{player.OneWayDropRearmed}}。</p>
          <table><thead><tr><th>Tick</th><th>Player x,y</th><th>Speed x,y</th><th>Grounded</th><th>Coyote/Buffer/Variable</th><th>事件</th></tr></thead><tbody>{{playerRows}}</tbody></table>
          <section id="duck-clearance-demo">
          <h2>CDR-075 蹲下进入矮处，确认头顶有空间再起身</h2>
          <p>角色原本高 {{ducking.StandingHeight}} 像素，按住下蹲后变成 {{ducking.DuckHeight}} 像素，利用原有速度滑进矮通道。脚底始终停在 y=11：{{ducking.FeetPreserved}}。第 7、8 tick 松开下蹲，但头顶仍被 duck-low-ceiling 挡住，所以继续蹲着。第 9 tick 移开天花板，角色安全恢复站立：{{ducking.RestoredStanding}}。全程没有钻进障碍物：{{ducking.NeverOverlapped}}；开始下蹲/起身受阻/成功起身={{ducking.StartedCount}}/{{ducking.BlockedCount}}/{{ducking.CompletedCount}}；重复运行一致={{ducking.DeterministicReplay}}。</p>
          <p>这为矮通道中的身体尺寸和安全起身提供基础；下蹲不是瞬移，也不会把脚底挪走。当前仍是程序生成碰撞几何，没有真人操作或角色动画，冲刺和攀爬中的下蹲联动尚未校准。</p>
          <table><thead><tr><th>时间点 tick</th><th>左上角 x,y</th><th>身体高度</th><th>脚底 y</th><th>蹲着</th><th>横向速度</th><th>阻止起身的障碍</th><th>发生了什么</th></tr></thead><tbody>{{duckRows}}</tbody></table>
          </section>
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
          <h2>CDR-042 Spring 纯离线交互轨迹</h2>
          <p>同一个程序生成的向上 Spring 依次接触 Player、Theo 和 Glider。每次接触都会输出带目标身份的发射效果，目标控制器在同一个固定 tick 真实应用速度；Spring 随后经历压缩、冷却和自动复位。共 {{spring.Rows.Count}} 个固定 tick；激活={{spring.ActivationCount}}，发射={{spring.LaunchCount}}，复位={{spring.ReadyCount}}，Player/Theo/Glider 实际应用={{spring.PlayerApplicationCount}}/{{spring.TheoApplicationCount}}/{{spring.GliderApplicationCount}}；重复运行：<span class="ok">{{(spring.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Spring state</th><th>压缩/冷却剩余</th><th>目标</th><th>目标类型</th><th>Player 速度</th><th>Theo 速度</th><th>Glider 速度</th><th>Spring 事件</th></tr></thead><tbody>{{springRows}}</tbody></table>
          <h2>CDR-043 Refill 纯离线交互轨迹</h2>
          <p>程序先把 Player 的冲刺次数和体力降到不足，再让 Player 接触 Refill。Refill 只输出带目标身份的资源恢复效果，由 Player 在同一个固定 tick 自己应用；Refill 随后进入冷却并自动重生。共 {{refill.Rows.Count}} 个固定 tick；收集={{refill.CollectionCount}}，恢复请求={{refill.RestoreCount}}，重生={{refill.RespawnCount}}，Player 实际应用={{refill.PlayerApplicationCount}}；重复运行：<span class="ok">{{(refill.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Refill state</th><th>重生剩余</th><th>Player 冲刺</th><th>Player 体力</th><th>Player 已应用</th><th>Refill 事件</th></tr></thead><tbody>{{refillRows}}</tbody></table>
          <h2>CDR-044 Water 纯离线交互轨迹</h2>
          <p>程序生成一个矩形水体和 Player 接触。Player 先以较快速度进入，水体逐 tick 输出带目标身份的阻力与浮力效果；中段加入方向游动和第二个合成漂浮目标，随后离开并再次进入。共 {{water.Rows.Count}} 个固定 tick；进入={{water.EnteredCount}}，浸没事实={{water.SubmergedCount}}，运动请求={{water.MotionIssuedCount}}，离开={{water.ExitedCount}}，Player 实际应用={{water.PlayerApplicationCount}}；重复运行：<span class="ok">{{(water.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Water state</th><th>浸没目标</th><th>游动 X,Y</th><th>Player 速度</th><th>Player 已应用</th><th>Water 事件</th></tr></thead><tbody>{{waterRows}}</tbody></table>
          <h2>CDR-045 Bumper 纯离线交互轨迹</h2>
          <p>程序生成一个圆形 Bumper、Player 接触点和第二个保持空闲的 Bumper。Player 依次从右侧、斜下方、中心重合位置和左侧接触；每次有效接触都由 Bumper 发出径向速度，再由 Player 自己实际应用。共 {{bumper.Rows.Count}} 个固定 tick；激活={{bumper.ActivationCount}}，弹飞请求={{bumper.LaunchCount}}，冷却完成={{bumper.ReadyCount}}，中心回退={{bumper.CenterFallbackCount}}，忽略={{bumper.IgnoredCount}}，Player 实际应用={{bumper.PlayerApplicationCount}}；重复运行：<span class="ok">{{(bumper.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Bumper state</th><th>冷却剩余</th><th>接触点 X,Y</th><th>弹飞方向</th><th>Player 速度</th><th>Player 已应用</th><th>另一个 Bumper</th><th>Bumper 事件</th></tr></thead><tbody>{{bumperRows}}</tbody></table>
          <h2>CDR-046 Puffer 纯离线交互轨迹</h2>
          <p>程序生成一个在左右边界间游动的 Puffer、Player 目标和第二个隔离 Puffer。Player 进入范围后触发固定 tick 预警，Puffer 随后爆炸并发出带目标身份的径向速度，由 Player 自己应用；冷却结束后 Puffer 回到出生点。共 {{puffer.Rows.Count}} 个固定 tick；游动={{puffer.SwamCount}}，转向={{puffer.TurnedCount}}，预警={{puffer.WarningCount}}，爆炸={{puffer.ExplosionCount}}，弹射={{puffer.LaunchCount}}，重生={{puffer.RespawnCount}}，中心回退={{puffer.CenterFallbackCount}}，忽略={{puffer.IgnoredCount}}，Player 实际应用={{puffer.PlayerApplicationCount}}；重复运行：<span class="ok">{{(puffer.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Puffer state</th><th>位置 X,Y</th><th>游动方向</th><th>预警/重生剩余</th><th>目标 X,Y</th><th>Player 速度</th><th>Player 已应用</th><th>另一个 Puffer</th><th>Puffer 事件</th></tr></thead><tbody>{{pufferRows}}</tbody></table>
          <h2>CDR-047 Seeker 纯离线交互轨迹</h2>
          <p>程序生成一个 Seeker、目标位置、墙面碰撞信号和第二个隔离 Seeker。主 Seeker 会巡逻，发现目标后经过预警、追逐和蓄力进入冲刺；第一轮命中目标并输出带身份的命中效果，第二轮撞墙进入眩晕，随后回到出生点恢复；第三轮展示丢失目标后返回巡逻。共 {{seeker.Rows.Count}} 个固定 tick；巡逻={{seeker.PatrolCount}}，发现={{seeker.AlertCount}}，开始追逐={{seeker.ChaseStartCount}}，追逐步进={{seeker.ChasedCount}}，蓄力={{seeker.WindupCount}}，开始冲刺={{seeker.DashStartCount}}，冲刺步进={{seeker.DashedCount}}，命中={{seeker.TargetHitCount}}，撞墙={{seeker.WallHitCount}}，眩晕={{seeker.StunnedCount}}，恢复={{seeker.RecoveredCount}}，目标丢失={{seeker.TargetLostCount}}；重复运行：<span class="ok">{{(seeker.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>Tick</th><th>Seeker state</th><th>位置 X,Y</th><th>状态/丢失计时</th><th>目标 X,Y</th><th>撞墙</th><th>命中目标</th><th>另一个 Seeker</th><th>Seeker 事件</th></tr></thead><tbody>{{seekerRows}}</tbody></table>
          <h2>CDR-050 App 统一编排与生命周期</h2>
          <p>程序把 Player、全部离线实体和呈现阶段放进同一个 App 会话。连续运行 {{app.Rows.Count}} 个 App tick，模拟完成 {{app.SimulationCompletedCount}} 次、路由效果 {{app.EffectRoutedCount}} 次；中途暂停并恢复一次。第 2 次呈现被故意触发异常后，呈现组件被单独禁用，但模拟继续到 tick 4 并正常停止。重复运行：<span class="ok">{{(app.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>App Tick</th><th>World Tick</th><th>Player 位置</th><th>Player 速度</th><th>结构化事件</th><th>已隔离组件</th></tr></thead><tbody>{{appRows}}</tbody></table>
          <h2>CDR-051 无界面 App 宿主调度</h2>
          <p>正常节奏运行 {{host.NormalCadence.ExecutedTicks}} tick，等待 {{host.NormalCadence.DelayCalls}} 次，丢弃积压 {{host.NormalCadence.DroppedIntervals}}；模拟“电脑短暂卡住”的积压节奏时，只追赶 {{host.BacklogCadence.ExecutedTicks}} tick，并明确丢弃 {{host.BacklogCadence.DroppedIntervals}} 个过期时间片，避免一次卡顿后无限追赶。两次都正常停止，重复运行：<span class="ok">{{(host.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <p>积压保护日志：<code>APP_HOST_BACKLOG_DROPPED</code>。它表示宿主明确丢弃了过期时间片，不表示这些时间片被悄悄执行。</p>
          <table><thead><tr><th>场景</th><th>实际执行 tick</th><th>丢弃过期时间片</th><th>等待次数</th><th>最终状态</th></tr></thead><tbody><tr><td>正常 60 Hz</td><td>{{host.NormalCadence.ExecutedTicks}}</td><td>{{host.NormalCadence.DroppedIntervals}}</td><td>{{host.NormalCadence.DelayCalls}}</td><td>{{host.NormalCadence.FinalLifecycle}}</td></tr><tr><td>积压保护</td><td>{{host.BacklogCadence.ExecutedTicks}}</td><td>{{host.BacklogCadence.DroppedIntervals}}</td><td>{{host.BacklogCadence.DelayCalls}}</td><td>{{host.BacklogCadence.FinalLifecycle}}</td></tr></tbody></table>
          <h2 id="extension-provider-contracts">CDR-060 未来素材与地图扩展接口（程序生成）</h2>
          <p>这里没有读取游戏或 Mod 文件。演示只让两个程序生成提供器通过正式合同交出一份 4 字节测试素材，以及一个含 {{extensionContracts.SolidCount}} 个 Solid、{{extensionContracts.SpawnCount}} 个出生点、{{extensionContracts.EntityCount}} 个受支持实体的不可变房间描述。超出读取/房间预算时均被明确拒绝；重复运行：<span class="ok">{{(extensionContracts.DeterministicReplay ? "完全一致" : "不一致")}}</span>。</p>
          <table><thead><tr><th>提供器</th><th>类型</th><th>成功结果</th><th>预算保护</th></tr></thead><tbody><tr><td>{{extensionContracts.AssetProviderId}}</td><td>{{extensionContracts.AssetSourceKind}}</td><td>{{extensionContracts.AssetBytes}} 字节 / {{extensionContracts.AssetResolutionStatus}}</td><td id="asset-budget-result">{{(extensionContracts.AssetBudgetRejected ? "已拒绝超预算读取" : "未拒绝")}}</td></tr><tr><td>{{extensionContracts.WorldProviderId}}</td><td>不可变世界内容</td><td>{{extensionContracts.WorldCount}} 世界 / {{extensionContracts.RoomCount}} 房间 / {{extensionContracts.EntityKinds}}</td><td id="world-budget-result">{{(extensionContracts.WorldBudgetRejected ? "已拒绝超预算房间" : "未拒绝")}}</td></tr></tbody></table>
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
              <li><strong>Spring 已有独立实体逻辑：</strong>上面的轨迹真实运行了接触激活、压缩、冷却、复位，并把带目标身份的方向速度实际应用到 Player、Theo 和 Glider；一个 Spring 的状态不会改写另一个 Spring。</li>
              <li><strong>Refill 已有独立实体逻辑：</strong>上面的轨迹真实运行了收集、冲刺次数与体力恢复、冷却和重生；恢复效果由 Player 自己应用，一个 Refill 的冷却不会改写另一个 Refill 或 Spring。</li>
              <li><strong>Water 已有独立体积逻辑：</strong>上面的轨迹真实运行了矩形重叠、进入、持续浸没、水平阻力、自然上浮、方向游动、限速、离开和再次进入；Water 发出的速度效果由 Player 自己应用，多个目标按稳定顺序隔离。</li>
              <li><strong>Bumper 已有独立弹飞逻辑：</strong>上面的轨迹真实运行了圆形范围判断、多个方向的径向弹飞、中心重合时固定向上回退、冷却、离开后重新武装和范围外忽略；Player 自己应用速度，另一个 Bumper 始终保持独立。</li>
              <li><strong>Puffer 已有独立游动与爆炸逻辑：</strong>上面的轨迹真实运行了左右游动/转向、接近预警、一次爆炸弹射、中心重合向上回退、冷却重生和范围外忽略；Player 自己应用速度，另一个 Puffer 独立运行。</li>
              <li><strong>Seeker 已有独立追逐状态机：</strong>上面的轨迹真实运行了巡逻、发现目标、预警、追逐、蓄力、定向冲刺、目标命中、撞墙眩晕、恢复和丢失目标；另一个 Seeker 始终独立运行。</li>
              <li><strong>App 已能统一组织离线模块：</strong>一次 App tick 只推进一次模拟，再按明确顺序把实体效果交给 Player/Theo/Glider，最后才把不可变动画帧交给呈现层；暂停、恢复、停止和呈现故障都有可追踪事件。</li>
              <li><strong>App 已有无界面运行节奏：</strong>宿主按固定 60 Hz 请求下一 tick，短暂卡顿时最多有限追赶，过期积压会留下明确记录；取消或达到演示上限后会停止并释放会话。</li>
              <li><strong>未来扩展已有安全插口：</strong>素材来源只能返回有长度和哈希的纯数据，房间只能描述矩形 Solid、出生点和项目支持的实体；路径、文件流、回调、程序集和任意脚本不会进入公共合同。</li>
              <li><strong>单向平台已有确定性规则：</strong>角色可以从下方穿过、从上方落地并稳定站立，也可以进入有时限且有日志的向下穿透状态；普通 Solid 不会因此被穿透。</li>
              <li><strong>角色已有安全下蹲和起身规则：</strong>身体会缩小但脚底不挪动；松开下蹲时，头顶仍有障碍就保持蹲着，障碍移开后才站起来。九步表格记录了开始、两次受阻和成功起身，全程没有进入障碍物。</li>
            </ul>
            <p class="plain"><strong>它在项目里的作用：</strong>以前项目只是“已经拿到安全的动画图片”，现在已经接通到“知道当前应该显示哪一张，并把它送去呈现”。这是以后让 Madeline 和交互物品真正动起来所必需的中间环节。</p>
          </div>
          <div class="limits" id="plain-language-limits">
            <h2>仍未证明什么</h2>
            <p class="plain"><strong>简单说：</strong>动画流水线已经会工作，但这还不是“桌面上已经出现原版 Madeline”。</p>
            <ul>
              <li><strong>这次使用的是程序生成测试图：</strong>不是 Celeste 的原版角色图片，也没有把任何商业素材保存进项目。</li>
              <li><strong>Present 不等于肉眼可见：</strong>它只说明渲染后端接收并提交了画面；当前走的是隐藏、离线验证路径，没有把角色窗口显示到真实桌面。</li>
              <li><strong>还没有可见桌面宿主：</strong>CDR-050 已完成纯离线 App 统一编排，但没有打开窗口、接入实时键盘、读取真实桌面或验证人眼可见。</li>
              <li><strong>单向平台仍是 partial：</strong>当前只验证静态程序生成平台，Normal 下蹲不会改变脚底接触；移动平台、特殊变体、冲刺接触联动和原版完整数值仍未建立。</li>
              <li><strong>下蹲仍是部分校准：</strong>当前只检查 Normal 移动中的身体尺寸和起身空间；冲刺、攀爬中的下蹲联动、原版完整碰撞箱偏移和真人手感尚未验证。</li>
              <li><strong>CDR-051 仍是 Headless：</strong>它只证明后台时钟和退出流程，不是可双击运行的正式桌面角色程序，也没有读取真实键盘、桌面或正版安装。</li>
              <li><strong>CDR-060 不是地图或 Mod 读取器：</strong>它只建立未来接口并用程序数据验证；没有解析 Celeste 地图、扫描 Mod 目录、加载 DLL/Lua/脚本，也没有承诺现有 Mod 兼容。</li>
              <li><strong>还没有真实交互验收：</strong>没有实时键盘输入、真实桌面观察，也没有检查角色是否能在桌面上持续生成、移动和保持可见。</li>
              <li><strong>CDR-016 只证明格式兼容：</strong>它说明指定正版安装中的素材能被只读解析，不等于这些原版素材已经在本次演示里被连接和显示。</li>
              <li><strong>Theo 手感仍是 partial：</strong>官方公开仓库没有 TheoCrystal 实体源码，因此当前数值是独立设计并由逐 tick 测试固定的离线基线，不宣称与商业发行版逐项完全一致。</li>
              <li><strong>Glider 手感仍是 partial：</strong>官方公开仓库没有发布商业版 Glider 实体行为，因此缓降、投掷和反弹数值是独立设计的确定性基线；Player 已能通过通用效果入口应用缓降，但正式 App 尚未负责持续组装这个交互。</li>
              <li><strong>Spring 手感仍是 partial：</strong>四个方向、压缩/冷却周期和发射速度已有确定性测试，但原版商业发行版的完整数值与接触判定尚未建立；当前演示只证明程序生成接触和通用运动效果链。</li>
              <li><strong>Refill 手感仍是 partial：</strong>恢复目标、冷却和重生已有确定性测试，但原版商业发行版的完整接触范围、重生时长和特殊变体尚未建立；当前演示只证明程序生成资源效果链。</li>
              <li><strong>Water 手感仍是 partial：</strong>进入/离开、阻力、浮力、方向游动和限速已有确定性测试，但原版水面、跳出水面、完整接触判定、特殊水体和商业发行版数值尚未建立；当前演示只证明程序生成矩形与运动效果链。</li>
              <li><strong>Bumper 手感仍是 partial：</strong>圆形接触、径向弹飞、冷却和重新武装已有确定性测试，但原版接触体积、弹飞速度、冷却时长、移动轨迹、特殊变体、动画与声音尚未建立；当前演示只证明程序生成接触与通用速度效果链。</li>
              <li><strong>Puffer 手感仍是 partial：</strong>有界游动、预警、爆炸弹射和重生已有确定性测试，但原版游动轨迹、触发/爆炸范围、预警/重生时长、数值、特殊交互、动画与声音尚未建立；当前演示只证明程序生成目标与通用速度效果链。</li>
              <li><strong>Seeker 手感仍是 partial：</strong>巡逻、发现、追逐、蓄力、冲刺、撞墙眩晕和恢复已有确定性测试，但原版寻路、复杂地形回避、完整伤害/反弹交互、数值、动画与声音尚未建立；命中效果目前只作为带目标身份的离线事实输出，尚未接入 Player 死亡或正式 App。</li>
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

static DemoSpring RunSyntheticSpring()
{
    static DemoSpring RunOnce()
    {
        var world = new SimulationWorld();
        var playerActor = new Actor("spring-demo-player", 0, 0, 1, 2);
        var theoActor = new Actor("spring-demo-theo", 20, 0, 2, 2);
        var gliderActor = new Actor("spring-demo-glider", 40, 0, 2, 2);
        world.Add(playerActor);
        world.Add(theoActor);
        world.Add(gliderActor);

        var spring = new SpringController(
            "demo-spring",
            new SimPoint(8, 10),
            SpringOrientation.Up);
        var player = new PlayerNormalController(playerActor, initialSpeed: new SimVector(0m, 80m));
        var theo = new TheoCrystalController(theoActor, initialSpeed: new SimVector(0m, 50m));
        var glider = new GliderController(gliderActor, initialSpeed: new SimVector(0m, 20m));
        var rows = new List<DemoSpringRow>();

        for (var tick = 0; tick < 30; tick++)
        {
            SpringSnapshot? springSnapshot = null;
            PlayerNormalSnapshot? playerSnapshot = null;
            TheoCrystalSnapshot? theoSnapshot = null;
            GliderSnapshot? gliderSnapshot = null;
            world.Step(current =>
            {
                var contact = tick switch
                {
                    0 => new SpringContact("spring-demo-player", SpringTargetKind.Player, new SimPoint(playerActor.X, playerActor.Y), new SimVector(player.SpeedX, player.SpeedY)),
                    10 => new SpringContact("spring-demo-theo", SpringTargetKind.Theo, new SimPoint(theoActor.X, theoActor.Y), new SimVector(theo.SpeedX, theo.SpeedY)),
                    20 => new SpringContact("spring-demo-glider", SpringTargetKind.Glider, new SimPoint(gliderActor.X, gliderActor.Y), new SimVector(glider.SpeedX, glider.SpeedY)),
                    _ => null
                };
                springSnapshot = spring.Update(new SpringInput(contact), current);
                var launch = springSnapshot.LaunchEffect;
                var playerEffects = launch?.TargetKind == SpringTargetKind.Player
                    ? new PlayerExternalEffects(null, launch.Velocity)
                    : PlayerExternalEffects.None;
                playerSnapshot = player.Update(new PlayerInput(0, 0, false, false), playerEffects, current);
                theoSnapshot = theo.Update(
                    TheoCrystalInput.None,
                    launch?.TargetKind == SpringTargetKind.Theo ? launch.Velocity : null,
                    current);
                gliderSnapshot = glider.Update(
                    GliderInput.None,
                    launch?.TargetKind == SpringTargetKind.Glider ? launch.Velocity : null,
                    current);
            });

            rows.Add(new DemoSpringRow(
                springSnapshot!.Tick,
                springSnapshot.State.ToString(),
                springSnapshot.RetractedTicksRemaining,
                springSnapshot.CooldownTicksRemaining,
                springSnapshot.LaunchEffect?.TargetId,
                springSnapshot.LaunchEffect?.TargetKind.ToString(),
                playerSnapshot!.Speed.X,
                playerSnapshot.Speed.Y,
                theoSnapshot!.Speed.X,
                theoSnapshot.Speed.Y,
                gliderSnapshot!.Speed.X,
                gliderSnapshot.Speed.Y,
                playerSnapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied),
                theoSnapshot.Events.Any(item => item.Kind == TheoCrystalEventKind.ExternalVelocityApplied),
                gliderSnapshot.Events.Any(item => item.Kind == GliderEventKind.ExternalVelocityApplied),
                string.Join(", ", springSnapshot.Events.Select(item => item.EventId))));
        }

        return new DemoSpring(
            rows.AsReadOnly(),
            rows.Count(row => row.Events.Contains(SpringEventIds.Activated, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(SpringEventIds.LaunchIssued, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(SpringEventIds.Ready, StringComparison.Ordinal)),
            rows.Count(row => row.PlayerApplied),
            rows.Count(row => row.TheoApplied),
            rows.Count(row => row.GliderApplied),
            false);
    }

    var first = RunOnce();
    var second = RunOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first.Rows) == JsonSerializer.Serialize(second.Rows) };
}

static DemoRefill RunSyntheticRefill()
{
    static DemoRefill RunOnce()
    {
        var world = new SimulationWorld();
        var actor = new Actor("refill-demo-player", 0, 0, 1, 2);
        world.Add(actor);
        var player = new PlayerTraversalController(actor);
        var refill = new RefillController("demo-refill", new SimPoint(8, 10), new RefillTuning(5));
        var rows = new List<DemoRefillRow>();

        for (var tick = 0; tick < 16; tick++)
        {
            RefillSnapshot? refillSnapshot = null;
            PlayerTraversalSnapshot? playerSnapshot = null;
            world.Step(current =>
            {
                var contact = tick is 1 or 9
                    ? new RefillContact(
                        "refill-demo-player",
                        new SimPoint(actor.X, actor.Y),
                        player.Dashes,
                        player.MaxDashes,
                        player.Stamina,
                        player.Tuning.ClimbMaxStamina)
                    : null;
                refillSnapshot = refill.Update(new RefillInput(contact), current);
                var resources = refillSnapshot.RestoreEffect?.Resources;
                if (tick is 0 or 8)
                {
                    resources = new ExternalResourceEffect(0, 25m);
                }
                playerSnapshot = player.Update(
                    new PlayerInput(0, 0, false, false),
                    new PlayerExternalEffects(null, resources: resources),
                    current);
            });

            rows.Add(new DemoRefillRow(
                refillSnapshot!.Tick,
                refillSnapshot.State.ToString(),
                refillSnapshot.RespawnTicksRemaining,
                playerSnapshot!.Dashes,
                playerSnapshot.Stamina,
                refillSnapshot.RestoreEffect is not null &&
                    playerSnapshot.Events.Any(item => item.Kind == PlayerTraversalEventKind.ExternalResourcesApplied),
                string.Join(", ", refillSnapshot.Events.Select(item => item.EventId))));
        }

        return new DemoRefill(
            rows.AsReadOnly(),
            rows.Count(row => row.Events.Contains(RefillEventIds.Collected, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(RefillEventIds.RestoreIssued, StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(RefillEventIds.Respawned, StringComparison.Ordinal)),
            rows.Count(row => row.PlayerApplied),
            false);
    }

    var first = RunOnce();
    var second = RunOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first.Rows) == JsonSerializer.Serialize(second.Rows) };
}

static DemoWater RunSyntheticWater()
{
    static DemoWater RunOnce()
    {
        var world = new SimulationWorld();
        var actor = new Actor("water-demo-player", 1, 1, 1, 2);
        world.Add(actor);
        var player = new PlayerNormalController(actor, initialSpeed: new SimVector(100m, 40m));
        var water = new WaterController(
            "demo-water",
            new SimRect(0, 0, 40, 20),
            new WaterTuning(60m, 60m, 20m, 300m, 240m));
        var rows = new List<DemoWaterRow>();

        for (var tick = 0; tick < 12; tick++)
        {
            WaterSnapshot? waterSnapshot = null;
            PlayerNormalSnapshot? playerSnapshot = null;
            var moveX = tick is >= 3 and <= 5 ? 1 : tick is >= 6 and <= 7 ? -1 : 0;
            var moveY = tick is >= 3 and <= 5 ? -1 : tick is >= 6 and <= 7 ? 1 : 0;
            world.Step(current =>
            {
                var contacts = new List<WaterContact>();
                if (tick is not (8 or 9))
                {
                    contacts.Add(new WaterContact(
                        "water-demo-player",
                        actor.Bounds,
                        new SimVector(player.SpeedX, player.SpeedY),
                        moveX,
                        moveY));
                }
                if (tick is >= 4 and <= 6)
                {
                    contacts.Add(new WaterContact(
                        "generated-floater",
                        new SimRect(20, 4, 2, 2),
                        new SimVector(0m, 20m),
                        0,
                        0));
                }

                waterSnapshot = water.Update(new WaterInput(contacts), current);
                var effect = waterSnapshot.MotionEffects
                    .SingleOrDefault(item => item.TargetId == "water-demo-player");
                playerSnapshot = player.Update(
                    new PlayerInput(0, 0, false, false),
                    new PlayerExternalEffects(null, effect?.Velocity),
                    current);
            });

            rows.Add(new DemoWaterRow(
                waterSnapshot!.Tick,
                waterSnapshot.State.ToString(),
                string.Join(", ", waterSnapshot.Occupants),
                moveX,
                moveY,
                playerSnapshot!.Speed.X,
                playerSnapshot.Speed.Y,
                waterSnapshot.MotionEffects.Any(item => item.TargetId == "water-demo-player") &&
                    playerSnapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied),
                string.Join(", ", waterSnapshot.Events.Select(item => $"{item.EventId}:{item.TargetId ?? "-"}"))));
        }

        return new DemoWater(
            rows.AsReadOnly(),
            rows.Sum(row => CountEvent(row.Events, WaterEventIds.Entered)),
            rows.Sum(row => CountEvent(row.Events, WaterEventIds.Submerged)),
            rows.Sum(row => CountEvent(row.Events, WaterEventIds.MotionIssued)),
            rows.Sum(row => CountEvent(row.Events, WaterEventIds.Exited)),
            rows.Count(row => row.PlayerApplied),
            false);
    }

    static int CountEvent(string events, string eventId) =>
        events.Split(", ", StringSplitOptions.RemoveEmptyEntries)
            .Count(item => item.StartsWith(eventId + ":", StringComparison.Ordinal));

    var first = RunOnce();
    var second = RunOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first.Rows) == JsonSerializer.Serialize(second.Rows) };
}

static DemoBumper RunSyntheticBumper()
{
    static DemoBumper RunOnce()
    {
        var world = new SimulationWorld();
        var actor = new Actor("bumper-demo-player", 0, 0, 1, 1);
        world.Add(actor);
        var player = new PlayerNormalController(actor);
        var bumper = new BumperController(
            "demo-bumper",
            new SimPoint(10, 10),
            new BumperTuning(12, 280m, 3));
        var other = new BumperController(
            "demo-bumper-isolated",
            new SimPoint(100, 100),
            new BumperTuning(12, 280m, 3));
        var rows = new List<DemoBumperRow>();

        for (var tick = 0; tick < 15; tick++)
        {
            var contactPoint = tick switch
            {
                0 => new SimPoint(13, 10),
                4 => new SimPoint(13, 14),
                8 => new SimPoint(10, 10),
                12 => new SimPoint(23, 10),
                14 => new SimPoint(7, 10),
                _ => (SimPoint?)null
            };
            BumperSnapshot? bumperSnapshot = null;
            BumperSnapshot? otherSnapshot = null;
            PlayerNormalSnapshot? playerSnapshot = null;
            world.Step(current =>
            {
                var contact = contactPoint is null
                    ? null
                    : new BumperContact("bumper-demo-player", contactPoint.Value);
                bumperSnapshot = bumper.Update(new BumperInput(contact), current);
                otherSnapshot = other.Update(BumperInput.None, current);
                playerSnapshot = player.Update(
                    new PlayerInput(0, 0, false, false),
                    new PlayerExternalEffects(null, bumperSnapshot.LaunchEffect?.Velocity),
                    current);
            });

            rows.Add(new DemoBumperRow(
                bumperSnapshot!.Tick,
                bumperSnapshot.State.ToString(),
                bumperSnapshot.CooldownTicksRemaining,
                contactPoint?.X,
                contactPoint?.Y,
                bumperSnapshot.LaunchEffect?.Direction.X,
                bumperSnapshot.LaunchEffect?.Direction.Y,
                playerSnapshot!.Speed.X,
                playerSnapshot.Speed.Y,
                bumperSnapshot.LaunchEffect is not null &&
                    playerSnapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied),
                otherSnapshot!.State.ToString(),
                string.Join(", ", bumperSnapshot.Events.Select(item => item.EventId))));
        }

        return new DemoBumper(
            rows.AsReadOnly(),
            rows.Sum(row => CountEvent(row.Events, BumperEventIds.Activated)),
            rows.Sum(row => CountEvent(row.Events, BumperEventIds.LaunchIssued)),
            rows.Sum(row => CountEvent(row.Events, BumperEventIds.Ready)),
            rows.Sum(row => CountEvent(row.Events, BumperEventIds.CenterFallbackUsed)),
            rows.Sum(row => CountEvent(row.Events, BumperEventIds.ContactIgnored)),
            rows.Count(row => row.PlayerApplied),
            false);
    }

    static int CountEvent(string events, string eventId) =>
        events.Split(", ", StringSplitOptions.RemoveEmptyEntries)
            .Count(item => string.Equals(item, eventId, StringComparison.Ordinal));

    var first = RunOnce();
    var second = RunOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first.Rows) == JsonSerializer.Serialize(second.Rows) };
}

static DemoPuffer RunSyntheticPuffer()
{
    static DemoPuffer RunOnce()
    {
        var world = new SimulationWorld();
        var actor = new Actor("puffer-demo-player", 0, 0, 1, 1);
        world.Add(actor);
        var player = new PlayerNormalController(actor);
        var tuning = new PufferTuning(60m, 10, 2, 240m, 3);
        var puffer = new PufferController("demo-puffer", SimVector.Zero, -2m, 2m, 1, tuning);
        var other = new PufferController("demo-puffer-isolated", new SimVector(100m, 100m), 98m, 102m, -1, tuning);
        var rows = new List<DemoPufferRow>();

        for (var tick = 0; tick < 20; tick++)
        {
            var contactPoint = tick switch
            {
                1 => new SimVector(6m, 0m),
                2 => new SimVector(5m, 3m),
                8 => new SimVector(2m, 0m),
                15 => new SimVector(30m, 0m),
                17 => new SimVector(-4m, 0m),
                _ => (SimVector?)null
            };
            PufferSnapshot? pufferSnapshot = null;
            PufferSnapshot? otherSnapshot = null;
            PlayerNormalSnapshot? playerSnapshot = null;
            world.Step(current =>
            {
                var contact = contactPoint is null
                    ? null
                    : new PufferContact("puffer-demo-player", contactPoint.Value);
                pufferSnapshot = puffer.Update(new PufferInput(contact), current);
                otherSnapshot = other.Update(PufferInput.None, current);
                playerSnapshot = player.Update(
                    new PlayerInput(0, 0, false, false),
                    new PlayerExternalEffects(null, pufferSnapshot.LaunchEffect?.Velocity),
                    current);
            });

            rows.Add(new DemoPufferRow(
                pufferSnapshot!.Tick,
                pufferSnapshot.State.ToString(),
                pufferSnapshot.Center.X,
                pufferSnapshot.Center.Y,
                pufferSnapshot.SwimDirection,
                pufferSnapshot.WarningTicksRemaining,
                pufferSnapshot.RespawnTicksRemaining,
                contactPoint?.X,
                contactPoint?.Y,
                playerSnapshot!.Speed.X,
                playerSnapshot.Speed.Y,
                pufferSnapshot.LaunchEffect is not null &&
                    playerSnapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied),
                otherSnapshot!.State.ToString(),
                string.Join(", ", pufferSnapshot.Events.Select(item => item.EventId))));
        }

        return new DemoPuffer(
            rows.AsReadOnly(),
            rows.Sum(row => CountEvent(row.Events, PufferEventIds.Swam)),
            rows.Sum(row => CountEvent(row.Events, PufferEventIds.Turned)),
            rows.Sum(row => CountEvent(row.Events, PufferEventIds.WarningStarted)),
            rows.Sum(row => CountEvent(row.Events, PufferEventIds.Exploded)),
            rows.Sum(row => CountEvent(row.Events, PufferEventIds.LaunchIssued)),
            rows.Sum(row => CountEvent(row.Events, PufferEventIds.Respawned)),
            rows.Sum(row => CountEvent(row.Events, PufferEventIds.CenterFallbackUsed)),
            rows.Sum(row => CountEvent(row.Events, PufferEventIds.ContactIgnored)),
            rows.Count(row => row.PlayerApplied),
            false);
    }

    static int CountEvent(string events, string eventId) =>
        events.Split(", ", StringSplitOptions.RemoveEmptyEntries)
            .Count(item => string.Equals(item, eventId, StringComparison.Ordinal));

    var first = RunOnce();
    var second = RunOnce();
    return first with { DeterministicReplay = JsonSerializer.Serialize(first.Rows) == JsonSerializer.Serialize(second.Rows) };
}

static DemoSeeker RunSyntheticSeeker()
{
    static DemoSeeker RunOnce()
    {
        var world = new SimulationWorld();
        var tuning = new SeekerTuning(60m, 60m, 120m, 10, 2, 2, 2, 3, 2, 2);
        var seeker = new SeekerController("demo-seeker", SimVector.Zero, -20m, 20m, 1, tuning);
        var other = new SeekerController("demo-seeker-isolated", new SimVector(100m, 100m), 98m, 102m, -1, tuning);
        var rows = new List<DemoSeekerRow>();

        for (var tick = 0; tick < 30; tick++)
        {
            var targetPoint = tick switch
            {
                >= 1 and <= 8 => new SimVector(5m, 0m),
                >= 11 and <= 18 => new SimVector(5m, 0m),
                22 or 23 or 24 => new SimVector(8m, 0m),
                _ => (SimVector?)null
            };
            var touching = tick == 8;
            var wallCollision = tick == 19;
            SeekerSnapshot? seekerSnapshot = null;
            SeekerSnapshot? otherSnapshot = null;
            world.Step(current =>
            {
                var target = targetPoint is null
                    ? null
                    : new SeekerTarget("seeker-demo-player", targetPoint.Value, isTouching: touching);
                seekerSnapshot = seeker.Update(new SeekerInput(target, wallCollision), current);
                otherSnapshot = other.Update(SeekerInput.None, current);
            });

            rows.Add(new DemoSeekerRow(
                seekerSnapshot!.Tick,
                seekerSnapshot.State.ToString(),
                seekerSnapshot.Center.X,
                seekerSnapshot.Center.Y,
                seekerSnapshot.StateTicksRemaining,
                seekerSnapshot.LostSightTicks,
                targetPoint?.X,
                targetPoint?.Y,
                wallCollision,
                seekerSnapshot.HitEffect?.TargetId,
                otherSnapshot!.State.ToString(),
                string.Join(", ", seekerSnapshot.Events.Select(item => item.EventId))));
        }

        return new DemoSeeker(
            rows.AsReadOnly(),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.Patrolled)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.Alerted)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.ChaseStarted)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.Chased)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.WindupStarted)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.DashStarted)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.Dashed)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.TargetHit)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.WallHit)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.Stunned)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.Recovered)),
            rows.Sum(row => CountEvent(row.Events, SeekerEventIds.TargetLost)),
            false);
    }

    static int CountEvent(string events, string eventId) =>
        events.Split(", ", StringSplitOptions.RemoveEmptyEntries)
            .Count(item => string.Equals(item, eventId, StringComparison.Ordinal));

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

    var liftWorld = new SimulationWorld();
    var liftFloor = new Solid("lift-floor", -32, 11, 160, 4);
    var liftActor = new Actor("lift-player", 0, 0, 8, 11);
    liftWorld.Add(liftFloor);
    liftWorld.Add(liftActor);
    var liftController = new PlayerNormalController(liftActor);
    var liftSnapshot = liftController.Step(
        new PlayerInput(0, 0, jumpPressed: true, jumpHeld: true),
        liftWorld,
        world => liftFloor.Move(5m, -3m, world));

    var wallWorld = new SimulationWorld();
    var wallActor = new Actor("wall-speed-player", 0, 0, 8, 11);
    var wall = new Solid("wall-speed-wall", 9, -20, 4, 40);
    wallWorld.Add(wallActor);
    wallWorld.Add(wall);
    var wallController = new PlayerNormalController(wallActor, initialSpeed: new SimVector(90m, 0m));
    var retainedSnapshot = wallController.Step(new PlayerInput(1, 0, false, false), wallWorld);
    var restoredSnapshot = wallController.Step(
        new PlayerInput(1, 0, false, false),
        wallWorld,
        current => wall.Move(20m, 0m, current));

    const int cornerStartX = 0;
    const int cornerStartY = 4;
    var cornerWorld = new SimulationWorld();
    var cornerActor = new Actor("corner-player", cornerStartX, cornerStartY, 2, 2);
    var corner = new Solid("corner-platform", -2, 1, 3, 2);
    cornerWorld.Add(cornerActor);
    cornerWorld.Add(corner);
    var cornerController = new PlayerNormalController(
        cornerActor,
        initialSpeed: new SimVector(0m, -120m));
    var cornerSnapshot = cornerController.Step(
        new PlayerInput(1, 0, jumpPressed: false, jumpHeld: true),
        cornerWorld);

    var oneWayWorld = new SimulationWorld();
    var oneWayActor = new Actor("one-way-player", 0, 0, 8, 11);
    oneWayWorld.Add(oneWayActor);
    oneWayWorld.Add(new OneWayPlatform("one-way-upper", -20, 11, 40, 2));
    oneWayWorld.Add(new OneWayPlatform("one-way-lower", -20, 30, 40, 2));
    var oneWayController = new PlayerNormalController(oneWayActor);
    _ = oneWayController.Step(new PlayerInput(0, 0, false, false), oneWayWorld);
    var oneWaySnapshots = new List<PlayerNormalSnapshot>
    {
        oneWayController.Step(
            new PlayerInput(0, 0, false, false, dropThroughPressed: true),
            oneWayWorld)
    };
    while (oneWaySnapshots[^1].GroundedOneWayPlatformId != "one-way-lower" &&
           oneWaySnapshots.Count < 30)
    {
        oneWaySnapshots.Add(oneWayController.Step(
            new PlayerInput(0, 0, false, false),
            oneWayWorld));
    }
    var oneWayLanded = oneWaySnapshots[^1];
    var oneWayRearmed = oneWayController.Step(
        new PlayerInput(0, 0, false, false, dropThroughPressed: true),
        oneWayWorld);

    var passWorld = new SimulationWorld();
    var passActor = new Actor("one-way-pass-player", 0, 20, 8, 11);
    passWorld.Add(passActor);
    passWorld.Add(new OneWayPlatform("one-way-pass", -20, 11, 40, 2));
    var passController = new PlayerNormalController(
        passActor,
        initialSpeed: new SimVector(0m, -240m));
    for (var tick = 0; tick < 8 && passActor.Bounds.Bottom > 11; tick++)
    {
        _ = passController.Step(new PlayerInput(0, 0, false, true), passWorld);
    }

    return new DemoPlayer(
        rows.AsReadOnly(),
        rows.Any(row => row.SpeedX == NormalJumpTuning.ReferencePartial.MaxRun),
        rows.Count(row => row.Events.Contains(nameof(PlayerNormalEventKind.Jumped), StringComparison.Ordinal)),
        rows.Min(row => row.Y),
        liftSnapshot.AppliedLiftSpeed.X,
        liftSnapshot.AppliedLiftSpeed.Y,
        liftSnapshot.Events.Count(item => item.Kind == PlayerNormalEventKind.LiftVelocityApplied),
        retainedSnapshot.WallSpeedRetained,
        retainedSnapshot.WallSpeedRetentionTicks,
        restoredSnapshot.Speed.X,
        retainedSnapshot.Events.Count(item => item.Kind == PlayerNormalEventKind.WallSpeedRetained),
        restoredSnapshot.Events.Count(item => item.Kind == PlayerNormalEventKind.WallSpeedRestored),
        cornerSnapshot.UpwardCornerCorrectionX,
        cornerStartX,
        cornerSnapshot.Position.X,
        cornerStartY,
        cornerSnapshot.Position.Y,
        cornerSnapshot.Events.Count(item => item.Kind == PlayerNormalEventKind.UpwardCornerCorrected),
        cornerSnapshot.Speed.Y < 0m,
        passActor.Bounds.Bottom <= 11,
        0,
        oneWayLanded.Position.Y,
        oneWayLanded.GroundedOneWayPlatformId,
        oneWaySnapshots.Sum(snapshot => snapshot.Events.Count(item => item.Kind == PlayerNormalEventKind.OneWayDropThroughStarted)),
        oneWaySnapshots.Sum(snapshot => snapshot.Events.Count(item => item.Kind == PlayerNormalEventKind.OneWayDropThroughCompleted)),
        oneWaySnapshots.Sum(snapshot => snapshot.Events.Count(item => item.Kind == PlayerNormalEventKind.OneWayPlatformLanded)),
        oneWayRearmed.DropThroughPlatformId == "one-way-lower" &&
            oneWayRearmed.Events.Any(item => item.Kind == PlayerNormalEventKind.OneWayDropThroughStarted),
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
    decimal AppliedLiftX,
    decimal AppliedLiftY,
    int LiftEventCount,
    decimal RetainedWallSpeed,
    int InitialWallRetentionTicks,
    decimal RestoredWallSpeed,
    int WallRetainedEventCount,
    int WallRestoredEventCount,
    int UpwardCornerCorrectionX,
    int CornerStartX,
    int CornerFinalX,
    int CornerStartY,
    int CornerFinalY,
    int CornerCorrectionEventCount,
    bool CornerVerticalSpeedPreserved,
    bool OneWayPassedUpward,
    int OneWayDropStartY,
    int OneWayLandingY,
    string? OneWayLandedPlatformId,
    int OneWayDropStartedCount,
    int OneWayDropCompletedCount,
    int OneWayLandingCount,
    bool OneWayDropRearmed,
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

internal sealed record DemoSpring(
    IReadOnlyList<DemoSpringRow> Rows,
    int ActivationCount,
    int LaunchCount,
    int ReadyCount,
    int PlayerApplicationCount,
    int TheoApplicationCount,
    int GliderApplicationCount,
    bool DeterministicReplay);

internal sealed record DemoSpringRow(
    long Tick,
    string State,
    int RetractedTicks,
    int CooldownTicks,
    string? TargetId,
    string? TargetKind,
    decimal PlayerSpeedX,
    decimal PlayerSpeedY,
    decimal TheoSpeedX,
    decimal TheoSpeedY,
    decimal GliderSpeedX,
    decimal GliderSpeedY,
    bool PlayerApplied,
    bool TheoApplied,
    bool GliderApplied,
    string Events);

internal sealed record DemoRefill(
    IReadOnlyList<DemoRefillRow> Rows,
    int CollectionCount,
    int RestoreCount,
    int RespawnCount,
    int PlayerApplicationCount,
    bool DeterministicReplay);

internal sealed record DemoRefillRow(
    long Tick,
    string State,
    int RespawnTicks,
    int PlayerDashes,
    decimal PlayerStamina,
    bool PlayerApplied,
    string Events);

internal sealed record DemoWater(
    IReadOnlyList<DemoWaterRow> Rows,
    int EnteredCount,
    int SubmergedCount,
    int MotionIssuedCount,
    int ExitedCount,
    int PlayerApplicationCount,
    bool DeterministicReplay);

internal sealed record DemoWaterRow(
    long Tick,
    string State,
    string Occupants,
    int MoveX,
    int MoveY,
    decimal PlayerSpeedX,
    decimal PlayerSpeedY,
    bool PlayerApplied,
    string Events);

internal sealed record DemoBumper(
    IReadOnlyList<DemoBumperRow> Rows,
    int ActivationCount,
    int LaunchCount,
    int ReadyCount,
    int CenterFallbackCount,
    int IgnoredCount,
    int PlayerApplicationCount,
    bool DeterministicReplay);

internal sealed record DemoBumperRow(
    long Tick,
    string State,
    int CooldownTicks,
    int? ContactX,
    int? ContactY,
    decimal? DirectionX,
    decimal? DirectionY,
    decimal PlayerSpeedX,
    decimal PlayerSpeedY,
    bool PlayerApplied,
    string OtherBumperState,
    string Events);

internal sealed record DemoPuffer(
    IReadOnlyList<DemoPufferRow> Rows,
    int SwamCount,
    int TurnedCount,
    int WarningCount,
    int ExplosionCount,
    int LaunchCount,
    int RespawnCount,
    int CenterFallbackCount,
    int IgnoredCount,
    int PlayerApplicationCount,
    bool DeterministicReplay);

internal sealed record DemoPufferRow(
    long Tick,
    string State,
    decimal CenterX,
    decimal CenterY,
    int Direction,
    int WarningTicks,
    int RespawnTicks,
    decimal? ContactX,
    decimal? ContactY,
    decimal PlayerSpeedX,
    decimal PlayerSpeedY,
    bool PlayerApplied,
    string OtherPufferState,
    string Events);

internal sealed record DemoSeeker(
    IReadOnlyList<DemoSeekerRow> Rows,
    int PatrolCount,
    int AlertCount,
    int ChaseStartCount,
    int ChasedCount,
    int WindupCount,
    int DashStartCount,
    int DashedCount,
    int TargetHitCount,
    int WallHitCount,
    int StunnedCount,
    int RecoveredCount,
    int TargetLostCount,
    bool DeterministicReplay);

internal sealed record DemoSeekerRow(
    long Tick,
    string State,
    decimal CenterX,
    decimal CenterY,
    int StateTicks,
    int LostSightTicks,
    decimal? TargetX,
    decimal? TargetY,
    bool WallCollision,
    string? HitTargetId,
    string OtherSeekerState,
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
