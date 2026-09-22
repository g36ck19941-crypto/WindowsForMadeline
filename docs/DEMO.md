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

Status: ready for developer acceptance

The current `演示当前进度.cmd` extends the cumulative generated-data report with two allowlisted sprite definitions and two animations parsed from generated XML. It cross-checks their logical animation paths against the two generated Atlas entries, alongside the prior 48 exact pixels. The report is now `artifacts/cdr-014-demo/index.html`, still with zero commercial bytes and `diagnostic_placeholder=true`.

The current `验证当前版本.cmd` adds 40 CDR-014 XML cases to the previous 108 tests. Expected results: `15/15`, `27/27`, `35/35`, `31/31`, `40/40`. CDR-013's prior demo is preserved in its accepted branch; the root launchers always target the latest local milestone.
