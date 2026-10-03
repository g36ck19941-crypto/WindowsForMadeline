using System.Text.Json;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

internal static class Cdr075Demo
{
    public static DuckingDemoResult Run()
    {
        var first = RunOnce();
        var second = RunOnce();
        if (!first.FeetPreserved || !first.NeverOverlapped || !first.RestoredStanding ||
            first.DuckHeight != 6 || first.StandingHeight != 11 ||
            first.StartedCount != 1 || first.BlockedCount != 2 || first.CompletedCount != 1)
            throw new InvalidOperationException("Generated duck clearance demonstration failed.");
        return first with { DeterministicReplay = JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second) };
    }

    private static DuckingDemoResult RunOnce()
    {
        var world = new SimulationWorld();
        var actor = new Actor("duck-player", 0, 0, 8, 11);
        world.Add(actor);
        world.Add(new Solid("duck-floor", -100, 11, 200, 8));
        var ceiling = new Solid("duck-low-ceiling", 10, 0, 20, 5);
        world.Add(ceiling);
        var player = new PlayerNormalController(actor, initialSpeed: new SimVector(90m, 0m));
        var rows = new List<DuckingDemoRow>();
        var neverOverlapped = true;
        for (var tick = 1; tick <= 9; tick++)
        {
            var result = player.Step(new PlayerInput(0, 0, false, false, duckHeld: tick <= 6), world,
                current => { if (tick == 9) ceiling.Move(50m, 0m, current); });
            neverOverlapped &= !result.CollisionBounds.Intersects(ceiling.Bounds);
            rows.Add(new DuckingDemoRow(result.Tick, result.Position.X, result.Position.Y,
                result.CollisionBounds.Height, result.CollisionBounds.Bottom, result.Ducking,
                result.Speed.X, result.UnduckBlockingSolidId,
                string.Join(',', result.Events.Select(e => e.Kind.ToString()))));
        }
        return new DuckingDemoResult("CDR-075", rows.AsReadOnly(), player.DuckHeight, player.StandingHeight,
            rows.All(row => row.Bottom == 11), neverOverlapped, !rows[^1].Ducking && rows[^1].Height == 11,
            rows.Count(row => row.Events.Contains(nameof(PlayerNormalEventKind.DuckStarted), StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(nameof(PlayerNormalEventKind.UnduckBlocked), StringComparison.Ordinal)),
            rows.Count(row => row.Events.Contains(nameof(PlayerNormalEventKind.UnduckCompleted), StringComparison.Ordinal)),
            false);
    }
}

internal sealed record DuckingDemoResult(string TaskId, IReadOnlyList<DuckingDemoRow> Rows,
    int DuckHeight, int StandingHeight, bool FeetPreserved, bool NeverOverlapped, bool RestoredStanding,
    int StartedCount, int BlockedCount, int CompletedCount, bool DeterministicReplay);

internal sealed record DuckingDemoRow(long Tick, int X, int Y, int Height, int Bottom,
    bool Ducking, decimal SpeedX, string? BlockingSolidId, string Events);
