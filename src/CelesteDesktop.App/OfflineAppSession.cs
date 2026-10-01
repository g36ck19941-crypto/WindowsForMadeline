using CelesteDesktop.Animation;
using CelesteDesktop.Entity.Bumper;
using CelesteDesktop.Entity.Glider;
using CelesteDesktop.Entity.Puffer;
using CelesteDesktop.Entity.Refill;
using CelesteDesktop.Entity.Seeker;
using CelesteDesktop.Entity.Spring;
using CelesteDesktop.Entity.Theo;
using CelesteDesktop.Entity.Water;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

namespace CelesteDesktop.App;

public sealed class OfflineAppSession : IDisposable
{
    public const string PresentationComponent = "presentation";
    public const string PlayerComponent = "player";
    public const string TheoComponent = "theo";
    public const string GliderComponent = "glider";
    public const string SpringComponent = "spring";
    public const string RefillComponent = "refill";
    public const string WaterComponent = "water";
    public const string BumperComponent = "bumper";
    public const string PufferComponent = "puffer";
    public const string SeekerComponent = "seeker";

    private readonly OfflineAppComponents _components;
    private readonly IAppPresentationStage _presentation;
    private readonly Action<AppDiagnosticEvent>? _diagnostics;
    private readonly HashSet<string> _disabled = new(StringComparer.Ordinal);
    private readonly List<AppDiagnosticEvent> _tickEvents = [];
    private long _sequence;
    private bool _disposed;
    private TheoCrystalSnapshot? _theo;
    private GliderSnapshot? _glider;
    private SpringSnapshot? _spring;
    private RefillSnapshot? _refill;
    private WaterSnapshot? _water;
    private BumperSnapshot? _bumper;
    private PufferSnapshot? _puffer;
    private SeekerSnapshot? _seeker;

    public OfflineAppSession(
        OfflineAppComponents components,
        IAppPresentationStage presentation,
        Action<AppDiagnosticEvent>? diagnostics = null)
    {
        _components = components ?? throw new ArgumentNullException(nameof(components));
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        _diagnostics = diagnostics;
    }

    public AppLifecycleState State { get; private set; } = AppLifecycleState.Created;
    public long Tick => _components.World.Tick;
    public IReadOnlyCollection<string> DisabledComponents =>
        Array.AsReadOnly(_disabled.Order(StringComparer.Ordinal).ToArray());

    public void Start()
    {
        EnsureNotDisposed();
        if (State != AppLifecycleState.Created) throw new InvalidOperationException("App can start only once from Created state.");
        TryStartPresentation();
        State = AppLifecycleState.Running;
        Emit(0, AppEventIds.Started, "lifecycle", "succeeded", "app", null, null, null, includeInTick: false);
    }

    public void Pause()
    {
        EnsureNotDisposed();
        if (State != AppLifecycleState.Running) throw new InvalidOperationException("Only a running App can pause.");
        State = AppLifecycleState.Paused;
        Emit(Tick, AppEventIds.Paused, "lifecycle", "succeeded", "app", null, null, null, includeInTick: false);
    }

    public void Resume()
    {
        EnsureNotDisposed();
        if (State != AppLifecycleState.Paused) throw new InvalidOperationException("Only a paused App can resume.");
        State = AppLifecycleState.Running;
        Emit(Tick, AppEventIds.Resumed, "lifecycle", "succeeded", "app", null, null, null, includeInTick: false);
    }

