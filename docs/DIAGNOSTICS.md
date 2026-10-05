# Diagnostics Contract

## Restricted startup diagnostics supplement

RESTRICTED_PHASE_PROTOCOL_VERIFIED:8 own file-protocol cases, no child starts. RESTRICTED_STARTUP_DIAGNOSTICS:phaseProtocolPassed,lastOwnPhase,coreClrImageObserved,loadedImageCount. Result loadedImageNames contains <=128 ASCII basenames from already-owned debug events, never paths/addresses/memory bytes. Four ordered phase files <=512B:own-entry-file-written,own-assembly-check-completed,gui-policy-query-returned,job-query-verified. No first marker does not prove Main never ran; first file operation may fail. Image observation does not prove API use/GUI creation. Summary startupDiagnosticsVersion1 validates phase/count; legacy summary remains readable.

## CDR-082 own restricted process prototype

- RESTRICTED_PROCESS_CASE:own run/scenario/PID/phase/exit/duration, pre-resume GUI denial/Job/resource query, resumed/ready/exit/cleanup and optional native exception metadata. Job accounting after close=null (not measured zero). No live input/window/title/memory bytes/dumps.
- RESTRICTED_PROCESS_FAILED:own phase/result, redacted full managed exception chain/HResult/native Win32 error; owned-child native exception code/parameter0/image names when observable. Failure code is not a root-cause claim.
- RESTRICTED_PROCESS_SUMMARY:nativeControlsPassed separately from managedStartup, PARTIAL exit2 for managed startup failure; fullSandboxEstablished=false/originalCompatibilityEstablished=false. Six native own cases do not count as managed or original runtime passes. Resources configured/queried, not stress tested; no GUI creation negative probe.

## CDR-082 readonly environment inventory

- ENVIRONMENT_SELF_CHECKS_OK:8 generated release-threshold checks; systemReads=0. Not isolation testing.
- ENVIRONMENT_INVENTORY_COMPLETED:fixed OS build/frameworkRelease32/file-slot count/isolationEstablished=false; REPORT_PATH points to ignored source-free JSON. Fixed3registryviews/8files existence+version only, no personal/window/input/game data. Actual enforced policies and original compatibility remain unknown; no repairs/fallback on missing evidence.

## CDR-082 own process guard

- PROCESS_GUARD_CASE: runId/scenario/ownedPid/outcome/exitCode/durationMs/lastStage/eventCount/terminationRequested/childExited/retained characters and overflow. Generated own phases only, not original actor health.
- PROCESS_GUARD_VERIFIED:8generated outcomes+phases/exits/budgets verified, allOwnedChildrenExited=true, originalCodeExecuted=false/processSandboxEstablished=false/originalRuntimeMonitorEstablished=false. Not native containment.
- PROCESS_GUARD_FAILED: timestamp/stage/type/message/HResult/stack/bounded inner, local paths redacted. No raw child text/source/pixels. Only own-created child may be terminated.

## CDR-082 cached XNA metadata preflight

- XNA_PRIVATE_CACHE_COMPLETED:3 approved fixed-hash files privately copied, source/destination identity stable; no target execution.
- XNA_STATIC_AUDIT_COMPLETED: cache metadata inspection completed, runtimeSafetyEstablished=false/originalTestExecuted=false. Mixed-mode/native/module-initializer declarations are risks requiring further review, not actual invocation evidence.
- XNA_PREFLIGHT_FAILED: JSON run/stage/type/message/HResult/stack/bounded inner with private path redaction. No commercial byte/source payload.
- XNA_PREFLIGHT_TESTS:13 own-tool check groups: prior9 plus4 generated native-prefix fixtures/rejections. Ordinary self-tests do not read system/cache files. Not actual original input/runtime acceptance.
- XNA_NATIVE_ENTRY_FORMS: cached PE32 prefix/import-slot aggregate, declared-import-thunk/body-unclassified and targetExecuted=false. Not general native disassembly/control-flow/safety proof. Raw entry symbols/bytes/disassembly remain ignored-only.
- XNA_BOUNDARY_CLASSIFICATION: pinvokeWithNativeRva/emptyModuleNames/memberScopes aggregate classification only; runtimeSafetyEstablished=false. Native RVA overlaps PInvoke count, not a new actual invocation. Raw method names/tokens/import details remain ignored cache only.
- SYSTEM_DEPENDENCY: fixed-slot name/status/machine/moduleInitializer/pinvokeMethods only. No raw bytes/source or recursive resolution.
- SYSTEM_DEPENDENCY_AUDIT_COMPLETED: readonly PE/CLR/import declarations collected, targetExecuted=false/runtimeSafetyEstablished=false. Found files are not execution-safety proof.
- XNA_INITIALIZER_GRAPH: conservative local method-token traversal aggregates; managedMethods/pinvokeBoundaries/memberBoundaries/indirectCalls, actualInvocationProven=false. No referenced-file resolution, native execution or complete initialization safety claim.

## CDR-082 managed isolation adapter

