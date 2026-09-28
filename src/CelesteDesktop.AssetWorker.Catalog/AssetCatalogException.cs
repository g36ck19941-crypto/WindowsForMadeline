namespace CelesteDesktop.AssetWorker.Catalog;

public sealed class AssetCatalogException : Exception
{
    public AssetCatalogException(string code, string? assetId = null, string? detail = null)
        : base(code)
    {
        Code = code;
        AssetId = assetId;
        Detail = detail;
    }

    public string Code { get; }

    public string? AssetId { get; }

    public string? Detail { get; }
}
