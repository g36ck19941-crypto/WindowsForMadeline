# CDR-022 Dash, Wall and Climb

## Contract

`PlayerTraversalController` composes generated per-tick input snapshots with the CDR-021 Normal/Jump controller. It adds explicit `Normal`, `Dash`, `WallSlide` and `Climb` states without polling live input or depending on a clock, GUI, filesystem or operating system.

The controller implements eight-way digital direction quantization, dash charge/cooldown/refill/attack windows, nine-tick dash motion and end speed, collision events, wall checks and wall jumps, timed wall-slide fall caps, grab/release, climb movement, stamina costs and tired/depleted states, climb jumps, a generated ledge hop, and isolated infinite-dash/infinite-stamina assists. Each tick returns an immutable state snapshot.

The work also corrected a Simulation.Core midpoint defect found by the new state trace: a `+0.5` or `-0.5` remainder no longer oscillates position during later zero-displacement ticks. Dedicated positive and negative regression cases preserve this invariant.

Reference constants and broad order are independently implemented from facts visible in the official public Celeste `Player.cs`: <https://github.com/NoelFB/Celeste/blob/master/Source/Player/Player.cs>. No implementation text was copied. Full shipped-build surroundings are not established, so fidelity remains `partial`.

## Evidence

- 44 generated traversal cases cover constants, direction quantization, dash lifecycle and assists, wall range/slide/jump, climb entry/motion/stamina/release/jumps/ledge hop, immutable snapshots, orchestration and replay.
- Simulation.Core now passes 33 cases, including three offset-query cases and two midpoint-stability regressions.
- The cumulative demo runs a 24-tick generated traversal twice: one wall slide, wall jump, blocked dash and climb entry occur, stamina falls below 110, and all rows replay identically.
- All ten offline suites pass 298 cases. Release builds with zero warnings and zero errors.
- No game, GUI, real desktop, real installation, live input or commercial asset is used.

## Developer verification

Double-click `演示当前进度.cmd`. Inspect the CDR-022 table and confirm `WallSlideStarted`, `WallJumped`, `DashStarted`, `DashBlocked` and `ClimbStarted` appear; climbing speed reaches `-45`, stamina decreases, stationary dash/climb rows do not jitter, and replay is identical. Then double-click `验证当前版本.cmd`; the final traversal suite must report `44/44`, followed by a validated CDR-022 manifest.

## Role in the project

This task completes the first offline player movement stack: the character logic can now run, jump, dash, interact with walls, climb and spend stamina in one deterministic state machine. Later entities can react to explicit player state and events rather than guessing from raw coordinates.

It does not implement upward dash corner correction, jump-through platforms, moving-wall lift boosts, climb blockers, all assist variants, duck/super techniques, holdables, water or environment variants. It also does not connect live controls, sprites, rendering or the desktop, and does not prove complete shipped-game feel.

## Next gate

CDR-022 is the last task in the developer's current automatic offline authorization. It must remain local until developer acceptance. Starting CDR-030 would introduce a synthetic Windows presentation/GUI boundary and therefore requires fresh authorization.
