# CDR-045 Deterministic Bumper Entity

## Purpose

CDR-045 adds an isolated fixed-tick Bumper that consumes generated target-center contacts. It checks a circular contact radius, emits a target-addressed radial velocity effect, and records activation, launch, cooldown, ready, disable, enable and ignored-contact facts.

## Boundaries

- Bumper owns generated circular contact, outward direction, deterministic coincident-center fallback, cooldown and release-to-rearm state.
- Bumper does not reference Player, Rendering, Desktop, files, GUI, clocks or live input.
- Player applies the resulting generic velocity effect through its existing entry point; issuance and application remain separate facts.
- All constants are an independently designed deterministic baseline. Original radius, launch speed, cooldown, moving-path, special variants, visuals and shipped-build parity remain `partial` or unestablished.
- No visible GUI, real desktop observation, installation access/write or commercial-byte persistence is authorized.

## Acceptance target

The generated cumulative demo must show deterministic contacts from multiple directions, radial launch, coincident-center fallback, cooldown, release-to-rearm, ignored out-of-range contact, two-Bumper isolation and actual Player velocity application. Focused tests and the complete offline regression suite must pass before developer review.

## Next gate

CDR-044 remains awaiting developer acceptance and unuploaded. CDR-046 Puffer is not authorized by CDR-045 implementation or acceptance.