- ISOLATION_NET472_COMPILED: own adapter emitted using approved4 framework refs, static target/identity/import/resource audit only, originalBound=false/outputExecuted=false.
- ISOLATION_AUDIT_TESTS:3 own generated negative fixtures, not original/runtime evidence. Static audit failure shares ISOLATION_VERIFICATION_FAILED; no output loading.

- ISOLATION_CONTEXT_ADVANCED: adapter tick, fixed delta, injected directions/button edges, originalBound=false and recoveredCodeExecuted=false. Never SIMULATION_TICK_ADVANCED or ENTITY_SPAWNED: no original actor has run.
- ISOLATION_SERVICE_DENIED: named boundary service, allowed=false, outcome=denied. No silent successful no-op; requests cannot activate native/input/file services.
- ISOLATION_VERIFICATION_FAILED: stage, exception type/message/HResult/stack/inner, recoverable=false. Synthetic own-code failures only, no commercial resource payloads.
- ISOLATION_VERIFIED: 45 generated assertions, no original binding/runtime proof. Service policy is cooperative and not an OS/process security sandbox.

## CDR-081 identity inspection

- ASSEMBLY_INVENTORY_COMPLETED: candidate and graph-node counts, assemblyExecuted=false; completion means inspection finished, not playable runtime.
- ASSEMBLY_INVENTORY_FAILED: phase/code and path-redacted exception type/message/HResult/stack/inner chain; candidate failures also retained in local summary.
- PROGRESS_REPORT_FAILED: local summary phase/error without source/resource payload.
- Reference statuses: exact-metadata-match, identity-mismatch, missing-or-unreadable, framework-not-inspected. These do not establish runtime binding or native ABI readiness.

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

Player Normal/Jump emits stable simulation facts including `LiftVelocityApplied`, `WallSpeedRetained`, `WallSpeedRestored`, `WallSpeedRetentionCancelled`, `WallSpeedRetentionExpired`, `UpwardCornerCorrected`, `OneWayPlatformLanded`, `OneWayDropThroughStarted`, `OneWayDropThroughCompleted` and `OneWayDropThroughExpired`. A wall-speed event proves only that the fixed-tick Player controller stored, restored, cancelled or expired a bounded numeric velocity. `UpwardCornerCorrected` proves only that generated simulation geometry allowed one bounded integer horizontal correction around an upward blocker; it carries the blocker ID but no geometry payload. One-way events prove only platform-addressed generated contact or bounded drop state; they carry an ID and no map or geometry payload. None of these events implies live input, original-game execution, rendering or human visibility.

Theo Crystal emits `THEO_PICKED_UP`, `THEO_CARRIED`, `THEO_HOLD_BLOCKED`, `THEO_DROPPED`, `THEO_THROWN`, `THEO_HORIZONTAL_BOUNCED`, `THEO_VERTICAL_BLOCKED`, `THEO_LANDED`, `THEO_BOUNCED`, `THEO_LIFT_CARRIED`, `THEO_LIFT_INHERITED`, `THEO_SQUISHED` and `THEO_EXTERNAL_VELOCITY_APPLIED`. Events contain bounded entity/holder/solid IDs, tick, integer position and velocity only. A Theo event proves a simulation transition, never an asset, render or visible-desktop result.

Glider emits `GLIDER_PICKED_UP`, `GLIDER_CARRIED`, `GLIDER_HOLD_BLOCKED`, `GLIDER_DROPPED`, `GLIDER_THROWN`, `GLIDER_HOLDER_FALL_LIMITED`, `GLIDER_OPENED`, `GLIDER_CLOSED`, `GLIDER_HORIZONTAL_BOUNCED`, `GLIDER_VERTICAL_BLOCKED`, `GLIDER_LANDED`, `GLIDER_BOUNCED`, `GLIDER_LIFT_CARRIED`, `GLIDER_LIFT_INHERITED`, `GLIDER_DESTROYED`, `GLIDER_SQUISHED` and `GLIDER_EXTERNAL_VELOCITY_APPLIED`. The fall-limit event proves an effect request was produced, not that Player applied it. Glider events never imply assets, rendering or human visibility.

Spring emits `SPRING_ACTIVATED`, `SPRING_LAUNCH_ISSUED`, `SPRING_RETRACTED`, `SPRING_COOLDOWN_STARTED`, `SPRING_READY`, `SPRING_DISABLED`, `SPRING_ENABLED` and `SPRING_CONTACT_IGNORED`. A launch event identifies the intended target but does not itself prove application; Player, Theo or Glider records the separate external-velocity-applied fact. No Spring event implies assets, rendering or human visibility.

Refill emits `REFILL_COLLECTED`, `REFILL_RESTORE_ISSUED`, `REFILL_COOLDOWN_STARTED`, `REFILL_RESPAWNED`, `REFILL_DISABLED`, `REFILL_ENABLED` and `REFILL_CONTACT_IGNORED`. A restore-issued event identifies the intended Player but does not itself prove application; Player records `ExternalResourcesApplied` separately. No Refill event implies assets, rendering or human visibility.

