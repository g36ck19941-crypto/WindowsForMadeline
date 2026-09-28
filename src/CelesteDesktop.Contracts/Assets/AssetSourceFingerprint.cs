using System.Collections.ObjectModel;

namespace CelesteDesktop.Contracts.Assets;

public sealed class AssetSourceFingerprint
{
    public AssetSourceFingerprint(
        string profileId,
        IEnumerable<AssetFileFingerprint> files)
    {
        ArgumentNullException.ThrowIfNull(profileId);
        ArgumentNullException.ThrowIfNull(files);

        ProfileId = profileId;
        Files = new ReadOnlyCollection<AssetFileFingerprint>(files.ToArray());
    }

    public string ProfileId { get; }

    public IReadOnlyList<AssetFileFingerprint> Files { get; }
}
