namespace CelesteDesktop.Contracts.Assets;

public sealed record AssetFileFingerprint(
    string LogicalPath,
    long ByteLength,
    string Sha256);
