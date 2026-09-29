# CDR-020 deterministic simulation core

## Contract

`CelesteDesktop.Simulation.Core` advances only through explicit fixed steps at 60 Hz. It has no clock, filesystem, asset, GUI, operating-system or live-input dependency. Positions and collision bounds are whole pixels; decimal remainders accumulate requested subpixel displacement and emit deterministic whole-pixel moves. Exact `+0.5` and `-0.5` midpoint remainders use stable ties-to-even rounding, so a later zero displacement cannot oscillate position.

Actors collide only with ordered Solids. Each requested whole-pixel displacement is checked one pixel at a time, and the first registered intersecting Solid is the blocker. Actors do not collide with other Actors in this kernel.

A moving Solid detects riders before motion, moves itself, then processes Actors in registration order. Intersecting Actors are pushed; prior riders are carried. The motion produces per-second `LiftSpeed`. A blocked push or carry emits an explicit squish event instead of silently passing through a blocker.

## Evidence

- 28 generated-geometry tests cover fixed ticks, positive/negative remainder accumulation, horizontal/vertical collision, blocker and rider order, exact carry/push, LiftSpeed, squish, snapshots, replay determinism and dependency surface.
- The cumulative demo runs 13 ticks twice and requires identical rows. It includes carry events and an Actor-blocked event against a generated wall.
- All inputs are program-generated. No Celeste installation, asset, GUI, desktop or live input is used.

## Developer verification

Double-click `演示当前进度.cmd`. Inspect the CDR-020 tick table and confirm the repeated replay is marked identical. Then double-click `验证当前版本.cmd`; the final suite is expected to report `28/28`, followed by a validated CDR-020 demo manifest.

## Role in the project

This task creates the small, predictable physics foundation that later Madeline and entity rules can share. It decides how objects move by fractions of a pixel, which wall blocks first, and how moving platforms carry or push an Actor.

It does not implement Madeline's run, jump, dash, climb or stamina values. It also does not prove original-game behavior parity, rendering, visibility or desktop interaction. Those require later task-specific evidence.

## Next gate

CDR-020 was accepted on 2026-09-29 and may be published to its independent branch. CDR-021 subsequently added Normal/Jump state and exact tick tests on top of this kernel.