    public OfflineAppSnapshot Step(OfflineAppTickInput input)
    {
        EnsureNotDisposed();
        ArgumentNullException.ThrowIfNull(input);
        if (State != AppLifecycleState.Running) throw new InvalidOperationException("App ticks require Running state.");
        if (input.Tick != checked(_components.World.Tick + 1)) throw new InvalidOperationException("App ticks must be consecutive and match the simulation world.");

        _tickEvents.Clear();
        Emit(input.Tick, AppEventIds.TickStarted, "tick", "started", "app", null, null, null);
        var router = new EffectRouter(this, input.Tick, _components);
        PlayerTraversalSnapshot? player = null;
        SimulationSnapshot simulation;
        try
        {
            simulation = _components.World.Step(world =>
            {
                _spring = TryOptional(SpringComponent, input.Tick, () => _components.Spring.Update(input.Spring, world), _spring);
                if (_spring?.LaunchEffect is { } springEffect) router.RouteVelocity(SpringComponent, springEffect.TargetId, springEffect.Velocity);

                _refill = TryOptional(RefillComponent, input.Tick, () => _components.Refill.Update(input.Refill, world), _refill);
                if (_refill?.RestoreEffect is { } restore) router.RouteResources(RefillComponent, restore.TargetId, restore.Resources);

                _water = TryOptional(WaterComponent, input.Tick, () => _components.Water.Update(input.Water, world), _water);
                if (_water is not null)
                    foreach (var motion in _water.MotionEffects) router.RouteVelocity(WaterComponent, motion.TargetId, motion.Velocity);

                _bumper = TryOptional(BumperComponent, input.Tick, () => _components.Bumper.Update(input.Bumper, world), _bumper);
                if (_bumper?.LaunchEffect is { } bumperLaunch) router.RouteVelocity(BumperComponent, bumperLaunch.TargetId, bumperLaunch.Velocity);

                _puffer = TryOptional(PufferComponent, input.Tick, () => _components.Puffer.Update(input.Puffer, world), _puffer);
                if (_puffer?.LaunchEffect is { } pufferLaunch) router.RouteVelocity(PufferComponent, pufferLaunch.TargetId, pufferLaunch.Velocity);

                _seeker = TryOptional(SeekerComponent, input.Tick, () => _components.Seeker.Update(input.Seeker, world), _seeker);
                if (_seeker?.HitEffect is { } hit)
                    Emit(input.Tick, AppEventIds.SeekerHitObserved, "route", "observed", SeekerComponent, hit.TargetId, "hit-fact-only", null);

                _theo = TryOptional(TheoComponent, input.Tick,
                    () => _components.Theo.Update(input.Theo, router.TakeVelocity(_components.Theo.Actor.Id), world), _theo);
                _glider = TryOptional(GliderComponent, input.Tick,
                    () => _components.Glider.Update(input.Glider, router.TakeVelocity(_components.Glider.Actor.Id), world), _glider);
                if (_glider?.HolderEffect is { LimitRequired: true } holder)
                    router.RouteMaximumFallSpeed(GliderComponent, holder.HolderId, holder.MaximumFallSpeed);

                player = _components.Player.Update(input.Player, router.BuildPlayerEffects(), world);
            });
            Emit(input.Tick, AppEventIds.SimulationCompleted, "simulation", "succeeded", "simulation", null, null, null);
        }
        catch (Exception exception)
        {
            State = AppLifecycleState.Faulted;
            Emit(input.Tick, AppEventIds.Faulted, "simulation", "failed", PlayerComponent, null, exception.Message, exception);
            throw new AppRuntimeException("simulation", PlayerComponent, "The required App simulation stage failed.", exception);
        }

        OfflineAnimationPresentationResult? presentation = null;
        if (!_disabled.Contains(PresentationComponent))
        {
            try
            {
                presentation = _presentation.Present(input.Animation);
                Emit(input.Tick, AppEventIds.PresentationCompleted, "presentation", "succeeded", PresentationComponent, null,
                    presentation.Presentation.Succeeded ? "presented" : presentation.Presentation.FailureCode, null);
                if (!presentation.Presentation.Succeeded)
                {
                    DisableComponent(PresentationComponent, input.Tick, "presentation", presentation.Presentation.FailureCode ?? "presentation-failed");
                    SafeStopPresentation(input.Tick);
                }
            }
            catch (Exception exception)
            {
                DisableComponent(PresentationComponent, input.Tick, "presentation", exception);
                SafeStopPresentation(input.Tick);
            }
        }

        Emit(input.Tick, AppEventIds.TickCompleted, "tick", "succeeded", "app", null, null, null);
        return new OfflineAppSnapshot(
            input.Tick,
            State,
            simulation,
            player ?? throw new InvalidOperationException("Player snapshot was not produced."),
            _theo,
            _glider,
            _spring,
            _refill,
            _water,
            _bumper,
            _puffer,
            _seeker,
            presentation,
            _disabled,
            _tickEvents);
    }

