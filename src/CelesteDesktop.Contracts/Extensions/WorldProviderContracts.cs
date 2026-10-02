using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Extensions;

public enum SupportedEntityKind
{
    Player,
    TheoCrystal,
    Glider,
    Spring,
    Refill,
    Water,
    Bumper,
    Puffer,
    Seeker
}

public enum RoomLoadStatus
{
    Loaded,
    NotFound,
    Rejected
}

public sealed record WorldPointDescriptor(int X, int Y);

public sealed record WorldRectangleDescriptor
{
    public WorldRectangleDescriptor(int x, int y, int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = checked(x + width);
        _ = checked(y + height);
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
}

public sealed record WorldDescriptor
{
    public WorldDescriptor(string worldId, IEnumerable<string> roomIds)
    {
        ProviderContractValidation.Identifier(worldId, nameof(worldId));
        ArgumentNullException.ThrowIfNull(roomIds);
        var copy = roomIds.ToArray();
        if (copy.Length is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(roomIds));
        foreach (var roomId in copy) ProviderContractValidation.Identifier(roomId, nameof(roomIds));
        if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("Room IDs must be unique.", nameof(roomIds));
        WorldId = worldId;
        RoomIds = new ReadOnlyCollection<string>(copy);
    }

    public string WorldId { get; }
    public IReadOnlyList<string> RoomIds { get; }
}

public sealed record WorldCatalogDescriptor
{
    public WorldCatalogDescriptor(string providerId, IEnumerable<WorldDescriptor> worlds)
    {
        ProviderContractValidation.Identifier(providerId, nameof(providerId));
        ArgumentNullException.ThrowIfNull(worlds);
        var copy = worlds.ToArray();
        if (copy.Length is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(worlds));
        if (copy.Select(world => world.WorldId).Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("World IDs must be unique.", nameof(worlds));
        ProviderId = providerId;
        Worlds = new ReadOnlyCollection<WorldDescriptor>(copy);
    }

    public string ProviderId { get; }
    public IReadOnlyList<WorldDescriptor> Worlds { get; }
}

public sealed record RoomRequest
{
    public RoomRequest(string worldId, string roomId)
    {
        ProviderContractValidation.Identifier(worldId, nameof(worldId));
        ProviderContractValidation.Identifier(roomId, nameof(roomId));
        WorldId = worldId;
        RoomId = roomId;
    }

    public string WorldId { get; }
    public string RoomId { get; }
}

public sealed record WorldContentBudget
{
    public WorldContentBudget(int maximumSolids, int maximumSpawnPoints, int maximumEntities)
    {
        if (maximumSolids is < 1 or > 65_536) throw new ArgumentOutOfRangeException(nameof(maximumSolids));
        if (maximumSpawnPoints is < 1 or > 4_096) throw new ArgumentOutOfRangeException(nameof(maximumSpawnPoints));
        if (maximumEntities is < 1 or > 65_536) throw new ArgumentOutOfRangeException(nameof(maximumEntities));
        MaximumSolids = maximumSolids;
        MaximumSpawnPoints = maximumSpawnPoints;
        MaximumEntities = maximumEntities;
    }

    public int MaximumSolids { get; }
    public int MaximumSpawnPoints { get; }
    public int MaximumEntities { get; }
}

public sealed record SpawnPointDescriptor
{
    public SpawnPointDescriptor(string spawnId, WorldPointDescriptor position)
    {
        ProviderContractValidation.Identifier(spawnId, nameof(spawnId));
        Position = position ?? throw new ArgumentNullException(nameof(position));
        SpawnId = spawnId;
    }

    public string SpawnId { get; }
    public WorldPointDescriptor Position { get; }
}

public sealed record SolidPlacementDescriptor
{
    public SolidPlacementDescriptor(string solidId, WorldRectangleDescriptor bounds)
    {
        ProviderContractValidation.Identifier(solidId, nameof(solidId));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        SolidId = solidId;
    }

    public string SolidId { get; }
    public WorldRectangleDescriptor Bounds { get; }
}

public sealed record EntityPlacementDescriptor
{
    public EntityPlacementDescriptor(string entityId, SupportedEntityKind kind, WorldPointDescriptor position)
    {
        ProviderContractValidation.Identifier(entityId, nameof(entityId));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Position = position ?? throw new ArgumentNullException(nameof(position));
        EntityId = entityId;
        Kind = kind;
    }

