namespace CelesteDesktop.Rendering;

public sealed class RenderBackendException : Exception
{
    public RenderBackendException(
        string code,
        string stage,
        string message,
        int hresult,
        bool recoverable,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        Code = code;
        Stage = stage;
        HResult = hresult;
        Recoverable = recoverable;
    }

    public string Code { get; }
    public string Stage { get; }
    public bool Recoverable { get; }
}
