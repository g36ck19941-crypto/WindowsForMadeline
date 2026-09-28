# CDR-021 Madeline Normal/Jump

## Contract

`CelesteDesktop.Player` consumes immutable, caller-supplied input snapshots inside the fixed 60 Hz `SimulationWorld` step. It does not poll a keyboard, controller, clock, window or operating system.

The controller implements ground run and friction, reduced air control, same-direction overspeed reduction, gravity and held-jump half gravity, normal and fast-fall caps, ground/coyote/buffered jumps, horizontal jump boost, variable jump duration, collision speed cancellation and explicit transition events. Every update produces an immutable snapshot.

The numeric tuning and update ordering are independently implemented from facts visible in the official public Celeste `Player.cs`. The public source does not establish every input-buffer setting or every surrounding subsystem used by a shipped build, so this milestone is deliberately classified `partial`, not exact parity.

Reference evidence: <https://github.com/NoelFB/Celeste/blob/master/Source/Player/Player.cs>. The repository records facts from this public source, not copied implementation text.

## Evidence

- 30 generated-input tests cover constants, input bounds, run/friction, air control, overspeed, gravity, fall caps, fast fall, jump boost, the exact coyote/buffer boundaries, variable jump, floor/wall/ceiling collision, events, orchestration, immutable snapshots and deterministic replay.
- The cumulative demo runs 24 generated player ticks twice. It reaches maximum run speed, emits one `Jumped` event, reaches `Y=-16`, and requires identical rows on replay.
- All nine offline suites pass 249 cases. Release builds with zero warnings and zero errors.
- No game, GUI, real desktop, real installation, live input or commercial asset is used.

## Developer verification

Double-click `演示当前进度.cmd`. In the CDR-021 player table, confirm the run accelerates to 90, tick 7 contains `Jumped`, the arc reaches `Y=-16`, and replay is marked identical. Then double-click `验证当前版本.cmd`; the final suite must report `30/30` and the script must validate the CDR-021 manifest.

## Role in the project

This task gives the simulation kernel its first controllable character behavior. Later dash, wall, climb and interactive-entity modules can reuse the same player speed, position, timer and event contracts instead of inventing separate movement loops.

It does not yet implement dash, wall slide, wall jump, climb, stamina, holding, corner correction, jump-through platforms, water, cold/space variants, live controls, rendering or desktop interaction. It also does not prove that the complete shipped game feels identical.

## Next gate

CDR-022 may add Dash/Wall/Climb through generated snapshots and geometry. CDR-021 must remain local until developer acceptance.
