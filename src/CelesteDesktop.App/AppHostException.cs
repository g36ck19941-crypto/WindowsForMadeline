namespace CelesteDesktop.App;

public sealed class AppHostException : Exception
{
    public AppHostException(string stage, string message, Exception innerException) : base(message, innerException) => Stage = stage;
    public string Stage { get; }
}
