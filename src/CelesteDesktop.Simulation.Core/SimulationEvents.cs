namespace CelesteDesktop.Simulation.Core;

public enum MovementAxis
{
    Horizontal,
    Vertical
}

public enum SimulationEventKind
{
    ActorBlocked,
    ActorCarried,
    ActorPushed,
    ActorSquished
}

public sealed record SimulationEvent(
    long Tick,
    SimulationEventKind Kind,
    string ActorId,
    string SolidId,
    MovementAxis Axis,
    int RequestedPixels,
    int MovedPixels,
    string? BlockingSolidId);
