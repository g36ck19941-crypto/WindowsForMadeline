# CDR-012 — Bounded Atlas Metadata Reader

Date: 2026-09-21

## Added function

- Added immutable atlas, page and entry descriptor contracts.
- Added a stream-only `.meta` reader with no filesystem access.
- Added bounded input, string, page, entry and extent budgets.
- Added strict UTF-8, canonical length and logical-path validation.
- Added duplicate, rectangle, trim, truncation and trailing-data rejection.
- Added stable `ATLAS_META_*` failures and 35 generated-byte/API-surface tests.

## Role in the project

This supplies the atlas table of contents required by later pixel decoding and catalog construction. It does not decode pixels, resolve animations or render anything.

## Evidence

- Command: `.\tools\Verify-CDR012.ps1`
- Release build: 0 warnings, 0 errors.
- CDR-010 regression: 15 passed, 0 failed.
- CDR-011 regression: 27 passed, 0 failed.
- CDR-012: 35 passed, 0 failed.
- Real installation access: 0.
- Game/Everest/GUI launch: 0.
- Commercial asset files: 0.

## Next function after acceptance

CDR-013 will implement bounded `.data` RLE decoding with generated inputs only.
