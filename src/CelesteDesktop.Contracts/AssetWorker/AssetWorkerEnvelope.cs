namespace CelesteDesktop.Contracts.AssetWorker;

public sealed record AssetWorkerEnvelope(
    int ProtocolVersion,
    AssetWorkerMessageType MessageType,
    Guid RequestId,
    string? ErrorCode = null);
