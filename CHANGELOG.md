# Changelog

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

## 2026-09-21 — Independent project foundation

- Created a new repository without deleting or modifying DesktopSummit Legacy.
- Defined a read-only正版-install asset architecture that never launches Celeste/Everest at product runtime.
- Split install discovery, isolated parsing, catalogs, simulation, entities, desktop geometry, rendering, diagnostics and App composition.
- Added stable health-chain semantics so parsing, spawn, simulation, submission, presentation and human visibility cannot be conflated.
- Added safety, reference provenance, parity and phased task contracts. No product code or commercial asset was added.
