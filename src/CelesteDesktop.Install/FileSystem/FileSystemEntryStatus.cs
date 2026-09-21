namespace CelesteDesktop.Install.FileSystem;

public enum FileSystemEntryStatus
{
    Missing,
    File,
    Directory,
    ReparsePoint,
    Inaccessible,
    Other
}
