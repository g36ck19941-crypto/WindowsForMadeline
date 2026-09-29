namespace CelesteDesktop.Animation;

public sealed class AnimationPipelineException : Exception
{
    public AnimationPipelineException(string code, string stage, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Stage = stage;
    }

    public string Code { get; }
    public string Stage { get; }
}
