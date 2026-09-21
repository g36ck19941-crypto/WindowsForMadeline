# CDR-013 — Bounded Atlas Page Decoder

Date: 2026-09-21

## Added function

- Added immutable BGRA32 frame ownership and SHA-256 fingerprints.
- Added a stream-only bounded `.data` run decoder.
- Added strict alpha-flag, run-length, exact-fill and trailing-data validation.
- Added 31 generated-data, fragmented-stream, immutability and API-surface tests.
- Added a cumulative generated Atlas demo that visually exposes the actual CDR-012 → CDR-013 result and limitations.
- Added double-click demo and verification launchers for developer acceptance.

## Role in the project

This supplies the in-memory pixels needed before later code can extract individual sprite frames. It does not parse animations, extract sprites, render or establish real-install compatibility.

## Evidence

- Command: `.\tools\Verify-CDR013.ps1`
- Release build: 0 warnings, 0 errors.
- CDR-010 regression: 15 passed, 0 failed.
- CDR-011 regression: 27 passed, 0 failed.
- CDR-012 regression: 35 passed, 0 failed.
- CDR-013: 31 passed, 0 failed.
- Cumulative demo: 1 page, 2 entries, 48 exact pixels, 0 commercial bytes.
- Real installation access: 0.
- Game/Everest/GUI launch: 0.
- Commercial asset files: 0.

## Developer acceptance

Double-click `演示当前进度.cmd`, inspect the generated HTML report, then double-click `验证当前版本.cmd`. The PowerShell verifier remains the internal agent/CI command.

## Next function after acceptance

CDR-014 will implement a bounded `Sprites.xml` reader with generated XML fixtures only.
