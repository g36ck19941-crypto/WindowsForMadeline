using System.Collections.ObjectModel;

namespace CelesteDesktop.Entity.Water;

public sealed class WaterInput
{
    public WaterInput(IEnumerable<WaterContact>? contacts = null, bool disableRequested = false)
    {
        var copied = contacts?.ToArray() ?? [];
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException("Water contacts cannot contain null.", nameof(contacts));
        }
        if (disableRequested && copied.Length > 0)
        {
            throw new ArgumentException("Disabled Water cannot receive contacts.", nameof(contacts));
        }
        if (copied.Select(item => item.TargetId).Distinct(StringComparer.Ordinal).Count() != copied.Length)
        {
            throw new ArgumentException("Water contacts must have unique target IDs.", nameof(contacts));
        }

        Contacts = new ReadOnlyCollection<WaterContact>(copied);
        DisableRequested = disableRequested;
    }

    public IReadOnlyList<WaterContact> Contacts { get; }
    public bool DisableRequested { get; }

    public static WaterInput None { get; } = new();
    public static WaterInput Disabled { get; } = new(disableRequested: true);
}