    public string EntityId { get; }
    public SupportedEntityKind Kind { get; }
    public WorldPointDescriptor Position { get; }
}

public sealed class RoomDescriptor
{
    public RoomDescriptor(
        string providerId,
        string worldId,
        string roomId,
        WorldRectangleDescriptor bounds,
        string sourceFingerprintSha256,
        IEnumerable<SolidPlacementDescriptor> solids,
        IEnumerable<SpawnPointDescriptor> spawnPoints,
        IEnumerable<EntityPlacementDescriptor> entities)
    {
        ProviderContractValidation.Identifier(providerId, nameof(providerId));
        ProviderContractValidation.Identifier(worldId, nameof(worldId));
        ProviderContractValidation.Identifier(roomId, nameof(roomId));
        ProviderContractValidation.Sha256(sourceFingerprintSha256, nameof(sourceFingerprintSha256));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        ArgumentNullException.ThrowIfNull(solids);
        ArgumentNullException.ThrowIfNull(spawnPoints);
        ArgumentNullException.ThrowIfNull(entities);

        var solidCopy = solids.ToArray();
        var spawnCopy = spawnPoints.ToArray();
        var entityCopy = entities.ToArray();
        EnsureUnique(solidCopy.Select(item => item.SolidId), nameof(solids));
        EnsureUnique(spawnCopy.Select(item => item.SpawnId), nameof(spawnPoints));
        EnsureUnique(entityCopy.Select(item => item.EntityId), nameof(entities));

        ProviderId = providerId;
        WorldId = worldId;
        RoomId = roomId;
        SourceFingerprintSha256 = sourceFingerprintSha256.ToLowerInvariant();
        Solids = new ReadOnlyCollection<SolidPlacementDescriptor>(solidCopy);
        SpawnPoints = new ReadOnlyCollection<SpawnPointDescriptor>(spawnCopy);
        Entities = new ReadOnlyCollection<EntityPlacementDescriptor>(entityCopy);
    }

    public string ProviderId { get; }
    public string WorldId { get; }
    public string RoomId { get; }
    public WorldRectangleDescriptor Bounds { get; }
    public string SourceFingerprintSha256 { get; }
    public IReadOnlyList<SolidPlacementDescriptor> Solids { get; }
    public IReadOnlyList<SpawnPointDescriptor> SpawnPoints { get; }
    public IReadOnlyList<EntityPlacementDescriptor> Entities { get; }

    private static void EnsureUnique(IEnumerable<string> identifiers, string parameterName)
    {
        var copy = identifiers.ToArray();
        if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
            throw new ArgumentException("Placement IDs must be unique within their category.", parameterName);
    }
}

public sealed record RoomLoadResult
{
    private RoomLoadResult(RoomLoadStatus status, RoomDescriptor? room, string? rejectionCode)
    {
        if (status == RoomLoadStatus.Loaded && room is null)
            throw new ArgumentException("Loaded result requires a room.", nameof(room));
        if (status != RoomLoadStatus.Loaded && room is not null)
            throw new ArgumentException("Only a loaded result may carry a room.", nameof(room));
        if (status == RoomLoadStatus.Rejected)
            ProviderContractValidation.Identifier(rejectionCode!, nameof(rejectionCode));
        else if (rejectionCode is not null)
            throw new ArgumentException("Only a rejected result may carry a rejection code.", nameof(rejectionCode));
        Status = status;
        Room = room;
        RejectionCode = rejectionCode;
    }

    public RoomLoadStatus Status { get; }
    public RoomDescriptor? Room { get; }
    public string? RejectionCode { get; }

    public static RoomLoadResult Loaded(RoomDescriptor room) =>
        new(RoomLoadStatus.Loaded, room ?? throw new ArgumentNullException(nameof(room)), null);

    public static RoomLoadResult NotFound() => new(RoomLoadStatus.NotFound, null, null);

    public static RoomLoadResult Rejected(string code) => new(RoomLoadStatus.Rejected, null, code);
}

public interface IWorldContentProvider
{
    WorldCatalogDescriptor DescribeWorlds();
    RoomLoadResult LoadRoom(RoomRequest request, WorldContentBudget budget);
}
