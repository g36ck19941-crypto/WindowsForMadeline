using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Extensions;

public enum AssetSourceKind
{
    Synthetic,
    BuiltInInstallation,
    DataOnlyMod
}

public enum AssetResourceKind
{
    AtlasMetadata,
    AtlasPage,
    SpriteDefinitions
}

public enum AssetResolutionStatus
{
    Found,
    NotFound,
    Rejected
}

public enum AssetReadStatus
{
    Loaded,
    NotFound,
    Rejected
}

public sealed record AssetSourceDescriptor
{
    public AssetSourceDescriptor(
        string providerId,
        AssetSourceKind sourceKind,
        int priority,
        string version,
        string fingerprintSha256)
    {
        ProviderContractValidation.Identifier(providerId, nameof(providerId));
        ProviderContractValidation.Text(version, nameof(version), 64);
        ProviderContractValidation.Sha256(fingerprintSha256, nameof(fingerprintSha256));
        if (priority is < 0 or > 1000) throw new ArgumentOutOfRangeException(nameof(priority));
        if (!Enum.IsDefined(sourceKind)) throw new ArgumentOutOfRangeException(nameof(sourceKind));

        ProviderId = providerId;
        SourceKind = sourceKind;
        Priority = priority;
        Version = version;
        FingerprintSha256 = fingerprintSha256.ToLowerInvariant();
    }

    public string ProviderId { get; }
    public AssetSourceKind SourceKind { get; }
    public int Priority { get; }
    public string Version { get; }
    public string FingerprintSha256 { get; }
}

public sealed record AssetSourceRequest
{
    public AssetSourceRequest(string logicalId, AssetResourceKind kind)
    {
        ProviderContractValidation.LogicalId(logicalId, nameof(logicalId));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        LogicalId = logicalId;
        Kind = kind;
    }

    public string LogicalId { get; }
    public AssetResourceKind Kind { get; }
}

public sealed record AssetSourceEntry
{
    public AssetSourceEntry(
        string providerId,
        string logicalId,
        AssetResourceKind kind,
        long contentLength,
        string contentSha256)
    {
        ProviderContractValidation.Identifier(providerId, nameof(providerId));
        ProviderContractValidation.LogicalId(logicalId, nameof(logicalId));
        ProviderContractValidation.Sha256(contentSha256, nameof(contentSha256));
        if (contentLength < 0) throw new ArgumentOutOfRangeException(nameof(contentLength));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));

        ProviderId = providerId;
        LogicalId = logicalId;
        Kind = kind;
        ContentLength = contentLength;
        ContentSha256 = contentSha256.ToLowerInvariant();
    }

    public string ProviderId { get; }
    public string LogicalId { get; }
    public AssetResourceKind Kind { get; }
    public long ContentLength { get; }
    public string ContentSha256 { get; }
}

public sealed record AssetSourceResolution
{
    private AssetSourceResolution(AssetResolutionStatus status, AssetSourceEntry? entry, string? rejectionCode)
    {
        if (status == AssetResolutionStatus.Found && entry is null)
            throw new ArgumentException("Found resolution requires an entry.", nameof(entry));
        if (status != AssetResolutionStatus.Found && entry is not null)
            throw new ArgumentException("Only a found resolution may carry an entry.", nameof(entry));
        if (status == AssetResolutionStatus.Rejected)
            ProviderContractValidation.Identifier(rejectionCode!, nameof(rejectionCode));
        else if (rejectionCode is not null)
            throw new ArgumentException("Only a rejected resolution may carry a rejection code.", nameof(rejectionCode));

        Status = status;
        Entry = entry;
        RejectionCode = rejectionCode;
    }

    public AssetResolutionStatus Status { get; }
    public AssetSourceEntry? Entry { get; }
    public string? RejectionCode { get; }

    public static AssetSourceResolution Found(AssetSourceEntry entry) =>
        new(AssetResolutionStatus.Found, entry ?? throw new ArgumentNullException(nameof(entry)), null);

    public static AssetSourceResolution NotFound() => new(AssetResolutionStatus.NotFound, null, null);

    public static AssetSourceResolution Rejected(string code) => new(AssetResolutionStatus.Rejected, null, code);
}

public sealed record AssetReadBudget
{
    public AssetReadBudget(int maximumBytes)
    {
        if (maximumBytes is < 1 or > 268_435_456) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        MaximumBytes = maximumBytes;
    }

    public int MaximumBytes { get; }
}

public sealed class AssetReadResult
{
    private AssetReadResult(AssetReadStatus status, IEnumerable<byte>? content, string? rejectionCode)
    {
        if (status == AssetReadStatus.Loaded && content is null)
            throw new ArgumentException("Loaded result requires content.", nameof(content));
        if (status != AssetReadStatus.Loaded && content is not null)
            throw new ArgumentException("Only a loaded result may carry content.", nameof(content));
        if (status == AssetReadStatus.Rejected)
            ProviderContractValidation.Identifier(rejectionCode!, nameof(rejectionCode));
        else if (rejectionCode is not null)
            throw new ArgumentException("Only a rejected result may carry a rejection code.", nameof(rejectionCode));

        Status = status;
        Content = new ReadOnlyCollection<byte>((content ?? []).ToArray());
        RejectionCode = rejectionCode;
    }

    public AssetReadStatus Status { get; }
    public IReadOnlyList<byte> Content { get; }
    public string? RejectionCode { get; }

    public static AssetReadResult Loaded(IEnumerable<byte> content) =>
        new(AssetReadStatus.Loaded, content ?? throw new ArgumentNullException(nameof(content)), null);

    public static AssetReadResult NotFound() => new(AssetReadStatus.NotFound, null, null);

    public static AssetReadResult Rejected(string code) => new(AssetReadStatus.Rejected, null, code);
}

public interface IAssetSourceProvider
{
    AssetSourceDescriptor DescribeSource();
    AssetSourceResolution Resolve(AssetSourceRequest request);
    AssetReadResult OpenRead(AssetSourceEntry entry, AssetReadBudget budget);
}

internal static class ProviderContractValidation
{
    internal static void Identifier(string value, string parameterName)
    {
        Text(value, parameterName, 128);
        if (value.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')))
            throw new ArgumentException("Identifier contains an unsupported character.", parameterName);
    }

    internal static void LogicalId(string value, string parameterName)
    {
        Text(value, parameterName, 256);
        if (value.StartsWith('/') || value.EndsWith('/') || value.Contains('\\') || value.Contains(':'))
            throw new ArgumentException("Logical ID must be relative and slash-separated.", parameterName);
        if (value.Split('/').Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw new ArgumentException("Logical ID contains an invalid segment.", parameterName);
    }

    internal static void Text(string value, string parameterName, int maximumLength)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength || value.Any(char.IsControl))
            throw new ArgumentException("Value must be nonblank, bounded text without control characters.", parameterName);
    }

    internal static void Sha256(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (value.Length != 64 || value.Any(character => !char.IsAsciiHexDigit(character)))
            throw new ArgumentException("Value must be a SHA-256 hexadecimal digest.", parameterName);
    }
}
