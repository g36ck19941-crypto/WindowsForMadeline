namespace CelesteDesktop.Contracts.AssetWorker;

public static class AssetWorkerOperationCodes
{
    public const string Succeeded = "WORKER_OPERATION_SUCCEEDED";
    public const string AlreadyReady = "WORKER_ALREADY_READY";
    public const string NotReady = "WORKER_NOT_READY";
    public const string StartTimeout = "WORKER_START_TIMEOUT";
    public const string RequestTimeout = "WORKER_REQUEST_TIMEOUT";
    public const string ShutdownTimeout = "WORKER_SHUTDOWN_TIMEOUT";
    public const string Cancelled = "WORKER_CANCELLED";
    public const string Exited = "WORKER_EXITED";
    public const string ProtocolRejected = "WORKER_PROTOCOL_REJECTED";
    public const string UnexpectedResponse = "WORKER_RESPONSE_UNEXPECTED";
    public const string LaunchFailed = "WORKER_LAUNCH_FAILED";
    public const string Disposed = "WORKER_SUPERVISOR_DISPOSED";
}
