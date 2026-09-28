using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class NormalizedAssetCatalog
{
    public NormalizedAssetCatalog(
        AssetSourceFingerprint source,
        IEnumerable<EntityAssetCatalog> entities,
        string catalogSha256,
        int decodedPageCount)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(catalogSha256);

        Source = source;
        Entities = new ReadOnlyCollection<EntityAssetCatalog>(entities.ToArray());
        CatalogSha256 = catalogSha256;
        DecodedPageCount = decodedPageCount;
    }

    public AssetSourceFingerprint Source { get; }

    public IReadOnlyList<EntityAssetCatalog> Entities { get; }

    public string CatalogSha256 { get; }

    public int DecodedPageCount { get; }
}
