namespace CelesteDesktop.Entity.Refill;

public readonly record struct RefillInput
{
    public RefillInput(RefillContact? contact = null, bool disableRequested = false)
    {
        if (disableRequested && contact is not null)
        {
            throw new ArgumentException("A disabled Refill cannot receive a contact.", nameof(contact));
        }

        Contact = contact;
        DisableRequested = disableRequested;
    }

    public RefillContact? Contact { get; }
    public bool DisableRequested { get; }

    public static RefillInput None { get; } = new();
    public static RefillInput Disabled { get; } = new(disableRequested: true);
}
