# CDR-015 Update — Normalized Synthetic Asset Catalog

## Added function

Generated Atlas metadata, generated page bytes and generated sprite definitions now join into immutable catalogs separated by entity. Required pages are opened on demand and decoded at most once per build; trimmed rectangles are reconstructed into transparent full frames.

The catalog carries caller-supplied source summaries and a deterministic SHA-256 covering canonical entity structure and frame-content hashes. Equal animation names belonging to different entities remain separate. No filesystem reader, global bitmap dictionary or disk cache was added.

## Verification

- Release build: 0 warnings, 0 errors.
- Regression: CDR-010 15/15, CDR-011 27/27, CDR-012 35/35, CDR-013 31/31, CDR-014 40/40.
- CDR-015: 26/26 generated catalog tests.
- Cumulative demo: 2 entity catalogs, 2 frames, 1 decoded page, 0 commercial bytes.

## Remaining gate

This proves the generated pipeline only. CDR-016 real-install conformance is not authorized and must not begin without fresh explicit developer permission.