    public void Stop()
    {
        EnsureNotDisposed();
        if (State == AppLifecycleState.Stopped) return;
        if (State is AppLifecycleState.Created or AppLifecycleState.Disposed)
            throw new InvalidOperationException("App must be started before it can stop.");
        SafeStopPresentation(Tick);
        State = AppLifecycleState.Stopped;
        Emit(Tick, AppEventIds.Stopped, "lifecycle", "succeeded", "app", null, null, null, includeInTick: false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (State is AppLifecycleState.Running or AppLifecycleState.Paused or AppLifecycleState.Faulted)
            SafeStopPresentation(Tick);
        try { _presentation.Dispose(); }
        catch (Exception exception)
        {
            DisableComponent(PresentationComponent, Tick, "presentation-dispose", exception, includeInTick: false);
        }
        finally
        {
            _disposed = true;
            State = AppLifecycleState.Disposed;
            Emit(Tick, AppEventIds.Disposed, "lifecycle", "succeeded", "app", null, null, null, includeInTick: false);
        }
    }

    private T? TryOptional<T>(string componentId, long tick, Func<T> update, T? previous) where T : class
    {
        if (_disabled.Contains(componentId)) return previous;
        try { return update(); }
        catch (Exception exception)
        {
            DisableComponent(componentId, tick, "simulation", exception);
            return previous;
        }
    }

    private void TryStartPresentation()
    {
        try { _presentation.Start(); }
        catch (Exception exception)
        {
            DisableComponent(PresentationComponent, 0, "presentation-start", exception, includeInTick: false);
            SafeStopPresentation(0);
        }
    }

    private void SafeStopPresentation(long tick)
    {
        try { _presentation.Stop(); }
        catch (Exception exception)
        {
            DisableComponent(PresentationComponent, tick, "presentation-stop", exception, includeInTick: false);
        }
    }

    private void DisableComponent(string componentId, long tick, string stage, Exception exception, bool includeInTick = true)
    {
        if (_disabled.Add(componentId))
            Emit(tick, AppEventIds.ComponentDisabled, stage, "failed", componentId, null, "isolated", exception, includeInTick);
    }

    private void DisableComponent(string componentId, long tick, string stage, string detail)
    {
        if (_disabled.Add(componentId))
            Emit(tick, AppEventIds.ComponentDisabled, stage, "failed", componentId, null, detail, null);
    }

    private void Emit(
        long tick,
        string eventId,
        string stage,
        string outcome,
        string componentId,
        string? targetId,
        string? detail,
        Exception? exception,
        bool includeInTick = true)
    {
        var item = new AppDiagnosticEvent(
            checked(++_sequence), tick, eventId, stage, outcome, componentId, targetId, detail,
            exception is null ? null : AppExceptionInfo.Capture(exception));
        if (includeInTick) _tickEvents.Add(item);
        _diagnostics?.Invoke(item);
    }

    private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private sealed class EffectRouter
    {
        private readonly OfflineAppSession _owner;
        private readonly long _tick;
        private readonly HashSet<string> _knownTargets;
        private readonly Dictionary<string, RoutedVelocity> _velocities = new(StringComparer.Ordinal);
        private ExternalResourceEffect? _resources;
        private decimal? _maximumFallSpeed;

        public EffectRouter(OfflineAppSession owner, long tick, OfflineAppComponents components)
        {
            _owner = owner;
            _tick = tick;
            _knownTargets = new HashSet<string>(StringComparer.Ordinal)
            {
                components.Player.Actor.Id,
                components.Theo.Actor.Id,
                components.Glider.Actor.Id
            };
            PlayerId = components.Player.Actor.Id;
        }

        public string PlayerId { get; }

        public void RouteVelocity(string source, string target, ExternalVelocityEffect effect)
        {
            if (!Known(source, target)) return;
            _velocities.TryGetValue(target, out var current);
            var x = MergeAxis(source, target, "x", current.X, effect.SpeedX);
            var y = MergeAxis(source, target, "y", current.Y, effect.SpeedY);
            _velocities[target] = new RoutedVelocity(x, y);
            _owner.Emit(_tick, AppEventIds.EffectRouted, "route", "succeeded", source, target, "velocity", null);
        }

        public void RouteResources(string source, string target, ExternalResourceEffect effect)
        {
            if (!Known(source, target)) return;
            if (!string.Equals(target, PlayerId, StringComparison.Ordinal))
            {
                _owner.Emit(_tick, AppEventIds.EffectTargetUnresolved, "route", "ignored", source, target, "resources-not-supported", null);
                return;
            }
            if (_resources is not null)
            {
                _owner.Emit(_tick, AppEventIds.EffectConflict, "route", "first-wins", source, target, "resources", null);
                return;
            }
            _resources = effect;
            _owner.Emit(_tick, AppEventIds.EffectRouted, "route", "succeeded", source, target, "resources", null);
        }

        public void RouteMaximumFallSpeed(string source, string target, decimal value)
        {
            if (!Known(source, target)) return;
            if (!string.Equals(target, PlayerId, StringComparison.Ordinal))
            {
                _owner.Emit(_tick, AppEventIds.EffectTargetUnresolved, "route", "ignored", source, target, "fall-limit-not-supported", null);
                return;
            }
            _maximumFallSpeed = _maximumFallSpeed is null ? value : Math.Min(_maximumFallSpeed.Value, value);
            _owner.Emit(_tick, AppEventIds.EffectRouted, "route", "succeeded", source, target, "maximum-fall-speed", null);
        }

        public ExternalVelocityEffect? TakeVelocity(string target)
        {
            if (!_velocities.Remove(target, out var value)) return null;
            return new ExternalVelocityEffect(
                value.X.HasValue ? value.X.Value : null,
                value.Y.HasValue ? value.Y.Value : null);
        }

        public PlayerExternalEffects BuildPlayerEffects() =>
            new(_maximumFallSpeed, TakeVelocity(PlayerId), _resources);

        private bool Known(string source, string target)
        {
            if (_knownTargets.Contains(target)) return true;
            _owner.Emit(_tick, AppEventIds.EffectTargetUnresolved, "route", "ignored", source, target, "unknown-target", null);
            return false;
        }

        private AxisValue MergeAxis(string source, string target, string axis, AxisValue current, decimal? incoming)
        {
            if (incoming is null) return current;
            if (current.HasValue)
            {
                _owner.Emit(_tick, AppEventIds.EffectConflict, "route", "first-wins", source, target, axis, null);
                return current;
            }
            return new AxisValue(true, incoming.Value);
        }

        private readonly record struct AxisValue(bool HasValue, decimal Value);
        private readonly record struct RoutedVelocity(AxisValue X, AxisValue Y);
    }
}
