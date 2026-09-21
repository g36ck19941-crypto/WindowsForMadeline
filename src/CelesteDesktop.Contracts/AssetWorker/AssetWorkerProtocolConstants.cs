namespace CelesteDesktop.Contracts.AssetWorker;

public static class AssetWorkerProtocolConstants
{
    public const int Version = 1;
    public const int LengthPrefixBytes = sizeof(int);
    public const int MaximumPayloadBytes = 4 * 1024;
    public const int MaximumErrorCodeCharacters = 128;
}
