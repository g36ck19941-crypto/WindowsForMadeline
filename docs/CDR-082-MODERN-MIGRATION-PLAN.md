# CDR-082 — Non-legacy-framework migration design

2026-10-06. Primary. Baseline7ce988e, branch codex/cdr-082-local-compile-probe. Design only: existing contracts and aggregate reports, no recovered-source reads/edits/compilation/execution, probe runs, assembly/dependency inspection, GUI or installation access. No external research or new dependency selection. No runtime success is claimed.

## Decision and role

Preserve the original-code-led desktop outcome. The preferred design is a minimal original-behavior slice rebuilt for a non-legacy-framework runtime, with independently authored platform boundaries, not a legacy host, full game launch or another guessed gameplay simulator. This is a candidate route, not demonstrated feasibility. The production dependency closure must exclude legacy .NET Framework directly and indirectly, including helpers. Existing legacy compile/probes remain comparison/history only; fixing them is not a prerequisite for product integration.

Use current modern own modules as reusable components, not evidence that the original will work. Existing net8 restricted-start failure means even the modern host still requires a distinct compatibility diagnosis. No runtime version is selected as verified or upgraded/downloaded here. Do not bypass restrictions because a newer target is proposed.

## Evidence used and limits

- [Original emission report](CDR-082-COMPILE-PASS.md):795-file x86 library emitted against XNA/net472/Steamworks references, core types present, no execution. Broad closure is not a minimal isolated core; legacy emission is now diagnostic-only.
- [Boundary preflight](CDR-082-STATIC-BOUNDARIES.md):Player construction uses Input/sprites/atlas; Added requires Level bounds; Update includes time/input/assist/audio/scene. Ordinary Engine/game initialization reaches graphics/window/Steam boundaries. This is historical static observation, not exhaustive reachability.
- [Input/time plan](CDR-082-BRIDGE-PLAN.md):stored input states and original virtual-input logic offer possible seams; concrete/static/private APIs are not automatically injected by our interfaces. Old reflection plan does not establish modern binding.
- [XNA audit](CDR-082-XNA-AUDIT.md):two mixed-mode libraries have native/module initialization and unresolved boundaries. Do not assume ordinary retargeting or wrapping makes them nonlegacy-compatible/safe.
- [Own isolation report](CDR-082-ISOLATION.md):modern fixed60Hz/generated contexts,45 historical assertions, no original binding, no OS sandbox.
- [Animation](updates/2026-09-29-cdr-032-animation-presentation.md) and [desktop geometry](updates/2026-09-29-cdr-031-desktop-geometry.md):existing independent components; historical test counts are not rerun evidence or original animation/motion proof.
- Current startup delivery and [legacy boundary](LEGACY-FRAMEWORK-BOUNDARY.md):own old-framework normal startup blocked; modern startup also historically partial. Neither is original execution failure.

## Proposed dependency separation

| Area | Preserve | Adapt or assess | Required evidence |
| --- | --- | --- | --- |
| Behavior | Original state transitions, collision order, numerical operations and interaction rules | Minimal source/dependency slice, no invented substitutes | Exact source provenance/signatures; behavior changes separately approved; original-consumption traces |
| Time/input | Original buffering/consumption and update order | Own generated context connected to audited original seams; no device polling | Press/hold/release/consume and fixed-step traces from migrated logic, not adapter replay |
| Scene/session | Original Level/session/assist requirements relevant to slice | Minimal explicit environment, without ordinary Engine construction | Constructor/Added/update preconditions and no unaccounted scene calls |
| Types/platform | Required numeric/rectangle/color operations | Explicit compatible types/API strategy after inventory | Float/int/rounding/overflow/order checks; target must not load old runtime/mixed libraries |
| Assets/animation | Existing validated catalog/readers/presentation contracts | Local-only original naming/resource bridge | Separate asset resolution, original animation state, submitted pixels and later visibility evidence |
| Services | Original call intent where relevant | Denied audio/Steam/live input/file/window boundaries for offline host | Reachability/initializers; if denial changes required behavior, stop/markunsupported rather than no-op success |
| Desktop/App | Existing anonymous geometry and presentation | Thin host/lifecycle bridge later | Original step output remains independent from desktop integration and human visibility |

No FNA/MonoGame/other replacement is selected or claimed compatible; selection requires a separately scoped dependency/license/API assessment. Rendering compatibility must not reintroduce a legacy dependency through transitive references or a helper. Do not use original Engine startup merely to obtain types/resources.

