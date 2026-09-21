using CelesteDesktop.Install.FileSystem;

namespace CelesteDesktop.Install.Tests;

internal sealed class SyntheticInstallFileSystem : IInstallFileSystem
{
    private readonly Dictionary<string, FileSystemEntryStatus> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public int InspectionCount { get; private set; }

    public string GetFullPath(string path) => Path.GetFullPath(path);

    public FileSystemEntryStatus GetEntryStatus(string absolutePath)
    {
        InspectionCount++;
        return _entries.TryGetValue(GetFullPath(absolutePath), out var status)
            ? status
            : FileSystemEntryStatus.Missing;
    }

    public void Set(string path, FileSystemEntryStatus status) =>
        _entries[GetFullPath(path)] = status;

    public void Remove(string path) => _entries.Remove(GetFullPath(path));
}
