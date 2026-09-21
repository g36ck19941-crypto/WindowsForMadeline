namespace CelesteDesktop.AssetWorker.Data;

public sealed class AtlasDataException : Exception
{
    public AtlasDataException(string code)
        : base(code)
    {
        Code = code;
    }

    public string Code { get; }
}
