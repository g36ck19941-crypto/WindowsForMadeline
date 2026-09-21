# Asset Safety and Local-Ownership Contract

## Runtime rule

The application reads resources only from a user-owned local Celeste installation. It never launches the game, loads Everest, deploys a mod or writes the installation.

## Approved inputs

Initial support is limited to explicitly required files under canonical locations:

- `Content/Graphics/Atlases/Gameplay.meta`
- referenced `Content/Graphics/Atlases/Gameplay*.data` pages
- `Content/Graphics/Sprites.xml`

Audio banks, Mod directories, archives and automatic Steam-library scanning are separate future tasks and are not implicitly authorized. `docs/EXTENSIONS.md` reserves a future data-only provider boundary; it grants no current access and never authorizes executable Mod code.

## Parser controls

- Caller supplies a canonical root; worker receives no unrestricted filesystem API through IPC.
- Reject absolute entry paths, traversal, alternate separators where disallowed, reparse points and paths outside the root.
- Enforce exact supported headers plus configured maxima for pages, entries, strings, dimensions, pixels and decoded bytes.
- Use checked arithmetic for width × height × stride and cumulative budgets.
- Require full reads and reject truncation, impossible page indices, invalid trim rectangles and ambiguous canonical IDs.
- A failed request publishes no partial catalog or frame.

## Persistence and distribution

- Phase 1 is memory-only.
- Any later local cache requires explicit approval, transactional publication, source fingerprints, restrictive permissions and complete invalidation on source/version change.
- No cache or decoded frame may enter Git, CI artifacts, support bundles or release packages.
- A build that cannot validate a local installation remains functional for diagnostics and synthetic tests but cannot claim original assets are available.
