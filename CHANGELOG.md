# Changelog

## 2026-10-01 — CDR-044 Water authorized

- Recorded developer authorization for an isolated deterministic Water volume using generated rectangles, velocities and swim axes only.
- Bounded implementation to enter/submerged/exit transitions, drag, buoyancy, directional swim targets, speed limits, multiple-occupant isolation and generic Player velocity application.
- Completed the independent Water module and 48 focused cases; the complete Release gate passes 579/579 with 0 warnings/errors.
- Extended the generated cumulative demo with a 12-tick Water trace: entered 3, submerged 13, motion issued 13, exited 2 and Player applied 10, with identical replay.
- Added stable Water diagnostics, English contracts, Chinese acceptance material and double-click developer entry updates.
- Kept original numeric parity, visible GUI, live input, game/install access, commercial bytes and CDR-045 Bumper outside the authorization.

## 2026-09-30 — CDR-043 accepted

- Recorded developer acceptance of CDR-043 and authorization for exact independent-branch publication after a fresh full gate and outbound audit.
- Exact commit `9d05ea67ed4c0aa7dcb45bddd74ba67577e17b14` passed Release with 0 warnings/errors, Refill 42/42 and 531/531 total regressions; the tracked-tree audit found no commercial media, binaries, archives, caches or build output.
- After two port-443 timeouts, connectivity recovered and exact commit `9d05ea67ed4c0aa7dcb45bddd74ba67577e17b14` was published and read back at `codex/cdr-043-refill`; remote `main` remained `d237277e10af090cf60ec015c22174565e6cdc0a`.
- Kept CDR-044 Water unauthorized; acceptance of Refill does not authorize the next entity automatically.
- Visible GUI, live input, game/install access or writes, and commercial-byte persistence remain forbidden.

## 2026-09-29 — CDR-043 deterministic Refill

- Added an isolated Refill module with immutable resource-aware contact, lifecycle, target restore effect and stable event snapshots.
- Implemented collection, dash/stamina restoration request, cooldown, respawn, disable/enable and release-to-rearm behavior.
- Added a generic bounded external-resource effect actually applied by Player without introducing Player dependencies into Refill.
- Added a 42-case interaction matrix and a 16-tick cumulative demo; Release passes 531/531 with 0 warnings/errors.
- Kept fidelity `partial`: formal App routing, original contact/respawn parity, special variants, asset animation and visible presentation remain unverified.

## 2026-09-29 — CDR-042 accepted

- Recorded developer acceptance of CDR-042 and authorization for exact independent-branch publication after a fresh full gate and outbound audit.
- Opened CDR-043 only for an isolated deterministic Refill module using generated geometry/input and verified contracts.
- Visible GUI, live input, game/install access or writes, and commercial-byte persistence remain forbidden.
- Reverification passed at 489/489 with 0 build warnings/errors; exact commit `c2717fa` was published and read back at `codex/cdr-042-spring` while remote `main` stayed unchanged.

## 2026-09-29 — CDR-042 deterministic Spring

- Added an isolated Spring module with immutable contact, state, target launch effect and stable event snapshots.
- Implemented four orientations, activation, retraction, cooldown, reset, disable/enable and release-to-rearm behavior.
- Added a generic axis-selective external-velocity effect actually applied by Player, Theo and Glider without introducing Spring dependencies into those modules.
- Added a 42-case interaction matrix, including safe default-input behavior, and a 30-tick cumulative demo; Release passes 489/489 with 0 warnings/errors.
- Kept fidelity `partial`: formal App routing, original contact/numeric parity, asset animation and visible presentation remain unverified.

## 2026-09-29 — CDR-041 accepted

- Recorded developer acceptance of CDR-041 and authorization for exact independent-branch publication after a fresh full gate and outbound audit.
- Opened CDR-042 only for an isolated deterministic Spring module using generated geometry/input and verified contracts.
- Visible GUI, live input, game/install access or writes, and commercial-byte persistence remain forbidden.
- Reverification passed at 447/447 with 0 build warnings/errors; after transient connectivity failures, exact commit `ccc1a6b` was published and read back at `codex/cdr-041-glider` while remote `main` stayed unchanged.

