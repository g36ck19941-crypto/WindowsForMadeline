# Tasks

任务顺序是强制依赖，不得跳过。Primary agent 为默认 owner；任何范围变化必须先更新本文件。

## P0 — Foundation

### CDR-001 — Architecture and diagnostics foundation

- Owner: Primary
- Scope: repository records and contracts only
- State: accepted by developer on 2026-09-21
- Acceptance:
  - module and process boundaries documented;
  - structured diagnostics and health chain documented;
  - asset safety and reference policy documented;
  - parity states and task order documented;
  - no product code, game read, GUI or Legacy mutation.
- Canonical review: `docs/reviews/CDR-001-ACCEPTANCE.md`
- Developer-facing Chinese mirror: `docs/zh-CN/reviews/CDR-001-ACCEPTANCE.md`
- Acceptance boundary: authorizes CDR-010 synthetic install verification only.

### CDR-002 — Remote version-management contract

- Owner: Primary
- Scope: remote configuration, documentation-only write test and repository records
- State: complete after developer confirmation
- Evidence:
  - remote `main` remained at `d237277`;
  - test branch `codex/connection-test-20260921` was created at `df00e2c`;
  - the test commit contains one documentation file and no commercial asset or product claim.
  - after confirmation and a clean outgoing audit, foundation branch `codex/cdr-001-foundation` was published at `5a1dd1f`.
- Gate: CDR-002 was satisfied independently; CDR-001 was subsequently accepted under its own review.

## P1 — Safe asset foundation

### CDR-010 — Explicit install verifier

- Owner: Primary
- Scope: `src/CelesteDesktop.Install/**`, its tests, contracts and records
- State: accepted by developer on 2026-09-21
- Deliverable: validate only a caller-supplied directory through an injectable filesystem; no automatic machine scan in this stage
- Evidence: Release build 0 warnings/errors; 15/15 complete, missing, root/type, reparse, containment, inaccessible and API-surface tests passed
- Manual acceptance: `docs/zh-CN/INSTALL_VALIDATION.md`, then run `.\tools\Verify-CDR010.ps1`
- Gate: satisfied for CDR-010 only; CDR-011 is authorized, with no real install access

### CDR-011 — AssetWorker protocol and supervision

- Owner: Primary
- Scope: bounded IPC contracts, worker lifecycle, timeout, cancellation and crash recovery
- State: accepted by developer on 2026-09-21
- Evidence: Release build 0 warnings/errors; CDR-010 regression 15/15; CDR-011 27/27 including real hidden Worker Start/Ping/Stop
- Manual acceptance: `docs/zh-CN/ASSET_WORKER.md`, then run `.\tools\Verify-CDR011.ps1`
- Gate: satisfied for CDR-011; CDR-012 synthetic parser authorized, with no real install access

### CDR-012 — Atlas metadata reader

- Owner: Primary
- Scope: independent `.meta` reader into immutable descriptors
- State: accepted by developer on 2026-09-21
- Evidence: Release build 0 warnings/errors; CDR-010 15/15; CDR-011 27/27; CDR-012 35/35 generated-byte cases
- Manual acceptance: `docs/zh-CN/ATLAS_METADATA.md`, then run `.\tools\Verify-CDR012.ps1`
- Gate: satisfied for CDR-012; CDR-013 synthetic decoder authorized, with no real install access

### CDR-013 — Atlas page decoder

- Owner: Primary
- Scope: bounded `.data` RLE decoder to immutable BGRA32 buffers, cumulative generated-data progress demo and developer launchers
- State: accepted by developer on 2026-09-21
- Evidence: Release build 0 warnings/errors; CDR-010 15/15; CDR-011 27/27; CDR-012 35/35; CDR-013 31/31 generated-byte and fragmented-stream cases
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the generated report, then double-click `验证当前版本.cmd`; details are in `docs/zh-CN/ATLAS_DATA.md`
- Gate: satisfied for CDR-013; CDR-014 synthetic XML parsing authorized, with no real install access

