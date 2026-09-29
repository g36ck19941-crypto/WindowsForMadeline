namespace CelesteDesktop.Entity.Glider;

public readonly record struct GliderInput
{
    public GliderInput(
        GliderAction action,
        GliderHolderSnapshot? holder = null,
        bool destroyRequested = false)
    {
        if (action is not GliderAction.None && holder is null)
        {
            throw new ArgumentException("Pickup, drop and throw actions require a holder snapshot.", nameof(holder));
        }
        if (destroyRequested && action is not GliderAction.None)
        {
            throw new ArgumentException("Destroy cannot be combined with a holder action.", nameof(destroyRequested));
        }

        Action = action;
        Holder = holder;
        DestroyRequested = destroyRequested;
    }

    public GliderAction Action { get; }
    public GliderHolderSnapshot? Holder { get; }
    public bool DestroyRequested { get; }

    public static GliderInput None { get; } = new(GliderAction.None);
    public static GliderInput Destroy { get; } = new(GliderAction.None, destroyRequested: true);
}
