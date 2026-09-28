# CDR-015 Normalized Asset Catalog

## Outcome

CDR-015 combines caller-supplied Atlas metadata, sprite metadata and page streams into an immutable catalog separated by entity. It uses generated inputs only in this milestone and never locates or reads a game installation.

## Contract

- The caller supplies an explicit entity allowlist and a stream-only `IAtlasPageStreamSource`.
- Explicit frame expressions resolve to the animation path plus a zero-padded numeric suffix; all-frame expressions select only numeric suffixes and sort them numerically.
- Entity and animation namespaces remain nested. There is no global animation-name or bitmap dictionary.
- Only pages referenced by selected frames are decoded. A build-local memory cache prevents decoding one required page more than once and is discarded after the build.
- Atlas rectangles are copied into transparent, untrimmed BGRA32 frames using validated trim offsets.
- Entity, animation, frame, decoded-page and pixel-byte budgets fail closed with stable `CATALOG_*` codes.
- The source fingerprint contains logical path, byte length and SHA-256 summaries supplied by the caller. The catalog fingerprint deterministically covers the normalized source summaries, entity structure and frame-content hashes.
- No disk cache or filesystem implementation exists in this module.

## What it does for the project

This is the joining layer between the parsers and later animation rendering. Earlier tasks could read three separate generated formats; this task turns their results into a single, safe list of frames for each character or object. Later rendering can ask for `player/idle` or `spring/idle` without those equal animation names overwriting each other.

It still does not prove that the real installed Celeste files match these assumptions, and it does not display or move a character. Real-install conformance is CDR-016 and requires fresh authorization; simulation and rendering are later phases.

## Developer acceptance

1. Double-click `演示当前进度.cmd`.
2. Confirm the report says `CDR-015`, `diagnostic_placeholder=true`, 2 entity catalogs, 2 catalog frames, 1 decoded page and 0 commercial bytes.
3. Double-click `验证当前版本.cmd`.
4. Confirm the Release build has 0 warnings/errors and all CDR-010 through CDR-015 suites report zero failures.

Do not interpret this as authorization for CDR-016, a real installation read, a game launch, GUI work or real desktop observation.

## Feature after acceptance

Acceptance permits publishing this generated-only CDR-015 version. The next task is CDR-016 opt-in real-install conformance, but it must not start without a separate, fresh authorization defining the selected installation and read-only evidence boundary.
