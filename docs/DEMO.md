# Demonstrations

## Demo 001 — Foundation audit

Status: accepted by the developer on 2026-09-21

This is a non-runnable foundation demo. Reviewers can verify:

1. Legacy repository branch/HEAD is recorded and untouched.
2. Product, legal and no-game-runtime boundaries are explicit.
3. Modules have one-way dependencies and an isolated AssetWorker.
4. Diagnostics distinguish every step from install selection through human visibility.
5. Parser safety, parity vocabulary and task order exist.
6. Repository contains no product binary, game path, game asset, cache or copied external source.
7. Level/map and data-only Mod capabilities are deferred through isolated provider seams rather than implemented or permanently rejected.

Evidence: `docs/updates/2026-09-21-project-foundation.md`.

Guided acceptance: `docs/reviews/CDR-001-ACCEPTANCE.md` provides a requirement-to-evidence crosswalk and a five-minute manual review. It does not launch any program or claim a runtime demo.

中文验收资料：`docs/zh-CN/README.md`；英文规范仍是代理执行依据。

## Demo 010 — Synthetic install verifier

Status: accepted by the developer on 2026-09-21

Run `.\tools\Verify-CDR010.ps1`. The demo builds Release and runs 15 in-memory filesystem cases. It neither discovers nor reads a real installation. Expected final line: `RESULT total=15 passed=15 failed=0`.

## Demo 011 — Bounded Worker lifecycle

Status: accepted by the developer on 2026-09-21

Run `.\tools\Verify-CDR011.ps1`. It runs the CDR-010 regression and 27 Worker cases, including one real hidden project Worker Start→Ping→Stop lifecycle. Expected result lines are `15/15` and `27/27`. No asset request exists.

## Demo 012 — Synthetic Atlas metadata reader

Status: accepted by the developer on 2026-09-21

Run `.\tools\Verify-CDR012.ps1`. It runs the prior 15 and 27 regression cases plus 35 metadata/API-surface cases. Expected result lines are `15/15`, `27/27` and `35/35`. No game file is opened.

## Demo 013 — Synthetic Atlas page decoder

Status: accepted by the developer on 2026-09-21

Double-click `演示当前进度.cmd`. It generates an 8×6 diagnostic Atlas, runs CDR-012 over its generated metadata and CDR-013 over its generated pixel runs, verifies 48 decoded pixels, then opens `artifacts/cdr-013-demo/index.html`. The report shows two named regions, pipeline stages, fingerprint and limitations. It uses zero commercial bytes.

Then double-click `验证当前版本.cmd`. It runs the prior 15, 27 and 35 regression cases plus 31 generated `.data`, fragmented-stream, immutability and API-surface cases, and independently validates the demo manifest/report. Expected result lines are `15/15`, `27/27`, `35/35` and `31/31`. The underlying PowerShell command is retained for agents and CI only.

## Demo 014 — Synthetic sprite definitions

Status: accepted by the developer on 2026-09-28

The current `演示当前进度.cmd` extends the cumulative generated-data report with two allowlisted sprite definitions and two animations parsed from generated XML. It cross-checks their logical animation paths against the two generated Atlas entries, alongside the prior 48 exact pixels. The report is now `artifacts/cdr-014-demo/index.html`, still with zero commercial bytes and `diagnostic_placeholder=true`.

The current `验证当前版本.cmd` adds 40 CDR-014 XML cases to the previous 108 tests. Expected results: `15/15`, `27/27`, `35/35`, `31/31`, `40/40`. CDR-013's prior demo is preserved in its accepted branch; the root launchers always target the latest local milestone.

## Demo 015 — Normalized synthetic asset catalog

Status: accepted by the developer on 2026-09-28

Double-click `演示当前进度.cmd`. The cumulative generated pipeline now joins the Atlas index, decoded page and sprite definitions into two isolated entity catalogs with two frames. It records that one required page was opened and decoded once, emits a deterministic catalog fingerprint, and writes `artifacts/cdr-015-demo/index.html`. The report remains a diagnostic placeholder with zero commercial bytes.

