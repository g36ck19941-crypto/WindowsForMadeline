# Tasks

任务顺序是强制依赖，不得跳过。Primary agent 为默认 owner；任何范围变化必须先更新本文件。

## Additive evidence tooling

### CDR-080 — Local original-resource and behavior capability inventory

- Owner: Primary
- State: developer redirected priority on 2026-10-03; initial read-only tool/cache assessment completed
- File scope: local reference-builder inspection, ignored-cache file presence and summary manifest, existing asset-conformance evidence, project records
- Scope: prioritize evidence from original local resources and behavior; identify recovered types and missing runtime/dependency/asset integration before proposing replacement of self-designed behavior
- Evidence: current ignored cache actually contains 1369 C# files, including Player, TheoCrystal, Glider, Spring, Refill, Puffer and Seeker; tool decompiles one selected assembly and does not compile or run the resulting project
- Direction: stop extending independently invented gameplay baselines; existing behavior modules are retained as reversible history pending reference-backed replacement, not fidelity authorities
- Forbidden: deleting/replacing modules as part of this assessment, copying reference code into tracked product, publishing source/IL/assets, game/GUI/live input, install writes
- Next gate: capability report and a concrete original-reference-led replacement plan; CDR-076 proposal superseded by this direction, CDR-075 remains unaccepted and unuploaded

### CDR-070 — Development-only local behavior reference

- Owner: Primary
- State: accepted by the developer on 2026-10-02; independent-branch publication authorized after fresh verification and audit
- Scope: explicit-path local assembly read, pinned ILSpy, hash-addressed Git-ignored cache, summary-only manifest and safety tests
- Forbidden: game launch, installation writes, automatic discovery, Git/publish/package of decompiled source or commercial data, compiling reference source and exact-parity claims
- Purpose: strengthen later behavior calibration without making the product depend on the game or distributing protected material
- Gate: one real local generation, no tracked/leaked reference content, focused plus full regression verification, bilingual acceptance handoff; no upload before acceptance

### CDR-071 — Player moving-platform jump inheritance calibration

- Owner: Primary
- State: accepted and published at `codex/cdr-071-player-lift-inheritance` exact commit `9f5594a6e41b76323e2227eb0e4b0750f2733f9e`
- Scope: bounded horizontal/upward lift inheritance on ordinary jump, downward rejection, immutable applied-lift evidence, stable diagnostic and generated moving-Solid tests
- Fidelity: `partial`; this is one Normal/Jump rule, not complete Player parity
- Forbidden: reference source/IL/path persistence, line copying, game/GUI/live-input/install access, CDR-061 work and upload before acceptance
- Evidence: Release 0 warnings/errors; Player 38/38; total 871/871; generated demo applies `(250,-130)` lift once and replays deterministically

### CDR-072 — Player horizontal wall-speed retention calibration

- Owner: Primary
- State: accepted and published at `codex/cdr-072-player-wall-speed-retention` exact commit `cc77d1dd1c725ab37750cc0328cf1f2f8ead47f8`; publication succeeded on the third attempt after two GitHub TCP 443 timeouts, and remote `main` remained unchanged
- Scope: fixed 4-tick horizontal collision retention, restoration when the wall clears, reverse-input cancellation, expiration, immutable snapshot evidence and stable events
- Fidelity: `partial`; this is one collision-feel rule, not complete Player parity
- Evidence: Release 0 warnings/errors; Player 45/45; total 878/878; generated demo retains speed `90` for 4 ticks and restores `90` with one retain/restore event each; jump and external velocity cancel stale retention
- Forbidden: reference source/IL/path persistence, line copying, game/GUI/live-input/install access, CDR-061 work and upload before acceptance

### CDR-073 — Player upward corner correction calibration

- Owner: Primary
- State: accepted and published at `codex/cdr-073-player-upward-corner-correction` exact commit `c607e55d9ff28cabb718c2c1f68f0714641f8ad3`; remote `main` remained unchanged
- File scope: `CelesteDesktop.Player` Normal movement/tuning/snapshot/events, the minimal `Simulation.Core.Actor.MoveXExact` support, their generated tests, cumulative demo, verification entry and project records
- Scope: upward-only collision correction within 4 integer pixels, current horizontal-travel direction preference, exact horizontal correction that preserves subpixel remainder, remaining upward motion retry, immutable applied-offset evidence and stable diagnostic
- Fidelity: `partial`; this is one upward Normal-movement collision rule, not complete Player or dash corner parity
- Evidence: Release 0 warnings/errors; Simulation.Core 34/34; Player 50/50; total 884/884; generated demo moves from `(0,4)` to `(1,2)` with correction `+1`, one `UpwardCornerCorrected` event, preserved upward speed and identical replay
- Forbidden: downward/dash corner correction, reference source/IL/path persistence, line copying, game/GUI/live-input/install access, CDR-061 work and upload before acceptance