## 2026-09-29 — CDR-041 deterministic Glider

- Added an isolated Glider module with immutable fixed-tick actions, holder snapshots, outward holder effects, states and stable semantic events.
- Implemented pickup/carry/drop/throw, holder fall-limit request plus application through Player's generic external-effect entry, open/closed slow flight, gravity/friction, Solid bounce/landing, lift inheritance, explicit destroy and per-entity squish isolation.
- Added Player external-effect coverage and a Player/Solid/Theo/two-Glider interaction matrix; Player 33/33, Glider 42/42 and 447/447 total regressions pass with 0 warnings/errors.
- Extended the cumulative report with a deterministic 48-tick Glider trajectory containing pickup, fall-limit request, Player application, throw, horizontal bounce and landing events.
- Kept behavior fidelity `partial`: formal App orchestration, original numeric parity, asset animation, visible GUI, live input, installation access and commercial bytes are not claimed.

## 2026-09-29 — CDR-040 accepted

- Recorded developer acceptance of CDR-040 and authorization for exact independent-branch publication after a fresh full gate and outbound audit.
- Opened CDR-041 only for an isolated deterministic Glider module using generated geometry/input and verified contracts.
- Visible GUI, live input, game/install access or writes, and commercial-byte persistence remain forbidden.
- Reverification passed at 402/402 with 0 build warnings/errors; the outbound audit was clean, and exact commit `23531c3` was published and read back at `codex/cdr-040-theo-crystal` while remote `main` stayed unchanged.

## 2026-09-29 — CDR-040 deterministic Theo Crystal

- Added an isolated Theo Crystal module with immutable fixed-tick inputs, state snapshots and stable semantic events.
- Implemented pickup, exact carry, drop, facing-aware throw, gravity, terminal fall, friction, Solid bounce/landing, moving-solid lift inheritance and per-entity squish isolation.
- Added a Player/Solid/two-Theo interaction matrix and 37 focused cases; Release passes 402/402 total regressions with 0 warnings/errors.
- Extended the cumulative report with a deterministic 36-tick Theo trajectory containing pickup, throw, horizontal bounce and landing events.
- Kept behavior fidelity `partial`: no original numeric parity, asset animation, visible GUI, live input, installation access or commercial bytes are claimed.

## 2026-09-29 — CDR-032 accepted

- Recorded developer acceptance of CDR-032 and authorization for independent-branch publication after a fresh full gate and outbound commercial-asset audit.
- Opened CDR-040 only for an isolated deterministic Theo Crystal module using generated geometry/input and verified contracts.
- Visible GUI, live input, game/install access or writes, and commercial-byte persistence remain forbidden.
- Reverification passed at 365/365 with 0 build warnings/errors; the outbound audit was clean, and exact commit `1c39c0a` was published and read back at `codex/cdr-032-animation-presentation` while remote `main` stayed unchanged.

## 2026-09-29 — Plain-language developer launchers

- Expanded both root `.cmd` launchers with plain Chinese explanations of what each entry does, what the developer should observe and how to interpret success.
- Rewrote the generated report's “what this proves / does not prove” area in plain Chinese with concrete frame-timing, positioning, determinism, one-way-state and remaining-visibility explanations.
- Made the distinction between cumulative demonstration and automated verification explicit, including that offline success does not prove real desktop visibility.
- Added failure guidance that asks for the first error and its stage; this is launcher/documentation UX only and does not change CDR-032 runtime behavior or acceptance state.
- Made `.cmd` files explicitly check out as CRLF and removed a non-ASCII source literal from the verifier so the double-click path also works under Windows PowerShell 5.1.

## 2026-09-29 — CDR-032 offline animation presentation

