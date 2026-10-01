# Pure-offline Headless App Host

CDR-051 adds a process-level host around the CDR-050 App session. It supplies time and immutable input; gameplay still belongs to the simulation and entity modules.

## Scheduling contract

- The host uses an injected monotonic clock and schedules App ticks at fixed 60 Hz.
- App tick numbers and generated input ticks are consecutive.
- A normal interval advances exactly one App tick.
- If the clock jumps forward, one cycle executes at most the configured catch-up limit and explicitly drops older intervals.
- Dropped intervals never rewrite simulation state or become a variable-size simulation step.

## Lifecycle and failure contract

The host starts a new App session, contains cancellation, stops bounded runs, and disposes the session it owns. Clock or input failures are recorded with full exception detail, stop the session and surface as `AppHostException`. Stable events cover start, dispatch, backlog drop, cancellation, stop, fault and disposal.

## Boundaries

CDR-051 uses only program-generated clocks and input in its tests and demo. It has no GUI, live-input, desktop-observation, filesystem or installation dependency. It proves that the existing App can be driven safely over time, not that a visible desktop character, original assets or original-game parity exists.
