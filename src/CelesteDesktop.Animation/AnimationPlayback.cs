using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.Animation;

public sealed class AnimationPlayback
{
    public const int TicksPerSecond = 60;
    private readonly EntityAssetCatalog _entity;
    private readonly IReadOnlyDictionary<string, CatalogAnimationDescriptor> _animations;
    private readonly AnimationDiagnosticEmitter _diagnostics;
    private string? _requestedAnimationId;
    private CatalogAnimationDescriptor? _activeAnimation;
    private long _activeStartTick;
    private long _lastTick = -1;

    public AnimationPlayback(EntityAssetCatalog entity, Action<AnimationDiagnosticEvent>? emit = null)
        : this(entity, new AnimationDiagnosticEmitter(emit, entity?.EntityId ?? "unknown"))
    {
    }

    internal AnimationPlayback(EntityAssetCatalog entity, AnimationDiagnosticEmitter diagnostics)
    {
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        if (string.IsNullOrWhiteSpace(entity.EntityId) || entity.Animations.Count == 0)
        {
            throw new ArgumentException("Entity must contain an ID and at least one animation.", nameof(entity));
        }

        var animations = new Dictionary<string, CatalogAnimationDescriptor>(StringComparer.Ordinal);
        foreach (var animation in entity.Animations)
        {
            ValidateAnimation(animation);
            if (!animations.TryAdd(animation.Id, animation))
            {
                throw new ArgumentException("Animation IDs must be unique.", nameof(entity));
            }
        }
        _animations = animations;
    }

    public AnimationFrameSnapshot Resolve(AnimationTickInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Tick <= _lastTick)
        {
            throw new AnimationPipelineException("ANIMATION_TICK_NOT_MONOTONIC", "resolve-frame", "Animation ticks must increase.");
        }

        var transitioned = false;
        if (!string.Equals(_requestedAnimationId, input.RequestedAnimationId, StringComparison.Ordinal))
        {
            _activeAnimation = Find(input.RequestedAnimationId);
            _requestedAnimationId = input.RequestedAnimationId;
            _activeStartTick = input.Tick;
            transitioned = true;
            _diagnostics.Success("ANIMATION_STATE_SELECTED", "select", input.Tick, _activeAnimation.Id);
        }

        var active = _activeAnimation ?? throw new AnimationPipelineException("ANIMATION_STATE_MISSING", "resolve-frame", "Animation state is missing.");
        for (var transitionCount = 0; transitionCount < 32; transitionCount++)
        {
            var durationTicks = DurationTicks(active.DelaySeconds);
            var elapsedTicks = checked(input.Tick - _activeStartTick);
            var ordinal = elapsedTicks / durationTicks;
            if (active.IsLooping)
            {
                var index = checked((int)(ordinal % active.Frames.Count));
                return Snapshot(input.Tick, active, index, transitioned);
            }

            if (ordinal < active.Frames.Count)
            {
                return Snapshot(input.Tick, active, checked((int)ordinal), transitioned);
            }

            if (string.IsNullOrWhiteSpace(active.GotoExpression))
            {
                return Snapshot(input.Tick, active, active.Frames.Count - 1, transitioned);
            }

            _activeStartTick = checked(_activeStartTick + (durationTicks * active.Frames.Count));
            active = Find(active.GotoExpression);
            _activeAnimation = active;
            transitioned = true;
            _diagnostics.Success("ANIMATION_STATE_TRANSITIONED", "transition", input.Tick, active.Id);
        }

        throw new AnimationPipelineException("ANIMATION_TRANSITION_BUDGET_EXCEEDED", "transition", "Animation transition budget was exceeded.");
    }

    private AnimationFrameSnapshot Snapshot(long tick, CatalogAnimationDescriptor animation, int frameIndex, bool transitioned)
    {
        var frame = animation.Frames[frameIndex];
        _lastTick = tick;
        _diagnostics.Success("ANIMATION_FRAME_RESOLVED", "resolve-frame", tick, animation.Id, frameIndex, frame.Frame.ContentSha256);
        return new AnimationFrameSnapshot(
            tick,
            _entity.EntityId,
            animation.Id,
            frameIndex,
            frame.AtlasEntryId,
            frame.Frame,
            _entity.Origin,
            _entity.Position,
            transitioned);
    }

    private CatalogAnimationDescriptor Find(string id) =>
        _animations.TryGetValue(id, out var animation)
            ? animation
            : throw new AnimationPipelineException("ANIMATION_ID_NOT_FOUND", "resolve-animation", $"Animation '{id}' was not found.");

    private static int DurationTicks(double delaySeconds)
    {
        if (!double.IsFinite(delaySeconds) || delaySeconds < 0 || delaySeconds > 60)
        {
            throw new ArgumentOutOfRangeException(nameof(delaySeconds));
        }
        return Math.Max(1, checked((int)decimal.Ceiling((decimal)delaySeconds * TicksPerSecond)));
    }

    private static void ValidateAnimation(CatalogAnimationDescriptor animation)
    {
        if (string.IsNullOrWhiteSpace(animation.Id) || animation.Frames.Count == 0)
        {
            throw new ArgumentException("Animation must contain an ID and frames.", nameof(animation));
        }
        _ = DurationTicks(animation.DelaySeconds);
    }

}
