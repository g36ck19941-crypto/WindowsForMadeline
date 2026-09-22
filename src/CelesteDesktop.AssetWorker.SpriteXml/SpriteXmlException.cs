namespace CelesteDesktop.AssetWorker.SpriteXml;

public sealed class SpriteXmlException : Exception
{
    public SpriteXmlException(string code)
        : base(code)
    {
        Code = code;
    }

    public SpriteXmlException(string code, Exception innerException)
        : base(code, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
