using System.Reflection;
using CelesteDesktop.Contracts.Extensions;

const string HashA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
const string HashB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

var tests = new (string Name, Action Body)[]
{
    ("asset source descriptor preserves facts", AssetSourceFacts),
    ("asset priority is bounded", AssetPriorityBounded),
    ("asset fingerprint requires sha256", AssetFingerprintRequiresSha),
    ("logical asset ID is relative", LogicalIdRelative),
    ("logical asset ID rejects parent segment", LogicalIdRejectsParent),
    ("asset entry preserves bounded metadata", AssetEntryFacts),
    ("asset length rejects negative", AssetLengthRejectsNegative),
    ("found resolution requires entry", FoundResolution),
    ("not found resolution carries no entry", NotFoundResolution),
    ("rejected resolution requires stable code", RejectedResolution),
    ("asset read budget is bounded", AssetBudgetBounded),
    ("loaded asset bytes are copied", AssetBytesCopied),
    ("asset content collection is read only", AssetContentReadOnly),
    ("synthetic asset provider resolves and reads", SyntheticAssetProviderReads),
    ("synthetic asset provider enforces budget", SyntheticAssetProviderBudget),
    ("world rectangle rejects empty area", WorldRectangleRejectsEmpty),
    ("world rectangle rejects overflow", WorldRectangleRejectsOverflow),
    ("world room IDs are immutable", WorldRoomIdsImmutable),
    ("world room IDs are unique", WorldRoomIdsUnique),
    ("world catalog IDs are unique", WorldCatalogIdsUnique),
    ("world content budget is bounded", WorldBudgetBounded),
    ("room copies solid placements", RoomCopiesSolids),
    ("room copies spawn points", RoomCopiesSpawns),
    ("room copies entity placements", RoomCopiesEntities),
    ("room placement IDs are unique", RoomPlacementIdsUnique),
    ("room fingerprint requires sha256", RoomFingerprintRequiresSha),
    ("synthetic world provider loads room", SyntheticWorldProviderLoads),
    ("synthetic world provider enforces budget", SyntheticWorldProviderBudget),
    ("same synthetic providers replay deterministically", ProvidersReplayDeterministically),
    ("provider contracts expose no path stream delegate or assembly", ProviderSurfaceIsDataOnly)
};

var failed = 0;
foreach (var test in tests)
{
    try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception exception) { failed++; Console.Error.WriteLine($"FAIL {test.Name}\n{exception}"); }
}
Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void AssetSourceFacts()
{
    var item = Source();
    Equal("synthetic.assets", item.ProviderId); Equal(AssetSourceKind.Synthetic, item.SourceKind); Equal(10, item.Priority); Equal("1", item.Version);
    Throws<ArgumentOutOfRangeException>(() => new AssetSourceDescriptor("source", (AssetSourceKind)99, 0, "1", HashA));
}

