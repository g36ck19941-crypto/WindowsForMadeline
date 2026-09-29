# CDR-031 Anonymous Desktop Geometry

CDR-031 adds an isolated desktop adapter that reports only anonymous visible-surface geometry, DPI and calculated velocity. The platform-neutral tracker owns filtering, ephemeral IDs, monotonic timing and aggregate diagnostics; the Windows provider performs read-only top-level enumeration.

## Privacy boundary

The Windows provider calls only visibility, cloaking, rectangle and DPI APIs. It does not call title/text APIs, capture pixels, inspect process/executable/class identity, read input or expose native handles. A provider-local handle-to-random-token map exists only in memory; public snapshots contain IDs such as `surface-000001`.

`DESKTOP_SNAPSHOT_CAPTURED` contains only sequence, visible/filtered counts and minimum/maximum DPI. Raw window lists and source tokens are excluded from logs and evidence.

## Verification evidence

- Release build: 0 warnings, 0 errors.
- Generated desktop tests: 19/19; complete regression: 337/337.
- Authorized hidden read-only Windows proof: 1/1, with aggregate count/DPI output only.
- Cumulative demo: two generated snapshots, two final visible surfaces and one moving surface; no real desktop values are written into the report.
- Titles, content, screenshots, input, visible GUI, game/install access and commercial bytes: 0.

## Manual acceptance

1. Double-click `演示当前进度.cmd` and inspect the CDR-031 anonymous geometry table. It must show two program-generated snapshots, anonymous IDs, DPI and velocity.
2. Confirm the report says the desktop data is generated and contains no title, content, screenshot, input or native handle.
3. Double-click `验证当前版本.cmd`. It must end with `CDR-031 READ-ONLY VERIFICATION PASSED`, the desktop suite must report `19/19`, and the real proof must report `1/1` plus one aggregate line.

The verification launcher reads the current desktop only through the explicitly authorized anonymous boundary. It does not open a visible GUI.

## Role in the project

This task gives the future desktop character safe knowledge of where visible surfaces are and how they move, so collision/support logic can react without knowing which application owns a surface. It also prevents personal window data from leaking into contracts and logs.

It does not place Madeline on a real window, connect commercial animation frames, open an overlay, observe pixels or prove human visibility.

## Next gate

CDR-031 was accepted by the developer on 2026-09-29 and may be published to its independent branch after the outbound audit. CDR-032 is separately authorized for offline asset-to-animation presentation without physics feedback; visible GUI remains forbidden.
