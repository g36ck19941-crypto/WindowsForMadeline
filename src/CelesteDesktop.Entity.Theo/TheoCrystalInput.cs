namespace CelesteDesktop.Entity.Theo;

public readonly record struct TheoCrystalInput
{
    public TheoCrystalInput(TheoCrystalAction action, TheoHolderSnapshot? holder = null)
    {
        if (action is not TheoCrystalAction.None && holder is null)
        {
            throw new ArgumentException("Pickup, drop and throw actions require a holder snapshot.", nameof(holder));
        }

        Action = action;
        Holder = holder;
    }

    public TheoCrystalAction Action { get; }
    public TheoHolderSnapshot? Holder { get; }

    public static TheoCrystalInput None { get; } = new(TheoCrystalAction.None);
}
