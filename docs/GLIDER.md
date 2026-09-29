# CDR-041 Glider

## Purpose

CDR-041 adds an isolated deterministic Glider entity over `Simulation.Core`. It accepts immutable fixed-tick holder/action snapshots and emits immutable state, interaction effects and stable semantic events. It has no file, platform, rendering, desktop or live-input dependency.

In plain terms, Player can request pickup, carry, drop and facing-aware throw. A held Glider requests a maximum holder fall speed; the generic Player external-effect entry applies that immutable request without either module depending on the other. Once free, it opens while descending, falls under a bounded slow-flight baseline, slows horizontally, collides with generated solids, bounces, lands and inherits moving-platform speed. Destroy or squish terminates only that Glider controller.

## Deterministic contract

- States: `Free`, `Held`, `Destroyed`, `Squished`.
- Actions: `None`, `Pickup`, `Drop`, `Throw`; destruction is a separate explicit request.
- Holder snapshot: anonymous holder ID, exact hold position, facing, lift speed and vertical speed.
- Holder effect: maximum fall-speed request plus whether the current holder speed exceeds it.
- Free motion: fixed 60 Hz gravity, slow terminal fall, horizontal friction, opening state and whole-pixel collision.
- Isolation: Player, Theo and other Gliders keep their own state if one Glider is destroyed or squished.

The official public Celeste repository does not publish the shipped Glider entity implementation. Tuning is therefore an independently designed deterministic baseline and behavior fidelity remains `partial`; no original numeric parity is claimed.

## Stable events

`GLIDER_PICKED_UP`, `GLIDER_CARRIED`, `GLIDER_HOLD_BLOCKED`, `GLIDER_DROPPED`, `GLIDER_THROWN`, `GLIDER_HOLDER_FALL_LIMITED`, `GLIDER_OPENED`, `GLIDER_CLOSED`, `GLIDER_HORIZONTAL_BOUNCED`, `GLIDER_VERTICAL_BLOCKED`, `GLIDER_LANDED`, `GLIDER_BOUNCED`, `GLIDER_LIFT_CARRIED`, `GLIDER_LIFT_INHERITED`, `GLIDER_DESTROYED`, `GLIDER_SQUISHED`, `GLIDER_EXTERNAL_VELOCITY_APPLIED`.

## Verification evidence

- Release build: 0 warnings, 0 errors.
- Player tests: 33/33; Glider-focused tests: 42/42.
- Full offline regression: 447/447.
- Generated demonstration: 48 ticks, 1 pickup, 1 holder fall-limit request, 1 Player application, 1 throw, 1 horizontal bounce, 1 landing and identical replay.
- Commercial bytes and installation accesses: 0.
- Visible GUI and live input: 0.

## Acceptance

1. Double-click `演示当前进度.cmd` and inspect `artifacts/cdr-041-demo/index.html`.
2. Confirm the CDR-041 table contains pickup, holder fall-limit, throw, open/close, horizontal-bounce and landing events, with identical replay.
3. Double-click `验证当前版本.cmd`; expect 0 warnings/errors, Player 33/33, Glider 42/42, total 447/447 and the final CDR-041 pass line.

This proves generated-input Glider behavior, actual Player consumption of the fall-limit effect and failure isolation. It does not prove formal App orchestration, original numeric parity, original animation pixels, visible desktop presentation or live input.
