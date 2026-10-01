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

## Next gate

CDR-045 Bumper is not authorized by CDR-044 implementation or acceptance.
