# Explicit Install Validation

Status: accepted by the developer on 2026-09-21. The acceptance authorizes CDR-011 bounded IPC and AssetWorker supervision only.

## Implemented function

CDR-010 validates only a directory explicitly supplied by its caller. The verifier has no discovery or enumeration API and does not open file content.

The `celeste-windows-foundation-v1` profile checks these exact structural markers:

- `Celeste.exe`
- `Content/Graphics/Atlases/Gameplay.meta`
- `Content/Graphics/Sprites.xml`

Atlas `.data` pages are intentionally not guessed here. CDR-012 will parse the metadata that identifies them, and later validation will use that evidence.

## Boundary

`InstallVerifier` receives an `IInstallFileSystem`. The interface exposes only:

- canonical path resolution;
- metadata status for one exact absolute path.

It exposes no directory enumeration, file-content read or automatic machine scan. The system adapter uses `File.GetAttributes` only.

Validation rejects:

- blank, relative, invalid, missing or non-directory roots;
- root, parent or required-file reparse points;
- absolute, traversing, malformed or duplicate profile paths;
- missing, inaccessible or wrong-type required entries;
- any normalized candidate outside the selected root.

An invalid result does not publish a canonical root and reports stable `INSTALL_*` code, stage and bounded relative path only. It does not report the user's absolute path in an issue.

## Security note

This validation is a structural preflight, not a permanent filesystem capability. Later AssetWorker requests must repeat containment and reparse checks when opening a source to address time-of-check/time-of-use changes.

## Verification

Run from the repository root:

```powershell
.\tools\Verify-CDR010.ps1
```

Expected result:

- Release build succeeds with 0 warnings and 0 errors;
- 15 synthetic tests print `PASS`;
- final line is `RESULT total=15 passed=15 failed=0`.

The command uses only generated in-memory filesystem entries. It does not inspect a real Celeste installation or launch a GUI.

## Next gate

Developer acceptance of CDR-010 authorizes CDR-011 AssetWorker protocol and supervision only. It does not authorize real installation access; that remains gated at CDR-016.
