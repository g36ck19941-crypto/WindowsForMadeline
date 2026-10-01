# CDR-044 Deterministic Water Volume

## Purpose

CDR-044 adds an isolated fixed-tick Water volume that consumes generated target rectangles, velocities and swim axes. It records explicit enter, submerged, motion-issued and exit facts and emits target-addressed velocity effects for application by an owning target module.

## Boundaries

- Water owns volume overlap, occupant tracking, drag, neutral buoyancy, swim-axis targets and bounded velocity calculation.
- Water does not reference Player, Rendering, Desktop, files, GUI, clocks or live input.
- Player applies the resulting generic velocity effect through its existing entry point; issuance and application remain separate facts.
- All constants are an independently designed deterministic baseline. Original contact thresholds, numeric tuning, surface behavior, visuals and shipped-build parity remain `partial` or unestablished.
- No visible GUI, real desktop observation, installation access/write or commercial-byte persistence is authorized.

## Acceptance target

The generated cumulative demo must show deterministic enter, sustained submerged motion, upward neutral buoyancy, directional swimming, bounded speeds, exit, multiple-occupant ordering and actual Player velocity application. Focused tests and the complete offline regression suite must pass before developer review.

## Acceptance procedure

1. Double-click `演示当前进度.cmd`.
2. Inspect the CDR-044 Water table in `artifacts/cdr-044-demo/index.html`.
3. Confirm 12 fixed ticks, 3 entered facts, 13 submerged facts, 13 motion requests, 2 exits and 10 actual Player applications with identical replay.
4. Double-click `验证当前版本.cmd`.
5. Confirm Release has 0 warnings/errors, Water reports `48/48`, total regressions are `579/579`, and the final line is `CDR-044 OFFLINE VERIFICATION PASSED`.

This is an offline acceptance candidate only. It does not prove original numeric parity, real assets, visible desktop presentation, live input or formal App orchestration.

## Next gate

CDR-045 Bumper was separately authorized on 2026-10-01 and is now a distinct local acceptance candidate. That later authorization does not accept or publish CDR-044.
