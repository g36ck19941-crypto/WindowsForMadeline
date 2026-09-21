namespace CelesteDesktop.AssetWorker.Meta;

public sealed class AtlasMetadataException : Exception
{
    public AtlasMetadataException(string code)
        : base(code)
    {
        Code = code;
    }

    public AtlasMetadataException(string code, Exception innerException)
        : base(code, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
