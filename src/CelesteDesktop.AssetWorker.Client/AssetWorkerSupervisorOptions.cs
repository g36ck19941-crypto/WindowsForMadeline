namespace CelesteDesktop.AssetWorker.Client;

public sealed record AssetWorkerSupervisorOptions(
    TimeSpan StartupTimeout,
    TimeSpan RequestTimeout,
    TimeSpan ShutdownTimeout)
{
    public static AssetWorkerSupervisorOptions Default { get; } = new(
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(2));

    public void Validate()
    {
        ValidateTimeout(StartupTimeout, nameof(StartupTimeout));
        ValidateTimeout(RequestTimeout, nameof(RequestTimeout));
        ValidateTimeout(ShutdownTimeout, nameof(ShutdownTimeout));
    }

    private static void ValidateTimeout(TimeSpan value, string name)
    {
        if (value <= TimeSpan.Zero || value > TimeSpan.FromMinutes(1))
        {
            throw new ArgumentOutOfRangeException(
                name,
                "Worker timeouts must be greater than zero and at most one minute.");
        }
    }
}
