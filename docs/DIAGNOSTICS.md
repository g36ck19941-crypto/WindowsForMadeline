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

Rendering lifecycle events outside the linear success chain are `RENDER_BACKEND_READY`, `RENDER_DEVICE_LOST` and `RENDER_DEVICE_RECOVERED`. A recoverable loss records the failed stage, HRESULT, full bounded exception chain and recovery action before one backend recreation and retry. None of these events implies visibility.

Desktop capture emits `DESKTOP_SNAPSHOT_CAPTURED` with sequence, visible/filtered counts and bounded DPI range only. It never carries source tokens, native handles, titles, content, screenshots, input or process identity.

Animation presentation emits `ANIMATION_STATE_SELECTED`, `ANIMATION_STATE_TRANSITIONED`, `ANIMATION_FRAME_RESOLVED` and `ANIMATION_FRAME_COMPOSED`. Frame events may contain a SHA-256 fingerprint and dimensions but never pixel payloads. Resolution and composition do not imply render submission, presentation or human visibility.

Theo Crystal emits `THEO_PICKED_UP`, `THEO_CARRIED`, `THEO_HOLD_BLOCKED`, `THEO_DROPPED`, `THEO_THROWN`, `THEO_HORIZONTAL_BOUNCED`, `THEO_VERTICAL_BLOCKED`, `THEO_LANDED`, `THEO_BOUNCED`, `THEO_LIFT_CARRIED`, `THEO_LIFT_INHERITED`, `THEO_SQUISHED` and `THEO_EXTERNAL_VELOCITY_APPLIED`. Events contain bounded entity/holder/solid IDs, tick, integer position and velocity only. A Theo event proves a simulation transition, never an asset, render or visible-desktop result.

Glider emits `GLIDER_PICKED_UP`, `GLIDER_CARRIED`, `GLIDER_HOLD_BLOCKED`, `GLIDER_DROPPED`, `GLIDER_THROWN`, `GLIDER_HOLDER_FALL_LIMITED`, `GLIDER_OPENED`, `GLIDER_CLOSED`, `GLIDER_HORIZONTAL_BOUNCED`, `GLIDER_VERTICAL_BLOCKED`, `GLIDER_LANDED`, `GLIDER_BOUNCED`, `GLIDER_LIFT_CARRIED`, `GLIDER_LIFT_INHERITED`, `GLIDER_DESTROYED`, `GLIDER_SQUISHED` and `GLIDER_EXTERNAL_VELOCITY_APPLIED`. The fall-limit event proves an effect request was produced, not that Player applied it. Glider events never imply assets, rendering or human visibility.

Spring emits `SPRING_ACTIVATED`, `SPRING_LAUNCH_ISSUED`, `SPRING_RETRACTED`, `SPRING_COOLDOWN_STARTED`, `SPRING_READY`, `SPRING_DISABLED`, `SPRING_ENABLED` and `SPRING_CONTACT_IGNORED`. A launch event identifies the intended target but does not itself prove application; Player, Theo or Glider records the separate external-velocity-applied fact. No Spring event implies assets, rendering or human visibility.

Refill emits `REFILL_COLLECTED`, `REFILL_RESTORE_ISSUED`, `REFILL_COOLDOWN_STARTED`, `REFILL_RESPAWNED`, `REFILL_DISABLED`, `REFILL_ENABLED` and `REFILL_CONTACT_IGNORED`. A restore-issued event identifies the intended Player but does not itself prove application; Player records `ExternalResourcesApplied` separately. No Refill event implies assets, rendering or human visibility.

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

AssetWorker supervision returns a stable operation code, bounded protocol detail code, state, observed exit code and recovery action. The process fallback event is `ASSET_WORKER_PROCESS_FAILED`; it is written as one bounded JSON record to stderr and never mixed with binary stdout IPC.

## 4. Storage and support bundle

- Default log is bounded by size and generation count under the user-local app data directory.
- A support bundle is explicit user action and contains logs, version manifests and hashes only.
- Commercial pixels, source atlas files, raw window lists and user input are excluded.
- All logs include application build, contract version and OS/render-backend summaries required to reproduce a defect.
