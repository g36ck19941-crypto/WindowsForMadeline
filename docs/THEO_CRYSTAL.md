# CDR-040 Theo Crystal

## Purpose

CDR-040 adds the first isolated interaction-entity module. `CelesteDesktop.Entity.Theo` consumes immutable fixed-tick input and the existing `Simulation.Core` geometry contract. It cannot read files, poll input, render pixels, inspect the desktop or access a game installation.

In plain terms, this gives the project a testable object that Player can pick up, carry, drop and throw. Once free, it falls, slows horizontally, collides with generated solids, bounces, inherits moving-platform speed and reports a bounded squish without changing Player or another Theo instance.

## Deterministic contract

- States: `Free`, `Held`, `Squished`.
- Actions: `None`, `Pickup`, `Drop`, `Throw`.
- Holder snapshot: anonymous holder ID, exact hold position, facing and lift speed.
- Free motion: fixed 60 Hz gravity, terminal fall, horizontal friction and whole-pixel collision through `Simulation.Core`.
- Interaction: static and moving solids, holder lift-speed inheritance and per-entity squish isolation.
- Output: immutable snapshot plus stable semantic events.

The current tuning is an independently designed offline baseline. The official public Celeste repository does not publish the Theo Crystal entity implementation, so its numeric behavior is deliberately `partial`; these values are not claimed as original-game parity.

## Stable events

`THEO_PICKED_UP`, `THEO_CARRIED`, `THEO_HOLD_BLOCKED`, `THEO_DROPPED`, `THEO_THROWN`, `THEO_HORIZONTAL_BOUNCED`, `THEO_VERTICAL_BLOCKED`, `THEO_LANDED`, `THEO_BOUNCED`, `THEO_LIFT_CARRIED`, `THEO_LIFT_INHERITED`, `THEO_SQUISHED`, `THEO_EXTERNAL_VELOCITY_APPLIED`.

## Verification evidence

- Release build: 0 warnings, 0 errors.
- Theo-focused tests: 37/37.
- Full offline regression: 402/402.
- Generated demonstration: 36 ticks, 1 pickup, 1 throw, 1 horizontal bounce, 2 landings and identical replay.
- Commercial bytes persisted: 0.
- Game/install accesses: 0.
- Visible GUI and live input: 0.

The focused matrix covers Player-issued pickup/carry/throw requests, static and moving Solid behavior, lift-speed handoff, two independent Theo controllers, failure isolation and deterministic replay.

## Acceptance

1. Double-click `演示当前进度.cmd` and inspect `artifacts/cdr-040-demo/index.html`.
2. In the CDR-040 table, confirm the pickup, throw, horizontal-bounce and landing events, and confirm replay is identical.
3. Double-click `验证当前版本.cmd`; expect 0 build warnings/errors, Theo 37/37, total 402/402 and the final CDR-040 pass line.

This proves deterministic generated-input entity behavior. It does not prove original Theo numeric parity, original pixels, animation selection, visible desktop presentation or real-time Player input.
