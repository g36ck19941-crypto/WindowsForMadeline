# CDR-014 — Bounded sprite metadata reader

CDR-014 reads a caller-supplied `Stream` containing generated `Sprites.xml`-shaped data into immutable descriptors. It does not open files, discover an installation, load an atlas, extract frames, render, or run Celeste/Everest. Real-install compatibility is not established.

## Role in the project

CDR-012 supplies an atlas index and CDR-013 supplies in-memory page pixels. This stage supplies animation instructions: which named sprite uses which atlas path, which frame numbers form an animation, whether it loops, its delay and starting animation, plus origin, position and player hair/carry frame metadata. CDR-015 can later reconcile these instructions with actual atlas entries. This stage does not prove that a real sprite exists or can be displayed.

## Supported bounded form

- Root: `<Sprites>` with direct sprite elements selected by an explicit, nonempty allowlist. Unselected definitions are ignored. Every selected ID must exist exactly once; duplicate selected IDs fail.
- Selected sprite attributes: `path` (required), `start`, `delay`. Direct children: `Anim`, `Loop`, one of `Center`/`Justify`/`Origin`, `Position`, and at most one `Metadata`.
- Animation attributes: `id` (required), `path`, `delay`, `frames`; `Anim` may additionally have `goto`. The `goto` expression is preserved as bounded text, not executed or interpreted. `Loop` is marked as looping.
- Explicit frame expressions accept comma-separated nonnegative indices, inclusive `first-last` ranges (also descending), and `index*repeat` tokens. Missing/blank `frames` means `UsesAllFrames`, with no invented frame list. Physical atlas-frame existence remains a later catalog check.
- `Metadata` contains `Frames` entries referring to an animation ID, with bounded `hair` values (`x,y`, optional `:0`–`:2` facing, or `x` for hidden hair) and/or bounded integer `carry` offsets. Where explicit animation frames exist, metadata counts must match them.
- `Center`, normalized `Justify`, absolute `Origin`, and `Position` remain distinct descriptor values. Only one origin mode is accepted.

## Security and failure contract

- Defaults: at most 4 MiB input, 1,024 characters per parsed attribute/expression, 4,096 root definitions, 512 animations per selected definition, 4,096 expanded frames per animation, 4,096 frame-metadata entries, frame indices up to 1,000,000 and coordinates within ±16,384.
- XML DTDs and external entities are prohibited. The parser reads the bounded stream into memory, then uses an `XmlReader` with no resolver. It does not expose a filename/URI API.
- Selected definitions reject unknown attributes/nodes, malformed paths, missing or duplicate animations, unresolved `start`, origin conflicts, and malformed or mismatched hair/carry metadata. Failures use stable `SPRITE_XML_*` codes. Unselected definitions do not claim validity.
- Atlas paths are logical IDs, not filesystem paths. They are normalized to `/` and reject absolute paths, traversal, empty segments, reserved Windows stems and invalid characters.
- `goto` expressions are preserved but not validated against other animations in CDR-014; transition resolution belongs to a later catalog/animation task. The reader intentionally does not claim full Celeste XML compatibility before the separately authorized real-install conformance gate.

## Verification and acceptance

Double-click `演示当前进度.cmd` to generate and open `artifacts/cdr-014-demo/index.html`. The cumulative report uses generated `.meta`, `.data` and XML streams: one page, two atlas entries, 48 pixels, two sprite definitions and two animation definitions, with zero commercial bytes. It is labeled `diagnostic_placeholder=true`.

Then double-click `验证当前版本.cmd`. It runs Release build, 15 CDR-010, 27 CDR-011, 35 CDR-012, 31 CDR-013 and 40 CDR-014 checks, plus demo-manifest checks. The PowerShell entry is internal: `tools/Verify-CDR014.ps1`.

Acceptance of CDR-014 would authorize only CDR-015 synthetic normalized asset-catalog work. It would not authorize real installation reads, GUI or game launch, desktop observation, or commercial asset persistence.
