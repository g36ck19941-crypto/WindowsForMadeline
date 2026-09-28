using System.Text;
using CelesteDesktop.AssetWorker.Catalog;
using CelesteDesktop.Contracts.Assets;

var tests = new (string Name, Action Body)[]
{
    ("explicit frames resolve in declared order", ExplicitFramesResolve),
    ("all frames resolve in numeric order", AllFramesResolveNumerically),
    ("entity namespaces remain isolated", EntityNamespacesRemainIsolated),
    ("only required pages are opened", OnlyRequiredPagesAreOpened),
    ("shared page is decoded once", SharedPageIsDecodedOnce),
    ("trimmed pixels reconstruct an untrimmed frame", TrimmedFrameIsReconstructed),
    ("catalog collections are immutable", CatalogCollectionsAreImmutable),
    ("catalog fingerprint is deterministic", FingerprintIsDeterministic),
    ("source fingerprint changes catalog fingerprint", SourceFingerprintChangesHash),
    ("pixel changes catalog fingerprint", PixelChangeChangesHash),
    ("missing allowlisted entity is rejected", MissingEntityIsRejected),
    ("duplicate allowlist entry is rejected", DuplicateAllowlistIsRejected),
    ("missing explicit atlas entry is rejected", MissingEntryIsRejected),
    ("ambiguous atlas IDs are rejected", AmbiguousEntryIsRejected),
    ("ambiguous entity IDs are rejected", AmbiguousEntityIsRejected),
    ("ambiguous animation IDs are rejected", AmbiguousAnimationIsRejected),
    ("ambiguous numeric all-frame suffix is rejected", AmbiguousNumericSuffixIsRejected),
    ("missing atlas page is rejected", MissingPageIsRejected),
    ("atlas rectangle outside page is rejected", PageBoundsAreRejected),
    ("entity budget is enforced", EntityBudgetIsEnforced),
    ("frame budget is enforced", FrameBudgetIsEnforced),
    ("page budget is enforced", PageBudgetIsEnforced),
    ("pixel budget is enforced", PixelBudgetIsEnforced),
    ("source file budget is enforced", SourceFileBudgetIsEnforced),
    ("invalid source fingerprint is rejected", InvalidSourceFingerprintIsRejected),
    ("catalog surface has no filesystem path API", SurfaceHasNoFilesystemPathApi)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void ExplicitFramesResolve()
{
    var fixture = Fixture(animation: Animation("idle", "demo/player/idle", false, [1, 0]));
    var catalog = Build(fixture);
    var ids = catalog.Entities[0].Animations[0].Frames.Select(frame => frame.AtlasEntryId);
    Assert(ids.SequenceEqual(["demo/player/idle01", "demo/player/idle00"]), "Explicit order changed.");
}

static void AllFramesResolveNumerically()
{
    var fixture = Fixture(animation: Animation("idle", "demo/player/idle", true, []));
    var entries = new[]
    {
        Entry("demo/player/idle10", 0, 2, 0),
        Entry("demo/player/idle02", 0, 1, 0),
        Entry("demo/player/idle00", 0, 0, 0)
    };
    var catalog = Build(fixture with { Atlas = Atlas(entries) });
    Assert(
        catalog.Entities[0].Animations[0].Frames.Select(frame => frame.AtlasEntryId)
            .SequenceEqual(["demo/player/idle00", "demo/player/idle02", "demo/player/idle10"]),
        "All-frame order was not numeric.");
}

static void EntityNamespacesRemainIsolated()
{
    var player = Definition("player", "demo/player/", Animation("idle", "demo/player/idle", false, [0]));
    var spring = Definition("spring", "demo/spring/", Animation("idle", "demo/spring/idle", false, [0]));
    var fixture = Fixture() with
    {
        Sprites = new SpriteMetadataDescriptor([spring, player]),
        Atlas = Atlas([
            Entry("demo/player/idle00", 0, 0, 0),
            Entry("demo/spring/idle00", 0, 1, 0)])
    };
    var catalog = Build(fixture, ["spring", "player"]);
    Assert(catalog.Entities.Select(entity => entity.EntityId).SequenceEqual(["player", "spring"]), "Entities were not canonicalized.");
    Assert(catalog.Entities.All(entity => entity.Animations.Single().Id == "idle"), "Same animation IDs were flattened.");
}

static void OnlyRequiredPagesAreOpened()
{
    var fixture = Fixture() with
    {
        Atlas = new AtlasMetadataDescriptor(1, "test", 1, [
            new AtlasPageDescriptor(0, "page0", [Entry("demo/player/idle00", 0, 0, 0)]),
            new AtlasPageDescriptor(1, "unused", [Entry("unused00", 1, 0, 0)])])
    };
    var source = new TrackingPageSource(new Dictionary<string, byte[]>
    {
        ["page0"] = PageBytes(),
        ["unused"] = PageBytes()
    });
    Build(fixture, source: source);
    Assert(source.Opens.SequenceEqual(["page0"]), "Unused page was opened.");
}

static void SharedPageIsDecodedOnce()
{
    var fixture = Fixture(animation: Animation("idle", "demo/player/idle", false, [0, 1]));
    var source = new TrackingPageSource(fixture.Pages);
    var catalog = Build(fixture, source: source);
    Assert(source.Opens.Count == 1 && catalog.DecodedPageCount == 1, "Shared page was decoded more than once.");
}

static void TrimmedFrameIsReconstructed()
{
    var entry = new AtlasEntryDescriptor("demo/player/idle00", 0, 1, 0, 1, 1, 1, 1, 3, 3);
    var fixture = Fixture() with { Atlas = Atlas([entry]) };
    var frame = Build(fixture).Entities[0].Animations[0].Frames[0].Frame;
    var pixels = frame.CopyPixels();
    Assert(frame.Width == 3 && frame.Height == 3, "Untrimmed dimensions were lost.");
    Assert(pixels.Skip(((1 * 3) + 1) * 4).Take(4).SequenceEqual(new byte[] { 5, 6, 7, 255 }), "Trimmed pixel was misplaced.");
    Assert(pixels.Take(4).All(value => value == 0), "Transparent padding was not preserved.");
}

static void CatalogCollectionsAreImmutable()
{
    var catalog = Build(Fixture());
    AssertReadOnly((IList<EntityAssetCatalog>)catalog.Entities, catalog.Entities[0]);
    AssertReadOnly((IList<CatalogAnimationDescriptor>)catalog.Entities[0].Animations, catalog.Entities[0].Animations[0]);
    AssertReadOnly((IList<CatalogFrameDescriptor>)catalog.Entities[0].Animations[0].Frames, catalog.Entities[0].Animations[0].Frames[0]);
    var copy = catalog.Entities[0].Animations[0].Frames[0].Frame.CopyPixels();
    copy[0] ^= 255;
    Assert(copy[0] != catalog.Entities[0].Animations[0].Frames[0].Frame.CopyPixels()[0], "Frame pixels leaked mutation.");
}

static void FingerprintIsDeterministic()
{
    var fixture = Fixture();
    var reversedSource = new AssetSourceFingerprint("synthetic", fixture.Source.Files.Reverse());
    var first = Build(fixture).CatalogSha256;
    var second = Build(fixture with { Source = reversedSource }).CatalogSha256;
    Assert(first == second, "Input ordering changed the fingerprint.");
}

static void SourceFingerprintChangesHash()
{
    var fixture = Fixture();
    var changed = new AssetSourceFingerprint("synthetic", [new AssetFileFingerprint("atlas.meta", 99, Hash('a'))]);
    Assert(Build(fixture).CatalogSha256 != Build(fixture with { Source = changed }).CatalogSha256, "Source change was ignored.");
}

static void PixelChangeChangesHash()
{
    var fixture = Fixture();
    var changedPages = new Dictionary<string, byte[]> { ["page0"] = PageBytes(firstBlue: 42) };
    Assert(Build(fixture).CatalogSha256 != Build(fixture with { Pages = changedPages }).CatalogSha256, "Pixel change was ignored.");
}

static void MissingEntityIsRejected() => AssertCode(
    AssetCatalogCodes.EntityMissing,
    () => Build(Fixture(), ["missing"]));

static void DuplicateAllowlistIsRejected() => AssertCode(
    AssetCatalogCodes.AllowlistInvalid,
    () => Build(Fixture(), ["player", "player"]));

static void MissingEntryIsRejected() => AssertCode(
    AssetCatalogCodes.AtlasEntryMissing,
    () => Build(Fixture() with { Atlas = Atlas([]) }));

static void AmbiguousEntryIsRejected()
{
    var entries = new[]
    {
        Entry("demo/player/idle00", 0, 0, 0),
        Entry("DEMO/PLAYER/IDLE00", 0, 1, 0)
    };
    AssertCode(AssetCatalogCodes.AtlasEntryAmbiguous, () => Build(Fixture() with { Atlas = Atlas(entries) }));
}

static void AmbiguousEntityIsRejected()
{
    var first = Definition("player", "demo/player/", Animation("idle", "demo/player/idle", false, [0]));
    var second = Definition("PLAYER", "demo/player/", Animation("idle", "demo/player/idle", false, [0]));
    var fixture = Fixture() with { Sprites = new SpriteMetadataDescriptor([first, second]) };
    AssertCode(AssetCatalogCodes.NameAmbiguous, () => Build(fixture));
}

static void AmbiguousAnimationIsRejected()
{
    var definition = Definition(
        "player",
        "demo/player/",
        Animation("idle", "demo/player/idle", false, [0]),
        Animation("IDLE", "demo/player/idle", false, [0]));
    var fixture = Fixture() with { Sprites = new SpriteMetadataDescriptor([definition]) };
    AssertCode(AssetCatalogCodes.NameAmbiguous, () => Build(fixture));
}

static void AmbiguousNumericSuffixIsRejected()
{
    var fixture = Fixture(animation: Animation("idle", "demo/player/idle", true, []));
    var entries = new[]
    {
        Entry("demo/player/idle00", 0, 0, 0),
        Entry("demo/player/idle000", 0, 1, 0)
    };
    AssertCode(AssetCatalogCodes.AtlasEntryAmbiguous, () => Build(fixture with { Atlas = Atlas(entries) }));
}

static void MissingPageIsRejected()
{
    var entry = Entry("demo/player/idle00", 7, 0, 0);
    AssertCode(AssetCatalogCodes.PageMissing, () => Build(Fixture() with { Atlas = Atlas([entry]) }));
}

static void PageBoundsAreRejected()
{
    var entry = new AtlasEntryDescriptor("demo/player/idle00", 0, 3, 0, 2, 1, 0, 0, 2, 1);
    AssertCode(AssetCatalogCodes.PageBoundsInvalid, () => Build(Fixture() with { Atlas = Atlas([entry]) }));
}

static void EntityBudgetIsEnforced()
{
    var options = AssetCatalogBuilderOptions.Default with { MaximumEntities = 1 };
    AssertCode(AssetCatalogCodes.AllowlistInvalid, () => Build(Fixture(), ["player", "other"], options));
}

static void FrameBudgetIsEnforced()
{
    var options = AssetCatalogBuilderOptions.Default with { MaximumFrames = 1 };
    AssertCode(AssetCatalogCodes.BudgetExceeded, () => Build(Fixture(animation: Animation("idle", "demo/player/idle", false, [0, 1])), options: options));
}

static void PageBudgetIsEnforced()
{
    var fixture = Fixture(animation: Animation("idle", "demo/player/idle", false, [0, 1])) with
    {
        Atlas = new AtlasMetadataDescriptor(1, "test", 1, [
            new AtlasPageDescriptor(0, "page0", [Entry("demo/player/idle00", 0, 0, 0)]),
            new AtlasPageDescriptor(1, "page1", [Entry("demo/player/idle01", 1, 0, 0)])]),
        Pages = new Dictionary<string, byte[]> { ["page0"] = PageBytes(), ["page1"] = PageBytes() }
    };
    var options = AssetCatalogBuilderOptions.Default with { MaximumDecodedPages = 1 };
    AssertCode(AssetCatalogCodes.BudgetExceeded, () => Build(fixture, options: options));
}

static void PixelBudgetIsEnforced()
{
    var options = AssetCatalogBuilderOptions.Default with { MaximumPixelBytes = 3 };
    AssertCode(AssetCatalogCodes.BudgetExceeded, () => Build(Fixture(), options: options));
}

static void SourceFileBudgetIsEnforced()
{
    var options = AssetCatalogBuilderOptions.Default with { MaximumSourceFiles = 1 };
    AssertCode(AssetCatalogCodes.SourceFingerprintInvalid, () => Build(Fixture(), options: options));
}

static void InvalidSourceFingerprintIsRejected()
{
    var source = new AssetSourceFingerprint("synthetic", [new AssetFileFingerprint("atlas.meta", 1, "bad")]);
    AssertCode(AssetCatalogCodes.SourceFingerprintInvalid, () => Build(Fixture() with { Source = source }));
}

static void SurfaceHasNoFilesystemPathApi()
{
    var methods = typeof(AssetCatalogBuilder).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
    Assert(methods.Length == 1 && methods[0].Name == nameof(AssetCatalogBuilder.Build), "Unexpected public builder API.");
    Assert(!methods[0].GetParameters().Any(parameter => parameter.Name?.Contains("path", StringComparison.OrdinalIgnoreCase) == true), "Builder exposed a filesystem path.");
    Assert(typeof(IAtlasPageStreamSource).GetMethod(nameof(IAtlasPageStreamSource.OpenPage))?.ReturnType == typeof(Stream), "Page source is not stream-only.");
}

static NormalizedAssetCatalog Build(
    FixtureData fixture,
    string[]? allowlist = null,
    AssetCatalogBuilderOptions? options = null,
    TrackingPageSource? source = null) =>
    new AssetCatalogBuilder(options: options).Build(
        fixture.Atlas,
        fixture.Sprites,
        fixture.Source,
        allowlist ?? ["player"],
        source ?? new TrackingPageSource(fixture.Pages));

static FixtureData Fixture(SpriteAnimationDescriptor? animation = null)
{
    animation ??= Animation("idle", "demo/player/idle", false, [0]);
    return new FixtureData(
        Atlas([
            Entry("demo/player/idle00", 0, 0, 0),
            Entry("demo/player/idle01", 0, 1, 0)]),
        new SpriteMetadataDescriptor([Definition("player", "demo/player/", animation)]),
        new AssetSourceFingerprint("synthetic", [
            new AssetFileFingerprint("sprites.xml", 12, Hash('b')),
            new AssetFileFingerprint("atlas.meta", 34, Hash('a'))]),
        new Dictionary<string, byte[]> { ["page0"] = PageBytes() });
}

static AtlasMetadataDescriptor Atlas(IEnumerable<AtlasEntryDescriptor> entries) =>
    new(1, "test", 1, [new AtlasPageDescriptor(0, "page0", entries)]);

static AtlasEntryDescriptor Entry(string id, int page, int x, int y) =>
    new(id, page, x, y, 1, 1, 0, 0, 1, 1);

static SpriteDefinitionDescriptor Definition(string id, string prefix, params SpriteAnimationDescriptor[] animations) =>
    new(id, prefix, animations[0].Id, new SpriteOriginDescriptor(SpriteOriginKind.Center, 0, 0), null, animations, []);

static SpriteAnimationDescriptor Animation(string id, string path, bool all, IEnumerable<int> frames) =>
    new(id, path, 0.1, true, null, all, frames);

static byte[] PageBytes(byte firstBlue = 1, byte secondBlue = 5)
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
        writer.Write(4);
        writer.Write(2);
        writer.Write((byte)1);
        writer.Write((byte)1); writer.Write((byte)255); writer.Write(firstBlue); writer.Write((byte)2); writer.Write((byte)3);
        writer.Write((byte)1); writer.Write((byte)255); writer.Write(secondBlue); writer.Write((byte)6); writer.Write((byte)7);
        writer.Write((byte)6); writer.Write((byte)0);
    }
    return stream.ToArray();
}

static string Hash(char value) => new(value, 64);

static void AssertCode(string code, Action action)
{
    try
    {
        action();
    }
    catch (AssetCatalogException exception) when (exception.Code == code)
    {
        return;
    }
    throw new InvalidOperationException($"Expected {code}.");
}

static void AssertReadOnly<T>(IList<T> list, T value)
{
    try
    {
        list.Add(value);
    }
    catch (NotSupportedException)
    {
        return;
    }
    throw new InvalidOperationException("Collection accepted mutation.");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

internal sealed record FixtureData(
    AtlasMetadataDescriptor Atlas,
    SpriteMetadataDescriptor Sprites,
    AssetSourceFingerprint Source,
    IReadOnlyDictionary<string, byte[]> Pages);

internal sealed class TrackingPageSource : IAtlasPageStreamSource
{
    private readonly IReadOnlyDictionary<string, byte[]> _pages;

    public TrackingPageSource(IReadOnlyDictionary<string, byte[]> pages) => _pages = pages;

    public List<string> Opens { get; } = [];

    public Stream? OpenPage(string logicalPath)
    {
        Opens.Add(logicalPath);
        return _pages.TryGetValue(logicalPath, out var bytes)
            ? new MemoryStream(bytes, writable: false)
            : null;
    }
}
