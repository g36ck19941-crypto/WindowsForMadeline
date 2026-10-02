namespace CelesteDesktop.Player;

public readonly record struct PlayerInput
{
    public PlayerInput(
        int moveX,
        int moveY,
        bool jumpPressed,
        bool jumpHeld,
        bool dashPressed = false,
        bool grabHeld = false,
        bool dropThroughPressed = false)
    {
        if (moveX is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moveX));
        }
        if (moveY is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(moveY));
        }

        MoveX = moveX;
        MoveY = moveY;
        JumpPressed = jumpPressed;
        JumpHeld = jumpHeld;
        DashPressed = dashPressed;
        GrabHeld = grabHeld;
        DropThroughPressed = dropThroughPressed;
    }

    public int MoveX { get; }
    public int MoveY { get; }
    public bool JumpPressed { get; }
    public bool JumpHeld { get; }
    public bool DashPressed { get; }
    public bool GrabHeld { get; }
    public bool DropThroughPressed { get; }
}
