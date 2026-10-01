namespace CelesteDesktop.Entity.Puffer;

public readonly record struct PufferInput
{
    public PufferInput(PufferContact? contact = null, bool disableRequested = false)
    {
        if (disableRequested && contact is not null)
        {
            throw new ArgumentException("A disabled Puffer cannot receive a contact.", nameof(contact));
        }

        Contact = contact;
        DisableRequested = disableRequested;
    }

    public PufferContact? Contact { get; }
    public bool DisableRequested { get; }

    public static PufferInput None { get; } = new();
    public static PufferInput Disabled { get; } = new(disableRequested: true);
}
