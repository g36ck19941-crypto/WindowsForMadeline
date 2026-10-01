namespace CelesteDesktop.App;

public sealed class AppRuntimeException : Exception
{
    public AppRuntimeException(string stage, string componentId, string message, Exception innerException)
        : base(message, innerException)
    {
        Stage = stage;
        ComponentId = componentId;
    }

    public string Stage { get; }
    public string ComponentId { get; }
}
