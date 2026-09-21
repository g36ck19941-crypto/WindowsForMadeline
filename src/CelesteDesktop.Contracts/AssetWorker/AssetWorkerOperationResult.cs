namespace CelesteDesktop.Contracts.AssetWorker;

public sealed record AssetWorkerOperationResult(
    bool Succeeded,
    string Code,
    AssetWorkerState State,
    string? DetailCode = null,
    int? ExitCode = null,
    string? RecoveryAction = null);
