# Offline App Orchestration

CDR-050 adds `CelesteDesktop.App` as the composition boundary for already verified offline modules. It owns lifecycle and ordering; it does not own gameplay rules.

## Tick contract

One accepted App tick advances `SimulationWorld` exactly once at fixed 60 Hz. Inside that step the App:

1. updates Spring, Refill, Water, Bumper, Puffer and Seeker in stable order;
2. routes target-addressed effects to Player, Theo or Glider;
3. updates Theo and Glider, then Player;
4. takes immutable snapshots;
5. presents the animation only after simulation completes.

Velocity conflicts are explicit and deterministic: the first value for each axis wins and `APP_EFFECT_CONFLICT` is recorded. Unknown targets are ignored with `APP_EFFECT_TARGET_UNRESOLVED`. A Seeker hit remains an observed fact; App does not invent Player death.

## Lifecycle and isolation

The state machine is `Created -> Running <-> Paused -> Stopped -> Disposed`, with `Faulted` reserved for required simulation/Player failure. Invalid transitions throw. Presentation and optional entity failures disable only the failing component, capture full exception details and allow the remaining simulation to continue. Required simulation failure faults the session and is surfaced as `AppRuntimeException`.

## Boundaries

The module has no filesystem, Windows desktop, installation, GUI or live-input dependency. CDR-050 uses program-generated inputs and existing verified contracts only. It does not prove real assets, original behavior parity, visible desktop output or human visibility.