- Added fixed-tick playback over validated immutable catalog animations, including loop, final-frame hold, bounded direct goto and requested-state reset.
- Added bounded transparent-canvas composition using origin, position and horizontal flip, then connected composed frames one-way to Rendering.
- Added distinct resolution/composition events and full structured failure diagnostics without a Simulation dependency or feedback path.
- Added 28 focused cases and an 8-tick generated cumulative demo; all 365 regressions pass with 0 build warnings/errors, 0 visible GUI and 0 persisted commercial bytes.
- After one GitHub connectivity failure, accepted CDR-031 was published and read back at `codex/cdr-031-desktop-geometry` commit `311c426`; remote `main` stayed unchanged.

## 2026-09-29 — CDR-031 accepted

- Recorded developer acceptance of CDR-031 and authorization for independent-branch publication after a fresh gate and outbound audit.
- Opened CDR-032 only for offline presentation of validated immutable catalog animations, with no physics feedback, visible GUI, live input, installation writes or commercial-byte persistence.

## 2026-09-29 — CDR-031 anonymous desktop geometry

- Added platform-neutral anonymous surface snapshots, visibility/cloaking filtering, bounded geometry/DPI, ephemeral IDs and monotonic velocity.
- Added a Windows provider limited to read-only visibility, cloaking, rectangle and DPI APIs; no title, content, screenshot, input, process identity or native handle leaves the adapter.
- Added 19 generated tests, one authorized aggregate-only real Windows proof, and a generated cumulative demo section; all 337 regressions pass with 0 build warnings/errors.
- CDR-031 remains local pending acceptance. After transient network failures, accepted CDR-030 was published and read back at `codex/cdr-030-synthetic-presentation` commit `43cad14`; remote `main` stayed unchanged.

## 2026-09-29 — CDR-030 accepted

- Recorded developer acceptance of CDR-030 and re-ran its gate: 0 build warnings/errors, 20/20 rendering cases and 318/318 total regressions passed.
- Authorized independent-branch publication after the outbound audit.
- Opened CDR-031 only for anonymous geometry, DPI, visible-surface state and velocity; titles, content, screenshots, input and visible GUI remain forbidden.

## 2026-09-29 — CDR-030 hidden DirectComposition presenter

- Added an isolated rendering controller for immutable premultiplied BGRA32 frames, ordered upload/submit/present/change diagnostics and one bounded device-loss recovery.
- Added a Windows backend that creates a never-shown HWND, uploads generated pixels with D3D11, writes an `IDCompositionSurface`, commits DirectComposition and waits for commit completion.
- Added bounded DPI conversion, negative virtual-origin preservation, hardware/WARP selection and 20 rendering cases; all 318 regressions pass with 0 build warnings/errors.
- Extended the cumulative demo with a generated presentation checkerboard and health chain; restored the separately passed CDR-016 milestone and explicitly marked CDR-017 through CDR-019 as unassigned reserved numbers.
- Kept CDR-030 local pending acceptance and made no human-visibility claim. Two initial publication attempts were reset; after a read-back proved no partial refs, connectivity recovered and the accepted CDR-016/020/021/022 commits were published to four independent branches. Remote `main` remained unchanged.

## 2026-09-29 — CDR-016 through CDR-022 accepted

- Recorded developer acceptance of the cumulative CDR-016, CDR-020, CDR-021 and CDR-022 version after the developer completed verification.
- Re-ran the CDR-022 Release gate: 0 warnings, 0 errors and 298/298 offline cases passed; the generated traversal replay remained identical.
- Audited the outgoing range for commercial assets, decoded frames, audio, game binaries, caches and local installation paths; none were present.
- Authorized independent-branch publication and bounded the next CDR-030 work to generated graphics plus hidden automated tests; visible GUI and real-desktop observation remain separately gated.

## 2026-09-28 — CDR-022 generated dash, wall and climb behavior

