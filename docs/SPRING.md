# CDR-042 Spring

## Purpose

CDR-042 adds an isolated deterministic Spring entity over `Simulation.Core`. It consumes immutable fixed-tick contact snapshots and emits immutable lifecycle state, target-addressed launch effects and stable semantic events. It has no file, platform, rendering, desktop or live-input dependency.

In plain terms, a Spring starts ready. An eligible Player, Theo or Glider contact activates it, produces a directional velocity effect for that exact target, retracts for a bounded number of ticks, cools down and becomes ready again. A target that remains touching cannot repeatedly reactivate it; contact must first be released. Disabled and re-enabled transitions are explicit.

## Deterministic contract

- Orientations: `Up`, `Right`, `Down`, `Left`; each replaces only the launch axis and preserves perpendicular velocity.
- States: `Ready`, `Retracted`, `Cooldown`, `Disabled`.
- Contact: target ID, target kind, position, incoming velocity and explicit eligibility.
- Launch effect: target ID, target kind and a generic external-velocity effect from `Simulation.Core`.
- Lifecycle: fixed 60 Hz activation, retraction, cooldown, release-to-rearm and reset.
- Isolation: each Spring owns its timers and armed state; Player, Theo, Glider and other Springs keep independent state.

The original commercial Spring contact rules, timing and numeric launch values are not established by public facts. Tuning is therefore an independently designed deterministic baseline and behavior fidelity remains `partial`; no original numeric parity is claimed.

## Stable events

`SPRING_ACTIVATED`, `SPRING_LAUNCH_ISSUED`, `SPRING_RETRACTED`, `SPRING_COOLDOWN_STARTED`, `SPRING_READY`, `SPRING_DISABLED`, `SPRING_ENABLED`, `SPRING_CONTACT_IGNORED`.

Player, Theo and Glider separately emit their own external-velocity-applied event. A Spring launch event proves that a targeted effect was produced; target application is a separate fact.

## Verification evidence

- Release build: 0 warnings, 0 errors.
- Spring-focused tests: 42/42.
- Full offline regression: 489/489.
- Generated demonstration: 30 ticks, 3 activations, 3 launches, 3 resets and one actual application each by Player, Theo and Glider; replay is identical.
- Commercial bytes and installation accesses: 0.
- Visible GUI and live input: 0.

## Acceptance

1. Double-click `演示当前进度.cmd` and inspect `artifacts/cdr-042-demo/index.html`.
2. Confirm the CDR-042 table shows three activation/retract/cooldown/reset cycles targeting Player, Theo and Glider, with each target speed changing and identical replay.
3. Double-click `验证当前版本.cmd`; expect 0 warnings/errors, Spring 42/42, total 489/489 and the final CDR-042 pass line.

This proves generated-contact Spring lifecycle, target routing, actual offline target application and failure isolation. It does not prove formal App orchestration, original numeric/contact parity, original animation pixels, visible desktop presentation or live input.
