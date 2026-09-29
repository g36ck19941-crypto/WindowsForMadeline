namespace CelesteDesktop.Animation;

public sealed record AnimationTickInput
{
    public AnimationTickInput(long tick, string requestedAnimationId, int anchorX, int anchorY, bool flipX = false)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (string.IsNullOrWhiteSpace(requestedAnimationId)) throw new ArgumentException("Animation ID is required.", nameof(requestedAnimationId));
        Tick = tick;
        RequestedAnimationId = requestedAnimationId;
        AnchorX = anchorX;
        AnchorY = anchorY;
        FlipX = flipX;
    }

    public long Tick { get; }
    public string RequestedAnimationId { get; }
    public int AnchorX { get; }
    public int AnchorY { get; }
    public bool FlipX { get; }
}
