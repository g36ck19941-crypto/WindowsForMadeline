# CDR-013 — Bounded Atlas Page Decoder

Date: 2026-09-21

## Added function

- Added immutable BGRA32 frame ownership and SHA-256 fingerprints.
- Added a stream-only bounded `.data` run decoder.
- Added strict alpha-flag, run-length, exact-fill and trailing-data validation.
- Added 31 generated-data, fragmented-stream, immutability and API-surface tests.

## Role in the project

This supplies the in-memory pixels needed before later code can extract individual sprite frames. It does not parse animations, extract sprites, render or establish real-install compatibility.

## Evidence

- Command: `.\tools\Verify-CDR013.ps1`
- Release build: 0 warnings, 0 errors.
- CDR-010 regression: 15 passed, 0 failed.
- CDR-011 regression: 27 passed, 0 failed.
- CDR-012 regression: 35 passed, 0 failed.
- CDR-013: 31 passed, 0 failed.
- Real installation access: 0.
- Game/Everest/GUI launch: 0.
- Commercial asset files: 0.

## Next function after acceptance

CDR-014 will implement a bounded `Sprites.xml` reader with generated XML fixtures only.
