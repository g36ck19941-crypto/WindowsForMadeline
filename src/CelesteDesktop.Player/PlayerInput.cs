namespace CelesteDesktop.Player;

public readonly record struct PlayerInput
{
    public PlayerInput(int moveX, int moveY, bool jumpPressed, bool jumpHeld)
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
    }

    public int MoveX { get; }
    public int MoveY { get; }
    public bool JumpPressed { get; }
    public bool JumpHeld { get; }
}