### CDR-014 — Sprite metadata reader

- Owner: Primary
- Scope: hardened `Sprites.xml` reader for explicit allowlisted player/entity definitions, generated fixtures, cumulative demo and developer records
- State: accepted by developer on 2026-09-28
- Evidence: Release 0 warnings/errors; prior 15/27/35/31 regression cases and CDR-014 40/40 generated XML/API-surface cases; cumulative demo parses two generated sprite definitions
- Manual acceptance: double-click `演示当前进度.cmd`, inspect generated report, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/SPRITE_XML.md`
- Gate: satisfied for CDR-014; CDR-015 synthetic normalized catalog authorized, with no real install access

### CDR-015 — Normalized asset catalog

- Owner: Primary
- Scope: allowlist, page-on-demand decode, source fingerprints, immutable asset catalog
- State: accepted by developer on 2026-09-28
- Evidence: Release 0 warnings/errors; prior 15/27/35/31/40 regression cases and CDR-015 26/26 generated catalog cases; cumulative demo builds two isolated entity catalogs and decodes one required page
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the generated report, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/ASSET_CATALOG.md`
- Gate: satisfied for CDR-015; developer authorized CDR-016 read-only conformance against the selected正版 installation, with no game/GUI launch, install writes or commercial-byte persistence

### CDR-016 — Opt-in real-install conformance

- Owner: Primary
- State: accepted by developer on 2026-09-29
- Scope: read-only selected installation; summary hashes and counts only
- Evidence: Release 0 warnings/errors; 191 offline cases; selected installation produced 3 source summaries, 6,824 Atlas entries, 5 definitions, 93 animations and 706 frames; repeated decoder/catalog fingerprints stable; zero install writes and zero commercial bytes persisted
- Manual acceptance: double-click `验证当前版本.cmd`, then optionally drag the selected installation onto `验证真实安装兼容性.cmd`; details in `docs/zh-CN/REAL_INSTALL_CONFORMANCE.md`
- Gate: accepted for publication to an independent branch; CDR-020 was completed under the prior offline authorization

## P2 — Deterministic simulation

### CDR-020 — Actor/Solid kernel

- Owner: Primary
- State: accepted by developer on 2026-09-29
- Scope: `Simulation.Core`, generated geometry tests, cumulative offline demonstration and project records
- Fixed 60 Hz, whole-pixel actor position, subpixel remainder, collision ordering, moving-solid carry and LiftSpeed contracts.
- Evidence: Release 0 warnings/errors; 28/28 generated simulation cases and 219 total offline cases; 13-tick cumulative demo replays identically with carry and blocked events
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the tick table, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/SIMULATION_CORE.md`
- Gate: accepted for publication to an independent branch; CDR-021 was completed under the prior offline authorization

### CDR-021 — Madeline Normal/Jump

- Owner: Primary
- State: accepted by developer on 2026-09-29
- Scope: `CelesteDesktop.Player`, minimal simulation orchestration surface, generated input tests, cumulative offline demonstration and records
- Run, friction, air control, overspeed reduction, gravity, fast fall, coyote, buffer, variable jump, collision/transition events and immutable snapshots.
- Evidence: Release 0 warnings/errors; 30/30 player cases and 249 total offline cases; 24-tick cumulative demo reaches max run, emits one jump and replays identically
- Fidelity: `partial`; public reference values/order are covered, but exact input buffer configuration and full shipped-build surroundings are not established
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the player tick table, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/PLAYER_NORMAL_JUMP.md`
- Gate: accepted for publication to an independent branch; CDR-022 was completed under the prior offline authorization

### CDR-022 — Dash/Wall/Climb

