namespace CelesteDesktop.Animation;

internal sealed class AnimationDiagnosticEmitter(Action<AnimationDiagnosticEvent>? emit, string entityId)
{
    private readonly Action<AnimationDiagnosticEvent> _emit = emit ?? (_ => { });
    private long _sequence;

    public void Success(string eventId, string stage, long tick, string animationId, int? frameIndex = null, string? fingerprint = null) =>
        _emit(new AnimationDiagnosticEvent(++_sequence, eventId, "Animation", "Info", stage, "succeeded", tick, entityId, animationId, frameIndex, fingerprint));

    public void Failure(AnimationPipelineException exception, long tick, string animationId)
    {
        static string? Inner(Exception? value) => value is null ? null : $"{value.GetType().FullName}: {value.Message}\n{value.StackTrace}";
        _emit(new AnimationDiagnosticEvent(
            ++_sequence,
            exception.Code,
            "Animation",
            "Error",
            exception.Stage,
            "failed",
            tick,
            entityId,
            animationId,
            DetailCode: exception.Code,
            ExceptionType: exception.GetType().FullName,
            Message: exception.Message,
            HResult: exception.HResult,
            Stack: exception.StackTrace,
            Inner: Inner(exception.InnerException),
            Recoverable: false,
            RecoveryAction: "none"));
    }
}