static void AssetPriorityBounded() => Throws<ArgumentOutOfRangeException>(() => new AssetSourceDescriptor("source", AssetSourceKind.Synthetic, 1001, "1", HashA));
static void AssetFingerprintRequiresSha() => Throws<ArgumentException>(() => new AssetSourceDescriptor("source", AssetSourceKind.Synthetic, 0, "1", "bad"));
static void LogicalIdRelative() => Throws<ArgumentException>(() => new AssetSourceRequest("/atlas/player", AssetResourceKind.AtlasPage));
static void LogicalIdRejectsParent() => Throws<ArgumentException>(() => new AssetSourceRequest("atlas/../player", AssetResourceKind.AtlasPage));
static void AssetEntryFacts() { var item = Entry(); Equal("synthetic.assets", item.ProviderId); Equal(4L, item.ContentLength); Equal(AssetResourceKind.AtlasPage, item.Kind); Throws<ArgumentOutOfRangeException>(() => new AssetSourceEntry("source", "atlas/player", (AssetResourceKind)99, 1, HashA)); }
static void AssetLengthRejectsNegative() => Throws<ArgumentOutOfRangeException>(() => new AssetSourceEntry("source", "atlas/player", AssetResourceKind.AtlasPage, -1, HashA));
static void FoundResolution() { var result = AssetSourceResolution.Found(Entry()); Equal(AssetResolutionStatus.Found, result.Status); True(result.Entry is not null); }
static void NotFoundResolution() { var result = AssetSourceResolution.NotFound(); Equal(AssetResolutionStatus.NotFound, result.Status); True(result.Entry is null); }
static void RejectedResolution() { var result = AssetSourceResolution.Rejected("ASSET_KIND_REJECTED"); Equal("ASSET_KIND_REJECTED", result.RejectionCode); }
static void AssetBudgetBounded() { Throws<ArgumentOutOfRangeException>(() => new AssetReadBudget(0)); Throws<ArgumentOutOfRangeException>(() => new AssetReadBudget(268_435_457)); }
static void AssetBytesCopied() { var source = new byte[] { 1, 2, 3 }; var result = AssetReadResult.Loaded(source); source[0] = 9; Equal((byte)1, result.Content[0]); }
static void AssetContentReadOnly() { var result = AssetReadResult.Loaded([1, 2]); Throws<NotSupportedException>(() => ((IList<byte>)result.Content)[0] = 9); }
static void SyntheticAssetProviderReads() { var provider = new SyntheticAssetProvider(); var found = provider.Resolve(new("atlas/player", AssetResourceKind.AtlasPage)); Equal(AssetResolutionStatus.Found, found.Status); var read = provider.OpenRead(found.Entry!, new(4)); Equal(AssetReadStatus.Loaded, read.Status); Equal(4, read.Content.Count); }
static void SyntheticAssetProviderBudget() { var provider = new SyntheticAssetProvider(); var read = provider.OpenRead(Entry(), new(3)); Equal(AssetReadStatus.Rejected, read.Status); Equal("ASSET_BUDGET_EXCEEDED", read.RejectionCode); }
static void WorldRectangleRejectsEmpty() => Throws<ArgumentOutOfRangeException>(() => new WorldRectangleDescriptor(0, 0, 0, 1));
static void WorldRectangleRejectsOverflow() => Throws<OverflowException>(() => new WorldRectangleDescriptor(int.MaxValue, 0, 1, 1));
static void WorldRoomIdsImmutable() { var ids = new[] { "room_a" }; var world = new WorldDescriptor("world", ids); ids[0] = "changed"; Equal("room_a", world.RoomIds[0]); Throws<NotSupportedException>(() => ((IList<string>)world.RoomIds)[0] = "changed"); }
static void WorldRoomIdsUnique() => Throws<ArgumentException>(() => new WorldDescriptor("world", ["room", "room"]));
static void WorldCatalogIdsUnique() => Throws<ArgumentException>(() => new WorldCatalogDescriptor("provider", [new("world", ["a"]), new("world", ["b"])]));
static void WorldBudgetBounded() { Throws<ArgumentOutOfRangeException>(() => new WorldContentBudget(0, 1, 1)); Throws<ArgumentOutOfRangeException>(() => new WorldContentBudget(1, 0, 1)); Throws<ArgumentOutOfRangeException>(() => new WorldContentBudget(1, 1, 0)); }
static void RoomCopiesSolids() { var source = new[] { Solid() }; var room = Room(solids: source); source[0] = new("changed", new(0, 0, 1, 1)); Equal("floor", room.Solids[0].SolidId); }
static void RoomCopiesSpawns() { var source = new[] { Spawn() }; var room = Room(spawns: source); source[0] = new("changed", new(0, 0)); Equal("start", room.SpawnPoints[0].SpawnId); }
static void RoomCopiesEntities() { var source = new[] { Entity() }; var room = Room(entities: source); source[0] = new("changed", SupportedEntityKind.Seeker, new(0, 0)); Equal("theo", room.Entities[0].EntityId); Throws<ArgumentOutOfRangeException>(() => new EntityPlacementDescriptor("bad", (SupportedEntityKind)99, new(0, 0))); }
static void RoomPlacementIdsUnique() => Throws<ArgumentException>(() => Room(solids: [Solid(), Solid()]));
static void RoomFingerprintRequiresSha() => Throws<ArgumentException>(() => new RoomDescriptor("provider", "world", "room", new(0, 0, 10, 10), "bad", [], [], []));
static void SyntheticWorldProviderLoads() { var result = new SyntheticWorldProvider().LoadRoom(new("world", "room"), new(4, 4, 4)); Equal(RoomLoadStatus.Loaded, result.Status); Equal(1, result.Room!.Solids.Count); Equal(1, result.Room.Entities.Count); }
static void SyntheticWorldProviderBudget() { var rejected = new SyntheticWorldProvider(oversized: true).LoadRoom(new("world", "room"), new(1, 1, 1)); Equal(RoomLoadStatus.Rejected, rejected.Status); Equal("WORLD_BUDGET_EXCEEDED", rejected.RejectionCode); }
static void ProvidersReplayDeterministically() { Equal(Signature(), Signature()); }