### CDR-074 — Player one-way-platform calibration

- Owner: Primary
- State: accepted by the developer on 2026-10-03 and published at `codex/cdr-074-player-one-way-platforms` exact commit `9e07d966e8398eb011536d87fa5af95e5f7e4ce3`; remote main unchanged
- File scope: one-way-platform geometry and filtered vertical collision in `CelesteDesktop.Simulation.Core`; Player Normal input/state/snapshot/events; their generated tests; cumulative demo, verifier and project records
- Scope: pass upward through a generated one-way platform, land and remain grounded from above, enter an explicit bounded drop-through state, ignore the selected platform until the actor clears it, and preserve ordinary Solid collision
- Fidelity: `partial`; this calibrates static one-way-platform contact only, not moving platforms, ducking, dash corner correction, special platform variants or complete Player parity
- Acceptance: deterministic per-tick tests for upward pass-through, downward landing, stable standing, explicit drop-through/rearm, side exclusion, Solid non-regression, immutable evidence and stable diagnostics; cumulative generated demo and complete Release gate
- Evidence: Release 0 warnings/errors; Simulation.Core 42/42; Player 59/59; total 901/901; generated demo passes upward, drops from y=0, lands on `one-way-lower` at y=19, records start/completion/landing `1/1/1`, rearms and replays identically
- Forbidden: game/GUI/live input, installation or local-reference access, commercial bytes, CDR-061 work, moving/special one-way variants and upload before developer acceptance
- Next gate: CDR-075 separately authorized on 2026-10-03

### CDR-075 — Player ducking and safe unduck clearance calibration

- Owner: Primary
- State: implementation complete and acceptance-pending on 2026-10-03; not uploaded
- File scope: minimal Actor height-resize/clearance support in Simulation.Core; Player Normal input/state/snapshot/events/tuning; generated Core/Player tests; cumulative demo, verifier, launchers and project records
- Scope: explicit generated duck input, feet-anchored reduced collision height, grounded duck friction, collision-checked restoration of standing height, blocked-rise diagnostics, safe ordinary jump and immutable per-tick evidence
- Fidelity: partial; project calibration, not complete commercial numeric parity or dash/climb duck interactions
- Acceptance: low-ceiling refusal without overlap or foot/subpixel drift, clear-space recovery, ordinary Solid and one-way grounding, deterministic replay and full Release gate
- Evidence: Core 48/48, Player 72/72, total 920/920; nine-tick generated trajectory, heights 6/11, feet y=11, entry/blocked/completion events 1/2/1, no overlap and identical replay; both no-open/no-pause launchers pass
- Forbidden: game/GUI/live input, install or local-reference access, commercial bytes, CDR-061, dash/climb special interactions and upload before developer acceptance
- Next gate: developer acceptance of CDR-075; later calibration tasks require authorization

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
- State: accepted by developer on 2026-09-29
- Scope: anonymous desktop geometry, DPI, visible-surface state and velocity through isolated platform-neutral and Windows modules
- Forbidden: window titles, text/content, screenshots, input, identity-bearing process metadata, raw native handles in contracts/evidence, visible GUI and game/install access
- Evidence: Release 0 warnings/errors; 19/19 generated desktop cases and 337 total regressions; one authorized hidden Windows proof passed with aggregate count/DPI evidence only; cumulative demo uses generated geometry and records no real desktop values.
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the generated anonymous geometry table, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/DESKTOP_GEOMETRY.md`.
- Gate: published at `codex/cdr-031-desktop-geometry` commit `311c426`. CDR-032 is authorized for offline validated-catalog animation presentation only; visible GUI, live input, installation writes and commercial-byte persistence remain forbidden.

### CDR-032 — Asset-to-animation presentation

- Owner: Primary
- State: accepted by developer on 2026-09-29; independent-branch publication authorized after fresh verification and outbound audit
- Scope: convert validated immutable catalog animation frames into offline immutable render snapshots with deterministic animation timing and explicit health events
- Forbidden: visible GUI, live input, installation writes, commercial-byte persistence, filesystem/game access and render-to-physics feedback
- Evidence: Release 0 warnings/errors; 28/28 animation cases and 365 total regressions; cumulative parsed/catalogued two-frame demo runs 8 fixed ticks, 8 offline presents, 3 pixel changes and identical replay.
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the CDR-032 tick/frame table, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/ANIMATION_PRESENTATION.md`.
- Gate: published at `codex/cdr-032-animation-presentation` commit `1c39c0a`. CDR-040 Theo Crystal is authorized for generated offline geometry/input and verified contracts only; no visible GUI, installation access or commercial bytes.

## P4 — Interaction entities

Each entity owns a separate module, tests and parity row:

### CDR-040 — Theo Crystal

