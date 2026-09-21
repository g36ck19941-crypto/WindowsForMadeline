namespace CelesteDesktop.AssetWorker.Protocol;

public sealed class AssetWorkerProtocolException : Exception
{
    public AssetWorkerProtocolException(string code)
        : base(code)
    {
        Code = code;
    }

    public AssetWorkerProtocolException(string code, Exception innerException)
        : base(code, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
