# CDR-082 M1 — Cached migration dependency inventory

2026-10-06. Primary, baseline360b47c, codex/cdr-082-local-compile-probe. Human accepted migration design and explicitly authorized read-only existing original recovery-cache analysis; no original edits/compilation/execution, new dependency/assets, probes, GUI or installation access. Own syntax analyzer and aggregate-report generator only.

## Role and implementation

Preparation for modern migration: identify possible type/file dependencies, platform call sites and initialization sites before selecting precise interfaces. This is not gameplay implementation, a runtime probe, compiler compatibility proof or a minimal executable slice.

Added a return-before-compilation syntax-only branch to the existing self-authored CompileClosure tool. It uses already available own SDK/Roslyn parser references, not original/XNA/Steam metadata. Only .cs text in the single918-file cache identified by prior original-assembly hash is read. Reparse paths, budgets, ambiguous/missing baseline, changed input hashes and reused/private-output escape are rejected. No emit, assembly loading or target invocation is used. The own tool is compiled, not recovered code. No generated recovered project/hooks/analyzers run.

Per-file private records retain SHA, type/delegate/base/use/alias declarations, identifier candidates, local call/construction token names with owner/member/line/context, explicit static constructor and DllImport attribute sites. File edges expand all simple identifier tokens including generic/delegate references; ambiguous names deliberately overinclude. Calls are syntactic occurrences, not bound targets. Full commercial-derived details remain only in Git-ignored local-cache/cdr-082-migration-inventory; private report has a32MiB cap. Public aggregate has no source text, names/locations or installation paths. Re-run creates a fresh private directory.

## Measured result and limits

-918 files,3,483,880 input bytes,0 syntax errors. All hashes match before/after. Full digest51371D343BCA79992951E0A64ED6A6072FD55FA06698B98A09975BE9AD79D7E4. The selected core digest equals prior closure89EF35562F630A23B2663E15C96678F1326521AD43B0A2D23156029E1A5FFC51; this links the selected file set to earlier evidence, not authenticity certification.
-1201 type/delegate declaration occurrences,42 simple names in multiple files,12816 file candidate edges. Core3seeds ->795 files; input/environment8seeds ->795. These broad sets demonstrate this conservative method does not isolate the minimal method slice.795 here is candidate expansion, not a migration requirement or modern compilation result.
-562 files declare XNA namespaces. Across ALL918files, platform token candidate occurrences: input-device122, audio919, Steam13, file64, graphics/window455, process/thread5, numeric-type3239. These include construction/invocation syntax nodes and are not unique APIs or actual calls. Categories may overlap and cannot identify precise namespace/overload or alias/dynamic/indirect dispatch; false positives and false negatives remain.
-220 call/construction occurrences syntactically under static-field/constructor contexts; explicit static constructors0, DllImport attributes489. Zero explicit constructors is NOT absence of implicit .cctor/type/native initialization; delayed lambdas may also appear syntactically inside initializers. Attributes are declarations, not native calls.26157 call/construction occurrences remain unclassified by the narrow platform-name rules.

No semantic binding, complete transitive call graph, feasibility, original behavior execution or minimality established. Parser success does not mean compiler success. Existing sources untouched; dependency signatures/native initialization remain future work. No alternative library/runtime selected. Legacy framework remains tests-only, never product/helper dependency.

## Practical next action

M2 should select specific input/time initialization and update methods plus necessary types, use the private locations to distinguish behavior to preserve from platform interfaces to redirect, and explicitly list unresolved initialization/dynamic/alias/member cases. Do not blindly migrate795 files or resolve compilation errors with invented stubs. Exact method-level/source assessment can remain existing-cache/static; new dependency reads, source transformation, compilation and execution need separate authority. Role/resource/desktop integration remains later gates.

## Verification and developer acceptance

Root `检查迁移依赖清单.cmd` exit0: sourceFiles918/core795/input795/syntaxErrors0/hashesStabletrue. Built-in own syntax assertions10 (classifier, unknown category, static constructor/delegate/generic recognition and cyclic closure) pass; they parse self-authored strings, start no child/probe. Two path/missing-cache negative inputs exit3 before target parsing. Own tool and summary generator build zero warnings/errors. Summary display10 cases: actual, missing and8 unsafe/inconsistent/oversize/outside negative inputs; no-open current page shows migration-inventory-v1. Existing report-check latest marker updated but its separate old framework fixture suite was NOT rerun.

No Full gate this turn: its existing ProcessGuard cases start probes, excluded by this permission. No claim of a new full regression pass; prior results remain history. No game/install/dependency/native/window/input/audio access, no OS/ACL changes or downloads. Ordinary gate does not start real-cache M1 automatically.

Review the root cmd explanation and aggregate result; read current progress latest section without mistaking it for a character demo. Do not send private details/source/DLLs. Private latest report from final entry: local-cache/cdr-082-migration-inventory/f7d394ce8d6c4a92bb3c7e48c9aa319b/details-local-only.json (Git ignored); public summary artifacts/cdr-082-migration-inventory/summary.json. The page reads aggregates only and does not trigger analysis.

Suggested next reply: `验收M1迁移清单〈提交〉；授权M2仅在现有恢复缓存和清单上做方法级只读分析与适配设计，优先输入/时间和初始化边界；不修改、编译或执行原版，不读新依赖/素材、不运行探针、不打开GUI或访问安装；遇到范围外问题先停止。` Under AGENTS, accepting the new commit also authorizes its later audited independent-branch upload, not main or runtime execution.

## Accepted version upload status

Follow-up2026-10-06: the already-authorized exact360b47c push succeeded and was read back as360b47c195d4473751a2318bee1685ec3374c877 on codex/cdr-082-local-compile-probe. Main remained d237277e10af090cf60ec015c22174565e6cdc0a. The network-blocked status described below is historical/resolved for this design commit. M1implementation5b0e371 was not pushed/accepted; no runtime or M2permission follows from publication. This follow-up only updates records, no new analysis/test execution.

360b47c outbound tree audit passed (no cache/artifacts/binaries/assets/install paths/secrets). Default Git network failed name-resolution worker; elevated remote query and one later accepted-only push each failed connecting github.com:443. No successful push/readback of360b47c. GitHub connector independently read the repository/known prior commit successfully: connector reachability does not prove Git upload. No alternate commit history or API snapshot was substituted for the requested commit. Main and remote ref were not mutated by these failed commands. Accepted360b47c remains pending upload; M1new version pending acceptance. No automatic accepted flag/GOAL completion.