- Owner: Primary
- State: accepted by developer on 2026-09-29; independent-branch publication authorized after fresh verification and outbound audit
- Scope: isolated `CelesteDesktop.Entity.Theo` deterministic fixed-tick module, focused tests, generated cumulative demo, verifier and project records
- Implemented behavior: immutable input/state snapshots; explicit pickup/carry/drop/throw; holder-relative placement; gravity, terminal fall and friction; Solid collision, landing bounce and LiftSpeed handoff; bounded squish failure; structured events and deterministic replay
- Required matrix: Theo with Player pickup/carry/throw requests, Theo with static/moving Solid, and failure isolation from Player and unrelated entities
- Evidence: Release 0 warnings/errors; 37/37 focused cases and 402/402 total regressions; 36-tick generated demo has 1 pickup, 1 throw, 1 horizontal bounce, 2 landings and identical replay
- Fidelity: `partial`; the generated interaction matrix passes, but the official public repository does not expose Theo entity behavior and original numeric parity is not established
- Forbidden: visible GUI, live input, game/install access or writes, commercial bytes, filesystem/platform/rendering/desktop dependencies
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the Theo table, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/THEO_CRYSTAL.md`.
- Gate: published at `codex/cdr-040-theo-crystal` commit `23531c3`. CDR-041 Glider is authorized for generated offline geometry/input and verified contracts only.

Remaining order:

2. CDR-041 Glider — accepted by the developer on 2026-09-29; independent-branch publication authorized after fresh verification and outbound audit. Isolated fixed-tick states cover pickup/carry/drop/throw, a holder fall-speed-limit effect applied through the generic Player external-effect contract, open/closed slow flight, collision bounce/landing, lift inheritance, destroy/squish isolation and stable events. Release passes Player 33/33, Glider 42/42 and 447/447 total regressions; the 48-tick generated demo records pickup, one fall-limit request, one Player application, throw, horizontal bounce, landing and identical replay. Fidelity remains `partial`; formal App orchestration, original numeric parity, asset animation and visible presentation remain unverified. Manual acceptance: double-click `演示当前进度.cmd`, inspect the Glider table, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/GLIDER.md`.
3. CDR-042 Spring — accepted and published at `codex/cdr-042-spring` commit `c2717fa`. Isolated fixed-tick state covers four orientations, target-addressed launch effects, retraction/cooldown/reset, disable/enable and release-to-rearm. Player, Theo and Glider actually apply the generic velocity effect without Spring depending on them. Release passes Spring 42/42 and 489/489 total regressions; the 30-tick generated demo records 3 activations, 3 launches, 3 ready transitions and one application per supported target with identical replay. Fidelity remains `partial`; formal App orchestration, original contact/numeric parity, asset animation and visible presentation remain unverified.
4. CDR-043 Refill — accepted and published at `codex/cdr-043-refill` commit `9d05ea67`. Fresh Release verification passed Refill 42/42 and 531/531 total plus a clean tracked-tree outbound audit; remote `main` remained unchanged. Isolated fixed-tick state covers resource-aware Player contact, target-addressed dash/stamina restoration, cooldown/respawn, disable/enable and release-to-rearm. Player actually applies and clamps the generic resource effect without Refill depending on Player. The 16-tick generated demo records 2 collections, 2 restore requests, 2 respawns and 2 Player applications with identical replay. Fidelity remains `partial`; formal App routing, original contact/respawn parity, special variants, asset animation and visible presentation remain unverified. CDR-044 was separately authorized on 2026-10-01; see the next item.
5. CDR-044 Water — accepted and published at `codex/cdr-044-water` exact commit `009c8811`. A fresh isolated 579/579 gate passed with 0 Release warnings/errors and a clean outbound audit; remote `main` remained unchanged. The isolated rectangular volume covers enter/submerged/exit, drag, neutral buoyancy, directional swimming, speed limits, stable multiple occupants, disable/enable and cross-entity isolation; Player separately applies target-addressed generic velocity. Fidelity remains `partial`; original surface/contact/numeric parity, formal App routing, assets and visible presentation remain unverified.
6. CDR-045 Bumper — accepted and published at `codex/cdr-045-bumper` exact commit `556939cb`. A fresh 625/625 gate passed with 0 Release warnings/errors and a clean outbound audit; remote `main` remained unchanged. The isolated circular contact covers outward radial target-addressed launch, deterministic coincident-center upward fallback, cooldown, release-to-rearm, disable/enable, two-Bumper isolation, actual Player generic-velocity application and Player-owned Solid collision. Fidelity remains `partial`; original tuning, movement path, variants, assets, audio, App routing and visible presentation are unverified.
7. CDR-046 Puffer — accepted and published at `codex/cdr-046-puffer` exact commit `903d137`. A fresh isolated 684/684 gate passed with 0 Release warnings/errors and a clean outbound audit; remote `main` remained unchanged. Generated-only behavior covers bounded horizontal swim/turns, proximity warning with locked target, deterministic radial explosion/launch, coincident-center fallback, spent cooldown/spawn reset, disable/enable, two-Puffer isolation and actual Player generic-velocity application. The 20-tick demo records swim 8, turn 3, warning/explosion/launch 3 each, respawn 2, center fallback 1, ignored 1 and Player applied 3 with identical replay. Fidelity remains `partial`; original movement/contact/timing/numeric parity, special interactions, assets, audio, App routing and visible presentation are unverified. CDR-047 was later authorized separately; see the next item.
8. CDR-047 Seeker — accepted and published at `codex/cdr-047-seeker` exact commit `14a5142`. Owner: Primary agent. A fresh isolated Release gate passed Seeker 66/66 and 750/750 total with 0 warnings/errors; the outbound audit found 0 forbidden commercial-media/build-output paths, 0 binary diffs, 0 blobs over 1 MiB and 0 real-install/user-path hits. Generated-only behavior covers bounded patrol/turn, eligible target detection and identity lock, alert, chase, lost-sight grace, windup, normalized dash, target-addressed hit fact, wall-hit stun, dash timeout, deterministic spawn recovery, disable/enable and two-Seeker isolation. The 30-tick demo records patrol 7, alert 3, chase-start 3, chased 3, windup 2, dash-start 2, dashed 1, target-hit 1, wall-hit 1, stunned/recovered 2 each and target-lost 1 with identical replay. Remote `main` remains unchanged. Fidelity remains `partial`; original navigation, obstacle avoidance, damage/bounce behavior, numeric parity, assets/audio, Player death, App routing and visible presentation are unverified. No later task is authorized.
9. CDR-050 Offline App orchestration — accepted and published at `codex/cdr-050-app-orchestration` exact commit `8430a85`. A fresh 800/800 gate passed with 0 Release warnings/errors and an outbound audit with 0 forbidden paths, binaries, large blobs or real-install/user paths. After two earlier GitHub timeouts, publication and remote read-back succeeded; remote `main` remained `d237277`. Fidelity remains `partial`; visible host, live input, selected-install assets, original parity and human visibility are not established.
10. CDR-051 Headless App Host — accepted and published at `codex/cdr-051-headless-app-host` exact commit `6c60fee`. Owner: Primary agent. The pure-offline host wraps CDR-050 with an injected monotonic clock, fixed 60 Hz scheduling, bounded catch-up, generated immutable input, cancellation, graceful stop/dispose and stable full-error diagnostics. Release built with 0 warnings/errors; focused tests passed 26/26 and all regressions passed 826/826. The cumulative demo proved normal cadence executed 6 ticks with 0 drops and a simulated eight-interval stall executed 4 ticks while explicitly dropping 4 stale intervals; both stopped cleanly and replayed identically. The outgoing audit and remote read-back passed; remote `main` remained unchanged.