Water emits `WATER_ENTERED`, `WATER_SUBMERGED`, `WATER_MOTION_ISSUED`, `WATER_EXITED`, `WATER_DISABLED`, `WATER_ENABLED` and `WATER_CONTACT_IGNORED`. A motion-issued event identifies the intended target but does not prove application; Player records `ExternalVelocityApplied` separately. Events carry only bounded entity/target IDs and generated volume bounds, never live input, assets or desktop content. No Water event implies rendering or human visibility.

Bumper emits `BUMPER_ACTIVATED`, `BUMPER_LAUNCH_ISSUED`, `BUMPER_CENTER_FALLBACK_USED`, `BUMPER_COOLDOWN_STARTED`, `BUMPER_READY`, `BUMPER_DISABLED`, `BUMPER_ENABLED` and `BUMPER_CONTACT_IGNORED`. A launch-issued event identifies the intended target and direction but does not prove application; Player records `ExternalVelocityApplied` separately. The center-fallback event makes the zero-distance direction decision observable instead of silently choosing one. No Bumper event implies assets, audio, rendering or human visibility.

Puffer emits `PUFFER_SWAM`, `PUFFER_TURNED`, `PUFFER_WARNING_STARTED`, `PUFFER_EXPLODED`, `PUFFER_LAUNCH_ISSUED`, `PUFFER_CENTER_FALLBACK_USED`, `PUFFER_SPENT_STARTED`, `PUFFER_RESPAWNED`, `PUFFER_DISABLED`, `PUFFER_ENABLED` and `PUFFER_CONTACT_IGNORED`. Warning and explosion events carry the locked target ID but no live input or content. A launch-issued event proves only that Puffer produced a target-addressed velocity; Player records `ExternalVelocityApplied` separately. Swim/turn/respawn facts describe simulation state only and never imply assets, audio, rendering or human visibility.

Seeker emits `SEEKER_PATROLLED`, `SEEKER_TURNED`, `SEEKER_ALERTED`, `SEEKER_CHASE_STARTED`, `SEEKER_CHASED`, `SEEKER_TARGET_LOST`, `SEEKER_WINDUP_STARTED`, `SEEKER_DASH_STARTED`, `SEEKER_DASHED`, `SEEKER_TARGET_HIT`, `SEEKER_WALL_HIT`, `SEEKER_STUNNED`, `SEEKER_RECOVERED`, `SEEKER_CENTER_FALLBACK_USED`, `SEEKER_DISABLED`, `SEEKER_ENABLED` and `SEEKER_TARGET_IGNORED`. Events carry only bounded generated entity/target IDs, center and tick. A target-hit event proves that Seeker produced a target-addressed hit fact; it does not prove Player death or application. No Seeker event implies original navigation parity, assets, audio, rendering or human visibility.

CDR-075 adds `DuckStarted`, `UnduckBlocked` and `UnduckCompleted`. Each records a fixed tick; blocked rise includes the ordinary Solid ID. Immutable Normal snapshots separately expose actual collision bounds and duck state. Each refused rise attempt is observable. These are generated simulation facts, not live input, animation or visibility evidence.

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

CDR-050 App events are `APP_STARTED`, `APP_TICK_STARTED`, `APP_SIMULATION_COMPLETED`, `APP_EFFECT_ROUTED`, `APP_EFFECT_CONFLICT`, `APP_EFFECT_TARGET_UNRESOLVED`, `APP_SEEKER_HIT_OBSERVED`, `APP_PRESENTATION_COMPLETED`, `APP_COMPONENT_DISABLED`, `APP_TICK_COMPLETED`, `APP_PAUSED`, `APP_RESUMED`, `APP_STOPPED`, `APP_FAULTED` and `APP_DISPOSED`. Each record includes sequence, tick, stage, outcome, component and optional target/detail. Failures include type, message, HResult, stack and inner exception. These events prove orchestration facts only; none implies original parity or human visibility.

CDR-051 host events are `APP_HOST_STARTED`, `APP_HOST_TICK_DISPATCHED`, `APP_HOST_BACKLOG_DROPPED`, `APP_HOST_CANCELLED`, `APP_HOST_STOPPED`, `APP_HOST_FAULTED` and `APP_HOST_DISPOSED`. Each record includes sequence, App tick, monotonic clock timestamp, executed tick count, dropped interval count and bounded detail. Host failures use the same full exception shape. A backlog-drop event proves scheduling protection acted; it does not imply a simulation tick was executed for each dropped interval.

AssetWorker supervision returns a stable operation code, bounded protocol detail code, state, observed exit code and recovery action. The process fallback event is `ASSET_WORKER_PROCESS_FAILED`; it is written as one bounded JSON record to stderr and never mixed with binary stdout IPC.

## 4. Storage and support bundle

- Default log is bounded by size and generation count under the user-local app data directory.
- A support bundle is explicit user action and contains logs, version manifests and hashes only.
- Commercial pixels, source atlas files, raw window lists and user input are excluded.
- All logs include application build, contract version and OS/render-backend summaries required to reproduce a defect.
