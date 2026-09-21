namespace CelesteDesktop.Contracts.AssetWorker;

public enum AssetWorkerMessageType
{
    Unknown = 0,
    Hello = 1,
    Ready = 2,
    Ping = 3,
    Pong = 4,
    Shutdown = 5,
    Stopped = 6,
    Error = 7
}
