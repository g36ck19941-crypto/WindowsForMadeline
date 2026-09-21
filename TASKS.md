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
- Gate: satisfied for CDR-002 only; CDR-001 remains independently pending review.

## P1 — Safe asset foundation

### CDR-010 — Explicit install verifier

- Owner: Primary
- Scope: `src/CelesteDesktop.Install/**`, its tests, contracts and records
- State: implemented and synthetic-verified; pending developer acceptance
- Deliverable: validate only a caller-supplied directory through an injectable filesystem; no automatic machine scan in this stage
- Evidence: Release build 0 warnings/errors; 15/15 complete, missing, root/type, reparse, containment, inaccessible and API-surface tests passed
- Manual acceptance: `docs/zh-CN/INSTALL_VALIDATION.md`, then run `.\tools\Verify-CDR010.ps1`
- Gate: developer acceptance before CDR-011; no real install access

### CDR-011 — AssetWorker protocol and supervision

- Scope: bounded IPC contracts, worker lifecycle, timeout, cancellation and crash recovery
- Gate: worker crash/timeout/malformed envelope cannot crash or hang App; no parser yet

### CDR-012 — Atlas metadata reader

- Scope: independent `.meta` reader into immutable descriptors
- Gate: synthetic positive fixtures plus truncation, count, path, dimension, overflow and trailing-data negatives

### CDR-013 — Atlas page decoder

- Scope: bounded `.data` RLE decoder to immutable BGRA32 buffers
- Gate: exact synthetic pixels, alpha/channel/stride checks, malformed-run and decompression-budget negatives

### CDR-014 — Sprite metadata reader

- Scope: hardened `Sprites.xml` reader for explicit allowlisted player/entity definitions
- Gate: bounded XML fixtures, animation/frame/origin/hair metadata, duplicate/missing/unknown-node negatives

### CDR-015 — Normalized asset catalog

- Scope: allowlist, page-on-demand decode, source fingerprints, immutable asset catalog
- Gate: no flattened ambiguous names, no global bitmap dictionary, no disk cache, deterministic catalog fingerprint

### CDR-016 — Opt-in real-install conformance

- Requires: explicit fresh authorization
- Scope: read-only selected installation; summary hashes and counts only
- Gate: zero writes, zero commercial bytes in evidence, exact required-frame resolution, decoder output fingerprint stability

## P2 — Deterministic simulation

### CDR-020 — Actor/Solid kernel

- Fixed 60 Hz, whole-pixel actor position, subpixel remainder, collision ordering, moving-solid carry and LiftSpeed contracts.

### CDR-021 — Madeline Normal/Jump

- Run, friction, gravity, fast fall, coyote, buffer and variable jump with exact tick evidence.

### CDR-022 — Dash/Wall/Climb

- Direction quantization, dash lifecycle, assists, wall slide/jump, climb, stamina and ledge transitions.

## P3 — Rendering and desktop adapter

### CDR-030 — Synthetic DirectComposition presenter

- Program-generated checkerboard only; device loss, DPI, virtual desktop, alpha and Present diagnostics.

### CDR-031 — Desktop geometry adapter

- Anonymous visible surfaces and velocity; no titles, contents, screenshots or typed input.

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