static void ProviderSurfaceIsDataOnly()
{
    var forbidden = new[] { typeof(Stream), typeof(Delegate), typeof(Assembly), typeof(System.Runtime.InteropServices.SafeHandle) };
    var types = typeof(IAssetSourceProvider).Assembly.GetExportedTypes().Where(type => type.Namespace == "CelesteDesktop.Contracts.Extensions");
    foreach (var type in types)
    foreach (var memberType in PublicMemberTypes(type))
        True(!forbidden.Any(item => item.IsAssignableFrom(memberType)), $"{type.Name} exposes {memberType.Name}");
}

static IEnumerable<Type> PublicMemberTypes(Type type) =>
    type.GetProperties().Select(property => property.PropertyType)
        .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(method => method.ReturnType))
        .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)));

static AssetSourceDescriptor Source() => new("synthetic.assets", AssetSourceKind.Synthetic, 10, "1", HashA);
static AssetSourceEntry Entry() => new("synthetic.assets", "atlas/player", AssetResourceKind.AtlasPage, 4, HashB);
static SolidPlacementDescriptor Solid() => new("floor", new(0, 16, 32, 8));
static SpawnPointDescriptor Spawn() => new("start", new(8, 8));
static EntityPlacementDescriptor Entity() => new("theo", SupportedEntityKind.TheoCrystal, new(16, 8));
static RoomDescriptor Room(IEnumerable<SolidPlacementDescriptor>? solids = null, IEnumerable<SpawnPointDescriptor>? spawns = null, IEnumerable<EntityPlacementDescriptor>? entities = null) =>
    new("synthetic.worlds", "world", "room", new(0, 0, 32, 24), HashA, solids ?? [Solid()], spawns ?? [Spawn()], entities ?? [Entity()]);

static string Signature()
{
    var assets = new SyntheticAssetProvider();
    var entry = assets.Resolve(new("atlas/player", AssetResourceKind.AtlasPage)).Entry!;
    var bytes = assets.OpenRead(entry, new(4)).Content;
    var room = new SyntheticWorldProvider().LoadRoom(new("world", "room"), new(4, 4, 4)).Room!;
    return $"{assets.DescribeSource().ProviderId}|{entry.LogicalId}|{string.Join(',', bytes)}|{room.WorldId}|{room.Solids[0].SolidId}|{room.Entities[0].Kind}";
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}
static void True(bool value, string? message = null) { if (!value) throw new InvalidOperationException(message ?? "Expected true."); }
static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }

sealed class SyntheticAssetProvider : IAssetSourceProvider
{
    private static readonly byte[] Bytes = [1, 2, 3, 4];
    public AssetSourceDescriptor DescribeSource() => new("synthetic.assets", AssetSourceKind.Synthetic, 10, "1", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    public AssetSourceResolution Resolve(AssetSourceRequest request) =>
        request.LogicalId == "atlas/player" && request.Kind == AssetResourceKind.AtlasPage
            ? AssetSourceResolution.Found(new("synthetic.assets", "atlas/player", AssetResourceKind.AtlasPage, 4, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"))
            : AssetSourceResolution.NotFound();
    public AssetReadResult OpenRead(AssetSourceEntry entry, AssetReadBudget budget) =>
        entry.LogicalId != "atlas/player" ? AssetReadResult.NotFound() :
        entry.ContentLength > budget.MaximumBytes ? AssetReadResult.Rejected("ASSET_BUDGET_EXCEEDED") : AssetReadResult.Loaded(Bytes);
}

sealed class SyntheticWorldProvider(bool oversized = false) : IWorldContentProvider
{
    public WorldCatalogDescriptor DescribeWorlds() => new("synthetic.worlds", [new("world", ["room"])]);
    public RoomLoadResult LoadRoom(RoomRequest request, WorldContentBudget budget)
    {
        if (request.WorldId != "world" || request.RoomId != "room") return RoomLoadResult.NotFound();
        var solids = oversized
            ? new[] { new SolidPlacementDescriptor("floor_a", new(0, 16, 16, 8)), new SolidPlacementDescriptor("floor_b", new(16, 16, 16, 8)) }
            : new[] { new SolidPlacementDescriptor("floor", new(0, 16, 32, 8)) };
        var room = new RoomDescriptor(
            "synthetic.worlds", "world", "room", new(0, 0, 32, 24),
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            solids,
            [new SpawnPointDescriptor("start", new(8, 8))],
            [new EntityPlacementDescriptor("theo", SupportedEntityKind.TheoCrystal, new(16, 8))]);
        if (room.Solids.Count > budget.MaximumSolids || room.SpawnPoints.Count > budget.MaximumSpawnPoints || room.Entities.Count > budget.MaximumEntities)
            return RoomLoadResult.Rejected("WORLD_BUDGET_EXCEEDED");
        return RoomLoadResult.Loaded(room);
    }
}
