namespace CelesteDesktop.AssetWorker.Catalog;

public sealed class AssetCatalogException : Exception
{
    public AssetCatalogException(string code)
        : base(code)
    {
        Code = code;
    }

    public string Code { get; }
}
