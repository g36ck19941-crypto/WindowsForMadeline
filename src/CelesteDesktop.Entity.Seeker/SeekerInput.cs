namespace CelesteDesktop.Entity.Seeker;

public readonly record struct SeekerInput
{
    public SeekerInput(
        SeekerTarget? target = null,
        bool wallCollision = false,
        bool disableRequested = false)
    {
        if (disableRequested && (target is not null || wallCollision))
        {
            throw new ArgumentException("A disabled Seeker cannot receive target or wall contact.");
        }

        Target = target;
        WallCollision = wallCollision;
        DisableRequested = disableRequested;
    }

    public SeekerTarget? Target { get; }
    public bool WallCollision { get; }
    public bool DisableRequested { get; }

    public static SeekerInput None { get; } = new();
    public static SeekerInput Disabled { get; } = new(disableRequested: true);
}
