namespace CelesteDesktop.Entity.Spring;

public readonly record struct SpringInput
{
    public SpringInput(SpringContact? contact = null, bool disableRequested = false)
    {
        if (disableRequested && contact is not null)
        {
            throw new ArgumentException("A disabled Spring cannot receive a contact.", nameof(contact));
        }

        Contact = contact;
        DisableRequested = disableRequested;
    }

    public SpringContact? Contact { get; }
    public bool DisableRequested { get; }

    public static SpringInput None { get; } = new();
    public static SpringInput Disabled { get; } = new(disableRequested: true);
}
