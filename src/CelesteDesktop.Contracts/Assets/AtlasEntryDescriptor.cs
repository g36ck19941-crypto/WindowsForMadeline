namespace CelesteDesktop.Contracts.Assets;

public sealed record AtlasEntryDescriptor(
    string Id,
    int PageIndex,
    int X,
    int Y,
    int Width,
    int Height,
    int TrimOffsetX,
    int TrimOffsetY,
    int FrameWidth,
    int FrameHeight);
