namespace CelesteDesktop.Contracts.Install;

public static class InstallValidationCodes
{
    public const string RootRequired = "INSTALL_ROOT_REQUIRED";
    public const string RootNotAbsolute = "INSTALL_ROOT_NOT_ABSOLUTE";
    public const string RootInvalid = "INSTALL_ROOT_INVALID";
    public const string RootMissing = "INSTALL_ROOT_MISSING";
    public const string RootNotDirectory = "INSTALL_ROOT_NOT_DIRECTORY";
    public const string ReparsePointRejected = "INSTALL_REPARSE_POINT_REJECTED";
    public const string PathEscapeRejected = "INSTALL_PATH_ESCAPE_REJECTED";
    public const string RequiredFileMissing = "INSTALL_REQUIRED_FILE_MISSING";
    public const string EntryTypeInvalid = "INSTALL_ENTRY_TYPE_INVALID";
    public const string EntryInaccessible = "INSTALL_ENTRY_INACCESSIBLE";
    public const string ProfileInvalid = "INSTALL_PROFILE_INVALID";
}
