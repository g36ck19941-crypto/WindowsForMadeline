# CDR-001 — Independent project foundation

Manual acceptance guide: `docs/reviews/CDR-001-ACCEPTANCE.md`.

Date: 2026-09-21

## Outcome

Created a new Git repository for an independent desktop runtime that reads a user-owned Celeste installation without launching Celeste/Everest. No prior implementation was copied, and the DesktopSummit Legacy repository remained unchanged at clean HEAD `12d21321acc37ca366d2c449155fd4bfb78a7d92` on `feature/ds015h-hidden-runtime-poc`.

## Contracts established

- Separate install, AssetWorker, asset catalog, simulation, entity, desktop, renderer, diagnostics and App boundaries.
- Stable structured health chain from installation selection through human-visible acceptance.
- Read-only, allowlisted, bounded parsing with no commercial bytes in Git, CI, logs or releases.
- Independent asset, behavior, integration and human acceptance states.
- Ordered work from synthetic install validation to opt-in real-install conformance, simulation, rendering and entities.
- Deferred, non-executable provider seams for future level/map restoration and data-only Mod assets; no current provider implementation or directory access.

## Verification

- Documentation and machine-readable task manifest exist.
- JSON manifest parses successfully.
- No game/Everest/GUI was launched.
- No game directory was read or written.
- No commercial asset, external source file or Legacy implementation was copied.

## Remaining gate

CDR-001 was accepted by the developer on 2026-09-21. CDR-010 is authorized only for an injectable filesystem and synthetic fixtures; it must not inspect the real machine or a game installation.
