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
