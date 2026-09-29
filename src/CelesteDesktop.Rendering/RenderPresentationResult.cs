namespace CelesteDesktop.Rendering;

public sealed record RenderPresentationResult(
    bool Succeeded,
    bool Recovered,
    bool PixelsChanged,
    string FrameFingerprint,
    long Sequence,
    string? FailureCode);