Then double-click `验证当前版本.cmd`. Expected suite results are `15/15`, `27/27`, `35/35`, `31/31`, `40/40` and `26/26`, followed by a successful demo-manifest check. It does not read a real installation or authorize CDR-016.

## CDR-016 — selected-install compatibility

The current `验证当前版本.cmd` remains fully offline and now runs seven suites: `15/15`, `27/27`, `35/35`, `31/31`, `44/44`, `29/29` and `10/10` (191 cases). It does not locate or read Celeste.

`验证真实安装兼容性.cmd` is a separate opt-in entry. Drag the explicitly selected installation folder onto it, or paste the path when prompted. It repeats the full offline gate, then reads only the three required source files and writes a hash/count report under project `artifacts`. Expected outcome: 5 definitions, 93 animations, 706 frames, stable fingerprints, zero installation writes and zero commercial bytes persisted. This is not a visible-character demo.

## CDR-020 — Actor/Solid simulation

Double-click `演示当前进度.cmd`. The report retains the generated asset pipeline and adds a 13-tick table for one generated Actor, moving platform and wall. It must show three `ActorCarried` rows, one `ActorBlocked` row and an identical replay result.

Then double-click `验证当前版本.cmd`. Expected suite results are `15/15`, `27/27`, `35/35`, `31/31`, `44/44`, `29/29`, `10/10` and `28/28`, followed by the CDR-020 manifest check. This proves the offline kernel behavior only; it does not prove Madeline movement parity.

## CDR-021 — Madeline Normal/Jump

Double-click `演示当前进度.cmd`. The cumulative report retains the generated asset and Actor/Solid stages and adds a 24-tick player table. It must reach speed 90, show one `Jumped` event at tick 7, reach `Y=-16`, and mark the replay identical.

Then double-click `验证当前版本.cmd`. Expected suite results are `15/15`, `27/27`, `35/35`, `31/31`, `44/44`, `29/29`, `10/10`, `28/28` and `30/30`, followed by the CDR-021 manifest check. This proves generated-input behavior only. Fidelity is `partial`; no live input, rendering, real installation or complete shipped-build parity is claimed.

## CDR-022 — Dash, Wall and Climb

Double-click `演示当前进度.cmd`. The cumulative report retains every earlier stage and adds a 24-tick traversal table. It must contain one `WallSlideStarted`, `WallJumped`, `DashStarted`, blocked dash and `ClimbStarted`; climb speed reaches `-45`, stamina decreases below 110, stationary special-state rows do not jitter, and replay is identical.

Then double-click `验证当前版本.cmd`. Expected suite results are `15/15`, `27/27`, `35/35`, `31/31`, `44/44`, `29/29`, `10/10`, `33/33`, `30/30` and `44/44`, for 298 total cases, followed by the CDR-022 manifest check. This remains generated-input behavior with `partial` fidelity and no game, GUI, real desktop, installation or live input.

## CDR-030 — Synthetic DirectComposition presentation

Double-click `演示当前进度.cmd`. The cumulative report adds a generated premultiplied-alpha checkerboard and a presentation health-chain table. It also restores CDR-016 as a separately passed read-only milestone and explicitly states that CDR-017 through CDR-019 are unassigned reserved numbers. Expected values are 3 presents, 2 changed-pixel events, `human_visible=false` and 0 commercial bytes.

Then double-click `验证当前版本.cmd`. The prior 298 cases remain, and the rendering suite must report `20/20`, for 318 total cases. The final line must be `CDR-030 HIDDEN VERIFICATION PASSED`. The native test creates a never-shown HWND and actually performs D3D11 uploads, DirectComposition `Commit` and `WaitForCommitCompletion`; it does not prove human visibility or inspect the real desktop.
