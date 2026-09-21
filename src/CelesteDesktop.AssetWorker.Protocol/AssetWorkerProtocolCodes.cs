namespace CelesteDesktop.AssetWorker.Protocol;

public static class AssetWorkerProtocolCodes
{
    public const string FrameTruncated = "IPC_FRAME_TRUNCATED";
    public const string FrameLengthInvalid = "IPC_FRAME_LENGTH_INVALID";
    public const string FrameTooLarge = "IPC_FRAME_TOO_LARGE";
    public const string JsonInvalid = "IPC_JSON_INVALID";
    public const string VersionUnsupported = "IPC_VERSION_UNSUPPORTED";
    public const string MessageInvalid = "IPC_MESSAGE_INVALID";
    public const string RequestIdInvalid = "IPC_REQUEST_ID_INVALID";
    public const string ErrorCodeInvalid = "IPC_ERROR_CODE_INVALID";
    public const string MessageUnexpected = "IPC_MESSAGE_UNEXPECTED";
}
