# CDR-070 Acceptance

State: ready for developer acceptance; not uploaded.

## Delivered

- Explicit-path, development-only local assembly reference builder.
- Pinned repository-local `ilspycmd` 11.1.0.9782 with explicit first-use restore consent.
- Hash-addressed output restricted to ignored `local-cache` with staging cleanup and reuse.
- Summary-only manifest that cannot carry paths, source bytes or raw commercial content.
- Plain-Chinese double-click launcher, bilingual contracts and synthetic isolation tests.

## Evidence

- Release solution build: 0 warnings, 0 errors.
- Focused CDR-070 safety tests: 10/10.
- Complete regression set: 866/866.
- Authorized local read produced 1,369 C# reference files from `Celeste.dll` under an ignored cache key derived from SHA-256.
- Local manifest records `gameLaunched=false`, `installationWrites=0`, `commercialReferenceTracked=false`, and `exactParityEstablished=false`.
- Git ignore and tracked-tree scans found no decompiled file, local cache content or selected-install path eligible for publication.

## Acceptance procedure

1. Double-click `建立本地行为参考.cmd`; selecting the same legitimate installation should reuse the existing hash-addressed cache and show `LOCAL_REFERENCE_COMPLETED`.
2. Confirm no game or GUI starts.
3. Double-click `验证当前版本.cmd`; expect 0 build warnings/errors, 10/10 focused and 866/866 total tests.
4. Run `git status --short`; no `local-cache` file should appear.

## What it does not prove

This does not automatically improve any Player/entity implementation, compile commercial code into the product, establish exact runtime behavior, or prove human-feel parity. Each later calibration remains a separate contract, implementation and acceptance task.
