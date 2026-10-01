namespace CelesteDesktop.App;

public sealed record AppExceptionInfo(
    string Type,
    string Message,
    int HResult,
    string Stack,
    string? InnerType,
    string? InnerMessage)
{
    public static AppExceptionInfo Capture(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new AppExceptionInfo(
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message,
            exception.HResult,
            exception.StackTrace ?? string.Empty,
            exception.InnerException?.GetType().FullName,
            exception.InnerException?.Message);
    }
}
