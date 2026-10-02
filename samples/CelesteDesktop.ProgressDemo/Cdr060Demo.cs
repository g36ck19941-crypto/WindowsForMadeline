using System.Security.Cryptography;
using CelesteDesktop.Contracts.Extensions;

internal static class Cdr060Demo
{
    public static DemoExtensionContracts Run()
    {
        var first = RunOnce();
        var second = RunOnce();
        return first with { DeterministicReplay = first == second };
    }

    private static DemoExtensionContracts RunOnce()
    {
        var assets = new DemoAssetProvider();
        var source = assets.DescribeSource();
        var resolution = assets.Resolve(new AssetSourceRequest("demo/player/atlas", AssetResourceKind.AtlasPage));
        var read = assets.OpenRead(resolution.Entry!, new AssetReadBudget(4));
        var rejectedRead = assets.OpenRead(resolution.Entry!, new AssetReadBudget(3));

        var worlds = new DemoWorldProvider();
        var catalog = worlds.DescribeWorlds();
        var loaded = worlds.LoadRoom(new RoomRequest("demo_world", "room_00"), new WorldContentBudget(8, 4, 8));
        var rejectedRoom = worlds.LoadRoom(new RoomRequest("demo_world", "room_00"), new WorldContentBudget(1, 1, 1));
        var room = loaded.Room!;

        return new DemoExtensionContracts(
            source.ProviderId,
            source.SourceKind.ToString(),
            resolution.Status.ToString(),
            read.Content.Count,
            rejectedRead.RejectionCode == "ASSET_BUDGET_EXCEEDED",
            catalog.ProviderId,
            catalog.Worlds.Count,
            catalog.Worlds.Sum(world => world.RoomIds.Count),
            room.Solids.Count,
            room.SpawnPoints.Count,
            room.Entities.Count,
            string.Join(',', room.Entities.Select(entity => entity.Kind.ToString())),
            rejectedRoom.RejectionCode == "WORLD_BUDGET_EXCEEDED",
            false);
    }

    private sealed class DemoAssetProvider : IAssetSourceProvider
    {
        private static readonly byte[] Content = [4, 3, 2, 1];
        private static readonly string Hash = Convert.ToHexString(SHA256.HashData(Content)).ToLowerInvariant();

        public AssetSourceDescriptor DescribeSource() =>
            new("demo.assets", AssetSourceKind.Synthetic, 10, "1", Hash);

        public AssetSourceResolution Resolve(AssetSourceRequest request) =>
            request.LogicalId == "demo/player/atlas" && request.Kind == AssetResourceKind.AtlasPage
                ? AssetSourceResolution.Found(new AssetSourceEntry("demo.assets", request.LogicalId, request.Kind, Content.Length, Hash))
                : AssetSourceResolution.NotFound();

        public AssetReadResult OpenRead(AssetSourceEntry entry, AssetReadBudget budget) =>
            entry.ContentLength > budget.MaximumBytes
                ? AssetReadResult.Rejected("ASSET_BUDGET_EXCEEDED")
                : AssetReadResult.Loaded(Content);
    }

    private sealed class DemoWorldProvider : IWorldContentProvider
    {
        public WorldCatalogDescriptor DescribeWorlds() =>
            new("demo.worlds", [new WorldDescriptor("demo_world", ["room_00"])]);

        public RoomLoadResult LoadRoom(RoomRequest request, WorldContentBudget budget)
        {
            if (request.WorldId != "demo_world" || request.RoomId != "room_00") return RoomLoadResult.NotFound();
            var room = new RoomDescriptor(
                "demo.worlds",
                request.WorldId,
                request.RoomId,
                new WorldRectangleDescriptor(0, 0, 320, 180),
                "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                [
                    new SolidPlacementDescriptor("floor", new WorldRectangleDescriptor(0, 160, 320, 20)),
                    new SolidPlacementDescriptor("platform", new WorldRectangleDescriptor(96, 112, 64, 8))
                ],
                [new SpawnPointDescriptor("start", new WorldPointDescriptor(32, 144))],
                [
                    new EntityPlacementDescriptor("theo", SupportedEntityKind.TheoCrystal, new WorldPointDescriptor(144, 144)),
                    new EntityPlacementDescriptor("spring", SupportedEntityKind.Spring, new WorldPointDescriptor(208, 152)),
                    new EntityPlacementDescriptor("glider", SupportedEntityKind.Glider, new WorldPointDescriptor(248, 136))
                ]);

            if (room.Solids.Count > budget.MaximumSolids ||
                room.SpawnPoints.Count > budget.MaximumSpawnPoints ||
                room.Entities.Count > budget.MaximumEntities)
                return RoomLoadResult.Rejected("WORLD_BUDGET_EXCEEDED");
            return RoomLoadResult.Loaded(room);
        }
    }
}

internal sealed record DemoExtensionContracts(
    string AssetProviderId,
    string AssetSourceKind,
    string AssetResolutionStatus,
    int AssetBytes,
    bool AssetBudgetRejected,
    string WorldProviderId,
    int WorldCount,
    int RoomCount,
    int SolidCount,
    int SpawnCount,
    int EntityCount,
    string EntityKinds,
    bool WorldBudgetRejected,
    bool DeterministicReplay);
