using System.Collections.ObjectModel;

namespace CelesteDesktop.Install;

public sealed class InstallValidationProfile
{
    public InstallValidationProfile(string id, IEnumerable<string> requiredFiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(requiredFiles);

        var files = requiredFiles.ToArray();
        if (files.Length == 0)
        {
            throw new ArgumentException("At least one required file is needed.", nameof(requiredFiles));
        }

        Id = id;
        RequiredFiles = new ReadOnlyCollection<string>(files);
    }

    public string Id { get; }

    public IReadOnlyList<string> RequiredFiles { get; }
}
