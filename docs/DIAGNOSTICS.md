# Diagnostics Contract

## 1. Record shape

Production diagnostics use one JSON object per line. Required fields:

```json
{
  "timestampUtc": "2026-09-21T00:00:00.0000000Z",
  "runId": "uuid",
  "eventId": "ASSET_FRAME_DECODED",
  "subsystem": "AssetWorker",
  "severity": "Info",
  "stage": "decode-page",
  "outcome": "succeeded",
  "durationMs": 1.25
}
```

Optional bounded fields include `requestId`, `tick`, `entityId`, `assetId`, `sequence`, dimensions, stride, counts, fingerprint, process exit code and recovery action.

Exceptions add `exceptionType`, `message`, `hresult`, `stack`, recursively bounded `inner`, last completed stage and `recoverable`. Logs never include raw pixel payloads, typed text, window titles or credentials.

## 2. Stable health chain

These events describe separate facts and must appear only when directly observed:

1. `INSTALL_SELECTED`
2. `INSTALL_VALIDATED`
3. `ASSET_WORKER_READY`
4. `ATLAS_INDEX_PARSED`
5. `ATLAS_PAGE_DECODED`
6. `ASSET_FRAME_DECODED`
7. `ASSET_VISIBLE_PIXELS_CONFIRMED`
8. `ASSET_CATALOG_READY`
9. `ENTITY_SPAWNED`
10. `SIMULATION_TICK_ADVANCED`
11. `ANIMATION_FRAME_RESOLVED`
12. `RENDER_TEXTURE_UPLOADED`
13. `RENDER_SUBMITTED`
14. `PRESENTER_PRESENTED`
15. `PRESENTED_PIXELS_CHANGED`
16. `HUMAN_VISIBILITY_CONFIRMED` — written only by an explicit acceptance workflow, never inferred automatically.

Missing downstream events must be reported as the exact remaining boundary. `PRESENTED_PIXELS_CHANGED` proves compositor input changed, not that a human saw it.

## 3. Error families

- `INSTALL_*`: selection, containment, missing/unsupported files.
- `IPC_*`: version, length, timeout, cancellation, worker exit.
- `ATLAS_META_*`: header, count, path, page, trim, trailing data.
- `ATLAS_DATA_*`: dimensions, run, alpha, truncation, decompression budget.
- `SPRITE_XML_*`: XML budget, schema, duplicate, missing frame, invalid metadata.
- `CATALOG_*`: allowlist, ambiguity, missing dependency, fingerprint mismatch.
- `SIM_*`: invalid state, non-finite data, tick/order invariant.
- `ENTITY_*`: entity-specific invariant and unsupported interaction.
- `DESKTOP_*`: capture, topology, DPI, privacy filter.
- `RENDER_*`: device, upload, stride, alpha, submit, present, device loss.
- `APP_*`: startup, pause, shutdown and recovery.

Every error code has one owning subsystem. App may surface it but must not reinterpret it.

## 4. Storage and support bundle

- Default log is bounded by size and generation count under the user-local app data directory.
- A support bundle is explicit user action and contains logs, version manifests and hashes only.
- Commercial pixels, source atlas files, raw window lists and user input are excluded.
- All logs include application build, contract version and OS/render-backend summaries required to reproduce a defect.
