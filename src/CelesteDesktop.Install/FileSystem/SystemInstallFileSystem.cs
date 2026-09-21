namespace CelesteDesktop.Install.FileSystem;

public sealed class SystemInstallFileSystem : IInstallFileSystem
{
    public string GetFullPath(string path) => Path.GetFullPath(path);

    public FileSystemEntryStatus GetEntryStatus(string absolutePath)
    {
        try
        {
            var attributes = File.GetAttributes(absolutePath);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                return FileSystemEntryStatus.ReparsePoint;
            }

            return (attributes & FileAttributes.Directory) != 0
                ? FileSystemEntryStatus.Directory
                : FileSystemEntryStatus.File;
        }
        catch (FileNotFoundException)
        {
            return FileSystemEntryStatus.Missing;
        }
        catch (DirectoryNotFoundException)
        {
            return FileSystemEntryStatus.Missing;
        }
        catch (UnauthorizedAccessException)
        {
            return FileSystemEntryStatus.Inaccessible;
        }
        catch (IOException)
        {
            return FileSystemEntryStatus.Inaccessible;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException or
            System.Security.SecurityException)
        {
            return FileSystemEntryStatus.Inaccessible;
        }
    }
}
