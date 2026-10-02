using System.Collections.ObjectModel;

namespace CelesteDesktop.Simulation.Core;

public sealed class SimulationWorld
{
    private readonly List<Actor> _actors = [];
    private readonly List<Solid> _solids = [];
    private readonly List<OneWayPlatform> _oneWayPlatforms = [];
    private readonly List<SimulationEvent> _events = [];
    private readonly HashSet<string> _ids = new(StringComparer.Ordinal);
    private bool _activeStep;

    public long Tick { get; private set; }
    public bool IsAdvancing => _activeStep;
    public IReadOnlyList<Actor> Actors => _actors.AsReadOnly();
    public IReadOnlyList<Solid> Solids => _solids.AsReadOnly();
    public IReadOnlyList<OneWayPlatform> OneWayPlatforms => _oneWayPlatforms.AsReadOnly();
    public IReadOnlyList<SimulationEvent> Events => _events.AsReadOnly();

    public void Add(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        EnsureMutable();
        if (_solids.Any(solid => solid.Bounds.Intersects(actor.Bounds)))
        {
            throw new ArgumentException("An actor cannot start overlapped with a solid.", nameof(actor));
        }
        AddId(actor.Id);
        _actors.Add(actor);
    }

    public void Add(Solid solid)
    {
        ArgumentNullException.ThrowIfNull(solid);
        EnsureMutable();
        if (_actors.Any(actor => actor.Bounds.Intersects(solid.Bounds)))
        {
            throw new ArgumentException("A solid cannot start overlapped with an actor.", nameof(solid));
        }
        AddId(solid.Id);
        _solids.Add(solid);
    }

    public void Add(OneWayPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);
        EnsureMutable();
        AddId(platform.Id);
        _oneWayPlatforms.Add(platform);
    }

    public SimulationSnapshot Step(Action<SimulationWorld> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (_activeStep)
        {
            throw new InvalidOperationException("Simulation steps cannot be nested.");
        }

        _activeStep = true;
        Tick = checked(Tick + 1);
        _events.Clear();
        foreach (var actor in _actors)
        {
            actor.ResetForTick();
        }

        try
        {
            update(this);
            return CaptureSnapshot();
        }
        finally
        {
            _activeStep = false;
        }
    }

    public SimulationSnapshot CaptureSnapshot() => new(
        Tick,
        _actors.Select(actor => new ActorSnapshot(
            actor.Id,
            new SimPoint(actor.X, actor.Y),
            actor.Width,
            actor.Height,
            actor.XSubpixel,
            actor.YSubpixel,
            actor.LiftSpeed,
            actor.IsSquished)),
        _solids.Select(solid => new SolidSnapshot(
            solid.Id,
            new SimPoint(solid.X, solid.Y),
            solid.Width,
            solid.Height,
            solid.XSubpixel,
            solid.YSubpixel)),
        _oneWayPlatforms.Select(platform => new OneWayPlatformSnapshot(
            platform.Id,
            new SimPoint(platform.X, platform.Y),
            platform.Width,
            platform.Height)));

    public bool IsGrounded(Actor actor, string? ignoredOneWayPlatformId = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!_actors.Contains(actor))
        {
            throw new ArgumentException("Actor is not registered in this world.", nameof(actor));
        }
        return FirstCollision(actor.Bounds.Offset(0, 1), ignoredSolid: null) is not null ||
            FirstOneWayPlatformBelow(actor, ignoredOneWayPlatformId) is not null;
    }

    public OneWayPlatform? FirstOneWayPlatformBelow(
        Actor actor,
        string? ignoredOneWayPlatformId = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!_actors.Contains(actor))
        {
            throw new ArgumentException("Actor is not registered in this world.", nameof(actor));
        }

        foreach (var platform in _oneWayPlatforms)
        {
            if (!string.Equals(platform.Id, ignoredOneWayPlatformId, StringComparison.Ordinal) &&
                actor.Bounds.Bottom == platform.Bounds.Top &&
                actor.Bounds.OverlapsHorizontally(platform.Bounds))
            {
                return platform;
            }
        }
        return null;
    }

    public OneWayPlatform? FindOneWayPlatform(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _oneWayPlatforms.FirstOrDefault(
            platform => string.Equals(platform.Id, id, StringComparison.Ordinal));
    }

    public Solid? FirstSolidAt(Actor actor, int offsetX, int offsetY)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!_actors.Contains(actor))
        {
            throw new ArgumentException("Actor is not registered in this world.", nameof(actor));
        }
        return FirstCollision(actor.Bounds.Offset(offsetX, offsetY), ignoredSolid: null);
    }

    internal void RequireActiveStep()
    {
        if (!_activeStep)
        {
            throw new InvalidOperationException("Movement is only valid inside a fixed simulation step.");
        }
    }

    internal Solid? FirstCollision(SimRect bounds, Solid? ignoredSolid)
    {
        foreach (var solid in _solids)
        {
            if (!ReferenceEquals(solid, ignoredSolid) && bounds.Intersects(solid.Bounds))
            {
                return solid;
            }
        }
        return null;
    }

    internal OneWayPlatform? FirstOneWayCollision(
        SimRect currentBounds,
        SimRect candidateBounds,
        string? ignoredOneWayPlatformId)
    {
        foreach (var platform in _oneWayPlatforms)
        {
            if (string.Equals(platform.Id, ignoredOneWayPlatformId, StringComparison.Ordinal))
            {
                continue;
            }

            var bounds = platform.Bounds;
            if (currentBounds.Bottom <= bounds.Top &&
                candidateBounds.Bottom > bounds.Top &&
                candidateBounds.OverlapsHorizontally(bounds))
            {
                return platform;
            }
        }
        return null;
    }

    internal void RecordBlocked(
        Actor actor,
        Actor.ExactMoveResult result,
        MovementAxis axis,
        int requested) =>
        _events.Add(new SimulationEvent(
            Tick,
            SimulationEventKind.ActorBlocked,
            actor.Id,
            result.Blocker?.Id ?? string.Empty,
            axis,
            requested,
            result.MovedPixels,
            result.Blocker?.Id));

    internal void RecordSolidMotion(
        Actor actor,
        Solid solid,
        Actor.ExactMoveResult result,
        MovementAxis axis,
        int requested,
        SimulationEventKind kind) =>
        _events.Add(new SimulationEvent(
            Tick,
            kind,
            actor.Id,
            solid.Id,
            axis,
            requested,
            result.MovedPixels,
            result.Blocker?.Id));

    internal void RecordSquish(
        Actor actor,
        Solid solid,
        Actor.ExactMoveResult result,
        MovementAxis axis,
        int requested) =>
        _events.Add(new SimulationEvent(
            Tick,
            SimulationEventKind.ActorSquished,
            actor.Id,
            solid.Id,
            axis,
            requested,
            result.MovedPixels,
            result.Blocker?.Id));

    private void AddId(string id)
    {
        if (!_ids.Add(id))
        {
            throw new ArgumentException($"Duplicate simulation ID: {id}", nameof(id));
        }
    }

    private void EnsureMutable()
    {
        if (_activeStep)
        {
            throw new InvalidOperationException("Simulation membership cannot change during a step.");
        }
    }

}