- Owner: Primary
- State: accepted by developer on 2026-09-29
- Scope: Player traversal state layer, minimal Simulation.Core spatial/midpoint correction, generated tests, cumulative offline demonstration and records
- Direction quantization, dash lifecycle/assists, wall slide/jump, climb movement/stamina and ledge hop are implemented as explicit deterministic states/events.
- Evidence: Release 0 warnings/errors; 44/44 traversal cases, 33/33 Simulation.Core cases and 298 total offline cases; 24-tick traversal demo contains wall slide, wall jump, blocked dash and climb entry with identical replay
- Fidelity: `partial`; upward-dash corner correction, jump-throughs, moving-wall boosts, blockers, all assists and advanced techniques remain outside this task
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the traversal table, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/PLAYER_DASH_WALL_CLIMB.md`
- Gate: accepted for publication to an independent branch; developer authorized continuing to CDR-030, limited to generated graphics and hidden automated tests. Visible GUI and real desktop observation remain separately gated.

## P3 — Rendering and desktop adapter

### CDR-030 — Synthetic DirectComposition presenter

- Owner: Primary
- State: accepted by developer on 2026-09-29
- Scope: isolated rendering controller, Windows DirectComposition backend, generated checkerboard, hidden native tests, cumulative demo and project records
- Program-generated premultiplied BGRA32 checkerboard, bounded DPI/negative-origin geometry, D3D11 upload, DirectComposition surface/commit/completion, ordered health events and one-retry device-loss recovery.
- Evidence: Release 0 warnings/errors; 20/20 rendering cases and 318 total regression cases; real hidden HWND path presented two generated frames through `Commit` and `WaitForCommitCompletion`; cumulative demo records 3 presents, 2 pixel changes, no human-visibility claim and 0 commercial bytes.
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the CDR-030 checkerboard/health chain and corrected CDR-016/017–019 explanation, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/SYNTHETIC_PRESENTATION.md`
- Gate: accepted for publication to an independent branch. CDR-031 is authorized only for anonymous geometry, DPI, visible-surface state and velocity; titles, content, screenshots, input and visible GUI remain forbidden.

### CDR-031 — Desktop geometry adapter

- Owner: Primary
- State: implementation complete on 2026-09-29; developer acceptance pending
- Scope: anonymous desktop geometry, DPI, visible-surface state and velocity through isolated platform-neutral and Windows modules
- Forbidden: window titles, text/content, screenshots, input, identity-bearing process metadata, raw native handles in contracts/evidence, visible GUI and game/install access
- Evidence: Release 0 warnings/errors; 19/19 generated desktop cases and 337 total regressions; one authorized hidden Windows proof passed with aggregate count/DPI evidence only; cumulative demo uses generated geometry and records no real desktop values.
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the generated anonymous geometry table, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/DESKTOP_GEOMETRY.md`.
- Gate: keep local until developer acceptance. CDR-032 and any visible GUI require separate authorization; no human-visibility claim.

### CDR-032 — Asset-to-animation presentation

- Connect validated catalog to animations; no physics-to-render coupling.

## P4 — Interaction entities

Each entity owns a separate module, tests and parity row:

1. CDR-040 Theo Crystal
2. CDR-041 Glider
3. CDR-042 Spring
4. CDR-043 Refill
5. CDR-044 Water
6. CDR-045 Bumper
7. CDR-046 Puffer
8. CDR-047 Seeker or later explicitly approved entities

No entity is accepted until its Player, Solid and supported entity-to-entity interaction matrix passes.

## P5 — Deferred content extensions

These tasks reserve future capability and do not expand current authorization.

### CDR-060 — Compiled provider contracts

- Scope: introduce data-only `IAssetSourceProvider` and immutable `IWorldContentProvider` contracts after core asset and simulation contracts stabilize.
- Gate: synthetic providers only; no plugin loader, Mod directory or real map read.

### CDR-061 — Level/map content provider

- Scope: normalize selected room geometry, spawn points and supported entity placements behind `IWorldContentProvider`.
- Gate: synthetic map fixtures first; real formats and installations require a new task and explicit authorization.

### CDR-062 — Read-only Mod asset provider

- Scope: resolve explicitly enabled, data-only Mod assets behind `IAssetSourceProvider`.
- Gate: separate security review, deterministic precedence, bounded reads and fresh authorization; never execute DLLs or scripts.