- Added explicit Normal, Dash, WallSlide and Climb traversal states driven only by immutable generated inputs.
- Added eight-way direction quantization, dash charges/cooldown/refill/attack windows, wall checks/slide/jump, climb motion/stamina/tired/depleted states, climb jump, ledge hop and isolated infinite-resource assists.
- Fixed zero-displacement jitter at exact `+0.5`/`-0.5` subpixel remainders and added positive/negative midpoint regressions plus ordered offset-query coverage.
- Added 44 traversal cases; Simulation.Core now has 33 cases and all ten offline suites pass 298 cases with 0 build warnings/errors.
- Extended the cumulative demo with a 24-tick wall-slide, wall-jump, blocked-dash and climb trace with identical replay.
- Kept fidelity `partial` and CDR-022 local pending acceptance; stopped before the fresh-authorization CDR-030 GUI boundary.

## 2026-09-28 — CDR-021 generated Normal/Jump behavior

- Added a platform-independent player controller driven only by immutable per-tick input snapshots.
- Added run/friction, reduced air control, overspeed reduction, gravity/half gravity, normal/fast-fall caps, coyote time, jump buffer, jump boost and variable jump.
- Added explicit ground/collision events and immutable player snapshots, plus a once-per-world-tick orchestration guard.
- Added 30 focused cases; all nine offline suites pass 249 cases with 0 build warnings/errors.
- Extended the cumulative demo with a 24-tick run/jump trace and identical replay check.
- Classified fidelity as `partial`, kept the task local pending acceptance, and began authorized CDR-022 offline work.

## 2026-09-28 — CDR-020 deterministic Actor/Solid kernel

- Added a platform-independent fixed-60-Hz simulation assembly with whole-pixel bounds and decimal subpixel remainders.
- Added ordered per-pixel Actor/Solid collision, moving-Solid rider carry, overlap push, per-second LiftSpeed, explicit blocked/squish events and immutable snapshots.
- Added 28 generated-geometry and dependency-surface tests; all eight offline suites pass 219 cases with 0 build warnings/errors.
- Extended the cumulative generated-data demo with a 13-tick simulation trace containing carry and blocked events and an identical replay check.
- Kept CDR-016 and CDR-020 local pending acceptance; began CDR-021 under the developer's prior offline authorization.

## 2026-09-28 — CDR-016 selected-install conformance

- Published the developer-accepted CDR-015 history exactly to `codex/cdr-015-asset-catalog` at `673f798`; remote `main` was not targeted.
- Added an explicit-path, read-only real-install verifier with before/after source snapshots, repeat decoder/catalog fingerprints and a hash/count-only report.
- Corrected real-format compatibility for page suffixes, trailing animation separators, shared metadata paths, empty hair metadata, case differences and variable-width numeric frame suffixes; each correction has synthetic regression coverage.
- Verified the selected installation as 3 source files, 1 page, 6,824 entries, 5 definitions, 93 animations and 706 resolved frames, with zero installation writes and zero persisted commercial bytes.
- Added a separate developer-facing `验证真实安装兼容性.cmd`; the default `验证当前版本.cmd` remains offline and now runs 191 cases.
- CDR-016 remains local pending developer acceptance; CDR-020 offline implementation begins under prior authorization.

## 2026-09-28 — CDR-015 normalized synthetic asset catalog

- Added per-entity immutable catalogs that resolve generated sprite animations to generated Atlas frames without flattening equal animation names.
- Added page-on-demand stream decoding, one-build page reuse, transparent untrimmed-frame reconstruction and bounded `CATALOG_*` failures.
- Added caller-supplied source summaries and a deterministic catalog SHA-256 over normalized structure and frame hashes; no disk cache or filesystem reader was introduced.
- Added 26 generated catalog tests and extended the cumulative demo to two entity catalogs, two frames and one required decoded page with zero commercial bytes.
- Published the accepted CDR-014 commit to `codex/cdr-014-sprite-xml` at `aa8543c` after connectivity recovered; remote `main` remained `d237277`.
- Recorded developer acceptance of CDR-015 and explicit authorization for read-only CDR-016 conformance against the selected正版 installation, without game/GUI launch, installation writes or commercial-byte persistence.

## 2026-09-22 — CDR-014 synthetic sprite definitions