## Local migration versus original preservation

Keep the recovered original baseline immutable. If later inventory shows source/API transformations necessary, propose a separate ignored derived working tree, exact allowed changes and reproducible provenance/hash/diff manifest BEFORE implementation. No original-source changes are authorized now. Separate platform redirection from behavior changes; movement constants, comparison/rounding rules, state/physics order and contact predicates must not be silently altered. Compilation errors cannot be resolved with guessed entity stubs.

Public repository: self-authored tools/adapters, synthetic fixtures and source-free summaries only. Original source, IL, resources, original-derived patched files and binaries remain ignored/local-only. Product assembly/dependency acceptance is separate from publication permission; a local compile does not authorize commercial redistribution.

## Ordered gates

| Gate | Deliverable and completion evidence | Permission needed next |
| --- | --- | --- |
| M0 — this design | Dependency separation/order/unknowns/review; no new running feature | Design currently authorized only |
| M1 — static migration inventory | Read only approved existing recovery cache; enumerate needed types/method edges/initializers and platform calls; private detailed map, public aggregate; classify preserve/adapt/unknown with evidence | Explicit existing-cache source read and self-authored static analysis scope; no target execution/new dependency reads |
| M2 — binding and platform design | Choose exact behavior slice/type and service contracts; proposed modern target and dependencies; source-transform change list if needed; dependency/side-effect/publication gates | Separate design scope for dependencies/transformations as required; not automatic substitution |
| M3 — private compile only | Authorized derived working copy emits for chosen modern target; static dependency closure/resource/entry/initializer audit shows no legacy runtime path; hashes/diff reviewed | Explicit local transformation/compile/reference authority; no execution |
| M4 — restricted own host | Own modern host ready/complete/timeout/exit/cleanup under validated restrictions; each required OS restriction proven independently | Own-only execution scope; not original/XNA permission; existing modern startup failure must be resolved without silent weakening |
| M5 — migrated input/time | Audited bounded original-derived slice consumes generated contexts with traceable buffering/time outputs, no Player/assets | Specific migrated-code execution after initialization/service review; stop on unknown effects |
| M6 — Player and frames (CDR-083) | Original-derived constructor/scene/update/animation traces and local resources/offscreen output; separately validate physics versus pixels | Exact character/resource/offscreen scope; not full game/GUI/live input |
| M7 — desktop/entities (CDR-084+) | Thin geometry/presentation host, each original entity separately integrated; later human visible/input acceptance | Desktop/GUI/input/resource scope separately approved |

M1 is the recommended next task. M3 static emission is NOT proof of M4/M5. Do not keep adding synthetic substitute behavior merely to get green tests. Unknown critical dependency/type/initializer prevents advancing that gate. If preserving original behavior requires forbidden legacy runtime or unapproved transformations, report the concrete conflict instead of reducing the objective.

## Logging and behavioral acceptance design (not implemented)

Each future run should identify migration revision, source/reference hashes, selected slice, actual host/architecture, gate, fixed input/tick, last own phase, original-consumption evidence, platform-request outcome and cleanup. Keep parent evidence/assertions distinct from child/OS events, exception records distinct from actual exit codes, bounded timeline and native first/second-chance classification when captured. Missing fields mean unknown, not pass; no raw commercial code/pixels/addresses/dumps/install paths in public summaries.

Behavior validation compares to existing source-derived criteria only after exact inventory. Later authorized independent original comparison may strengthen evidence, but legacy control tests never become a product dependency. Until such evidence exists, report migration behavior as unverified, not exact parity. Original step, animation resolution, render submission, pixel output and human-visible character are separate milestones.

## Review and next reply

Document review only, no cmd/probe necessary. Check: no legacy dependency/helper; original behavior preserved rather than guessed; existing failures/history retained; gates require actual evidence; no silent platform substitute or permission expansion. No tests run (git diff/content review only); no runnable demo because this is a design deliverable. STATUS is the updated text progress panel; the existing generated HTML is not regenerated or presented as new.

Suggested reply: `验收非旧框架迁移方案〈提交〉并上传；授权M1仅只读分析已存在的原版恢复缓存，生成本地依赖/平台调用清单和不含源码的汇总；不修改、编译或执行原版，不读取新依赖或素材，不运行探针、不打开GUI或访问安装目录；遇到范围外问题先停止。`

This does not accept previous standalone diagnostic results as runtime success. No upload/main/accepted change this iteration. Whole GOAL remains incomplete.
