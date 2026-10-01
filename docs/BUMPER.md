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

## Acceptance procedure

1. Double-click `演示当前进度.cmd`.
2. Inspect the CDR-045 Bumper table in `artifacts/cdr-045-demo/index.html`.
3. Confirm 15 fixed ticks, 4 activations, 4 launch requests, 3 ready transitions, 1 center fallback, 1 ignored contact and 4 actual Player applications with identical replay.
4. Confirm the second Bumper remains `Ready` throughout the generated trace.
5. Double-click `验证当前版本.cmd`.
6. Confirm Release has 0 warnings/errors, Bumper reports `46/46`, total regressions are `625/625`, and the final line is `CDR-045 OFFLINE VERIFICATION PASSED`.

This is an offline acceptance candidate only. It does not prove original numeric/path parity, real assets or audio, visible desktop presentation, live input or formal App orchestration.

## Next gate

CDR-044 remains awaiting developer acceptance and unuploaded. CDR-046 Puffer is not authorized by CDR-045 implementation or acceptance.

Current state: CDR-045 local implementation and verification are complete, awaiting developer acceptance and not uploaded.
