namespace CelesteDesktop.Entity.Bumper;

public readonly record struct BumperInput
{
    public BumperInput(BumperContact? contact = null, bool disableRequested = false)
    {
        if (disableRequested && contact is not null)
        {
            throw new ArgumentException("A disabled Bumper cannot receive a contact.", nameof(contact));
        }

        Contact = contact;
        DisableRequested = disableRequested;
    }

    public BumperContact? Contact { get; }
    public bool DisableRequested { get; }

    public static BumperInput None { get; } = new();
    public static BumperInput Disabled { get; } = new(disableRequested: true);
}