- Published the developer-accepted CDR-013 version to independent branch `codex/cdr-013-atlas-data` at `a29ec1d`; remote `main` remains `d237277`.
- Added an allowlisted, stream-only `Sprites.xml` reader with immutable animation, origin and frame-metadata contracts and stable `SPRITE_XML_*` failures.
- Added 39 generated XML/API-surface tests and extended the cumulative generated Atlas demo to two parsed sprite definitions and two animations.
- Kept developer-facing double-click launchers current with CDR-014 verification; no real installation, game or agent GUI access.
- Recorded developer acceptance of CDR-014 on 2026-09-28 and authorization to publish it and begin CDR-015 using generated descriptors and frames only.

## 2026-09-21 — GitHub connection and write test

- Configured the user-owned `WindowsForMadeline` repository as `origin`.
- Preserved remote `main` at `d237277` and pushed a documentation-only test branch `codex/connection-test-20260921` at `df00e2c`.
- Added the rule that product updates remain local until the developer confirms them effective; every approved push requires a change explanation and commercial-asset audit.
- After developer confirmation, published the audited foundation history to independent branch `codex/cdr-001-foundation` at `5a1dd1f`; remote `main` remained unchanged.
- Added a CDR-001 acceptance crosswalk and bounded manual-review procedure without introducing runtime code or assets.
- Added an isolated `docs/zh-CN/` mirror of every CDR-001 review document while keeping English as the canonical implementation source.
- Reclassified level/map restoration and data-only Mod assets from permanently unsupported to deferred, with non-executable provider seams and later gated tasks.
- Recorded developer acceptance of CDR-001 and the bounded authorization to begin synthetic-only CDR-010 work.
- Added CDR-010 explicit install structure validation, metadata-only filesystem abstraction, bounded issue contracts, 15 synthetic tests and an isolated offline verification command.
- Recorded developer acceptance of CDR-010 and authorization to begin bounded CDR-011 AssetWorker protocol and supervision.
- Added CDR-011 independent Worker process, bounded lifecycle protocol, restricted launch, timeout/cancellation/crash supervision, recovery and 27 verification cases.
- Recorded developer acceptance of CDR-011 and authorization to publish it and begin the synthetic-only CDR-012 `.meta` parser.
- Published accepted CDR-011 to `codex/cdr-011-asset-worker` at `b918996`; remote `main` remained unchanged.
- Added CDR-012 immutable atlas descriptors, bounded stream-only `.meta` parsing, stable validation failures and 35 verification cases, including Windows-ambiguous logical paths.
- Recorded developer acceptance of CDR-012 and authorization to publish it and begin the synthetic-only CDR-013 `.data` RLE decoder.
- Published accepted CDR-012 to `codex/cdr-012-atlas-metadata` at `1417b42`; remote `main` remained unchanged.
- Added CDR-013 immutable BGRA32 frames, bounded stream-only `.data` run decoding, stable validation failures and 29 generated-data verification cases.
- Hardened CDR-013 to 31 cases with fragmented-stream and maximum-run coverage.
- Added a cumulative generated Atlas progress demo that runs CDR-012 metadata parsing and CDR-013 pixel decoding, writes an offline HTML report and safe manifest, and persists zero commercial bytes.
- Added developer-facing double-click launchers `演示当前进度.cmd` and `验证当前版本.cmd`; PowerShell remains an internal implementation detail.
- Recorded developer acceptance of CDR-013 and authorization to publish it and begin the synthetic-only CDR-014 `Sprites.xml` reader.

## 2026-09-21 — Independent project foundation

- Created a new repository without deleting or modifying DesktopSummit Legacy.
- Defined a read-only正版-install asset architecture that never launches Celeste/Everest at product runtime.
- Split install discovery, isolated parsing, catalogs, simulation, entities, desktop geometry, rendering, diagnostics and App composition.
- Added stable health-chain semantics so parsing, spawn, simulation, submission, presentation and human visibility cannot be conflated.
- Added safety, reference provenance, parity and phased task contracts. No product code or commercial asset was added.
