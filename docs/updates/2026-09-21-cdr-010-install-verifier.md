# CDR-010 — Explicit Synthetic Install Verifier

Date: 2026-09-21

Status: accepted by the developer on 2026-09-21.

## Added function

- Added .NET 8 Contracts and Install projects.
- Added a caller-supplied-root verifier with canonical containment checks.
- Added root, parent and leaf reparse-point rejection.
- Added stable bounded `INSTALL_*` issues without absolute-path disclosure on failure.
- Added a metadata-only system filesystem adapter and injectable synthetic filesystem.
- Added an isolated offline verification command.

## Evidence

- Command: `.\tools\Verify-CDR010.ps1`
- Release build: 0 warnings, 0 errors.
- Synthetic tests: 15 passed, 0 failed.
- Real installation reads: 0.
- Directory enumeration or file-content reads: 0 API surface.
- Game/Everest/GUI launches: 0.
- Commercial asset files: 0.

## Limitations

This proves deterministic structural validation against synthetic metadata states only. It does not prove that any real installation is supported, that atlas files can be parsed or that original assets can be displayed.

## Manual acceptance

Run `.\tools\Verify-CDR010.ps1` and confirm the final line is `RESULT total=15 passed=15 failed=0`.

## Next function after acceptance

CDR-011 will add bounded IPC contracts and AssetWorker lifecycle supervision with synthetic crash, timeout, cancellation and malformed-envelope tests. It will not parse assets or read a real installation.