No entity is accepted until its Player, Solid and supported entity-to-entity interaction matrix passes.

## P5 — Deferred content extensions

These tasks reserve future capability. Only CDR-060 is currently authorized; later providers remain separately gated.

### CDR-060 — Compiled provider contracts

- Owner: Primary agent.
- State: implementation-complete and acceptance-pending; do not upload before developer acceptance.
- Scope: compiled data-only `IAssetSourceProvider` and immutable `IWorldContentProvider` contracts, bounded results, synthetic providers, tests, cumulative demo and records.
- Evidence: Release 0 warnings/errors; 30/30 focused and 856/856 total regressions; generated demo returns 4 bounded bytes and one world/room with 2 Solids, 1 spawn and 3 supported entities, rejects both over-budget requests and replays identically.
- Role: a safe socket for future map and data-only Mod readers; it prevents filesystem or executable behavior from entering Simulation contracts.
- Gate: synthetic providers only; no plugin loader, Mod directory, real map, install access, GUI, live input or commercial bytes. CDR-061 is not authorized.
- Manual acceptance: double-click `演示当前进度.cmd`, inspect the CDR-060 provider table and plain-language limits, then double-click `验证当前版本.cmd`; details in `docs/zh-CN/EXTENSION_PROVIDER_CONTRACTS.md`.

### CDR-061 — Level/map content provider

- Scope: normalize selected room geometry, spawn points and supported entity placements behind `IWorldContentProvider`.
- Gate: synthetic map fixtures first; real formats and installations require a new task and explicit authorization.

### CDR-062 — Read-only Mod asset provider

- Scope: resolve explicitly enabled, data-only Mod assets behind `IAssetSourceProvider`.
- Gate: separate security review, deterministic precedence, bounded reads and fresh authorization; never execute DLLs or scripts.
