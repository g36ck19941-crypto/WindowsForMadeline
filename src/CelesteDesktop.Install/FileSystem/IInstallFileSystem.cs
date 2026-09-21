namespace CelesteDesktop.Install.FileSystem;

public interface IInstallFileSystem
{
    string GetFullPath(string path);

    FileSystemEntryStatus GetEntryStatus(string absolutePath);
}
