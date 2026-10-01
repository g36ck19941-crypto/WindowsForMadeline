# CDR-046 Deterministic Puffer Entity

## Purpose

CDR-046 adds an isolated fixed-tick Puffer that swims between generated horizontal bounds, enters a warning fuse when an eligible target is near, explodes once, emits a target-addressed radial velocity effect and returns to its spawn after a deterministic spent cooldown.

## Boundaries

- Puffer owns generated swim bounds/direction, warning, locked target identity, explosion, coincident-center fallback, spent/respawn and disable/enable state.
- Puffer does not reference Player, Rendering, Desktop, files, GUI, clocks or live input.
- Player applies the resulting generic velocity effect through its existing entry point; issuance and application remain separate facts.
- A second Puffer advances independently; one controller's warning, explosion, disable or failure state cannot mutate another entity.
- All constants are an independently designed deterministic baseline. Original swim path, contact/explosion radii, warning/respawn timing, launch tuning, special interactions, visuals, audio and shipped-build parity remain `partial` or unestablished.
- No visible GUI, real desktop observation, installation access/write or commercial-byte persistence is authorized.

## Acceptance target

The generated cumulative demo must show bounded swim and turns, three warning/explosion/launch cycles, coincident-center fallback, spent cooldown and spawn reset, ignored out-of-range contact, two-Puffer isolation and actual Player velocity application. Focused tests and the complete offline regression suite must pass before developer review.

## Acceptance procedure

1. Double-click `演示当前进度.cmd`.
2. Inspect the CDR-046 Puffer table in `artifacts/cdr-046-demo/index.html`.
3. Confirm 20 fixed ticks, 8 swim events, 3 turns, 3 warnings, 3 explosions, 3 launch requests, 2 respawns, 1 center fallback, 1 ignored contact and 3 actual Player applications with identical replay.
4. Confirm the second Puffer remains independently `Swimming` throughout the generated trace.
5. Double-click `验证当前版本.cmd`.
6. Confirm Release has 0 warnings/errors, Puffer reports `59/59`, total regressions are `684/684`, and the final line is `CDR-046 OFFLINE VERIFICATION PASSED`.

This is an offline acceptance candidate only. It does not prove original numeric/path parity, real assets or audio, visible desktop presentation, live input or formal App orchestration.

## Next gate

CDR-047 Seeker remains deferred and unauthorized. CDR-046 must stay local until explicit developer acceptance.

Current state: CDR-046 local implementation and verification are complete, awaiting developer acceptance and not uploaded.
