# Player Wall-Speed Retention Calibration

## CDR-072 scope

CDR-072 calibrates one horizontal collision-feel rule. When Player hits a wall with nonzero horizontal speed, the controller stores that speed for four fixed 60 Hz ticks. If the blocking side clears during the window, the stored speed is restored. Opposite horizontal input, an ordinary jump or an external velocity launch cancels the window, and continuous blocking expires it explicitly. This prevents stale wall speed from overwriting a later Spring, Bumper or Puffer launch.

The four-tick value is the deterministic fixed-step representation of the observed `0.06s` behavior window. It is deliberately documented as a bounded project rule rather than a claim that every shipped-build timing edge is exact.

The immutable snapshot exposes the retained speed and remaining ticks. `WallSpeedRetained`, `WallSpeedRestored`, `WallSpeedRetentionCancelled` and `WallSpeedRetentionExpired` distinguish every outcome without reading live input or platform state.

## Limits

This task does not implement corner correction, jump-through platforms, ducking, holdable movement modifiers, low-friction/core/space variants, audio, animation or human-feel acceptance. Player fidelity remains `partial`.
