# CDR-011 — Bounded AssetWorker Protocol and Supervision

Date: 2026-09-21

## Added function

- Added an actual independent, hidden Worker process with lifecycle-only messages.
- Added a versioned 4 KiB length-prefixed JSON codec.
- Added restricted process launch that cannot target Celeste or arbitrary executable names.
- Added serialized supervision with separate startup, request and shutdown timeouts.
- Added cancellation, crash, malformed response, forced cleanup and fresh-session recovery.
- Added structured operation results and bounded JSON fallback failures.

## Evidence

- Command: `.\tools\Verify-CDR011.ps1`
- Release build: 0 warnings, 0 errors.
- CDR-010 regression: 15 passed, 0 failed.
- CDR-011: 27 passed, 0 failed.
- Real project Worker lifecycle: Start, Ping and Stop passed.
- Asset request or parser messages: 0.
- Real installation access: 0.
- Game/Everest/GUI launch: 0.
- Commercial asset files: 0.

## Limitation

This proves bounded lifecycle IPC only. No original asset has been located, parsed, decoded or displayed.

## Manual acceptance

Run `.\tools\Verify-CDR011.ps1` and confirm both final result lines report all tests passed.

## Next function after acceptance

CDR-012 will implement the bounded `.meta` reader with synthetic positive, truncation, count, path, page, trim, overflow and trailing-data fixtures. It will not read a real installation.
