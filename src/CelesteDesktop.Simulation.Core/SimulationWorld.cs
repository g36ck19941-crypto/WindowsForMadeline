using System.Collections.ObjectModel;

namespace CelesteDesktop.Simulation.Core;

public sealed class SimulationWorld
{
    private readonly List<Actor> _actors = [];
    private readonly List<Solid> _solids = [];
    private readonly List<SimulationEvent> _events = [];
    private readonly HashSet<string> _ids = new(StringComparer.Ordinal);
    private bool _activeStep;

    public long Tick { get; private set; }
    public IReadOnlyList<Actor> Actors => _actors.AsReadOnly();
    public IReadOnlyList<Solid> Solids => _solids.AsReadOnly();
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
            solid.YSubpixel)));

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
