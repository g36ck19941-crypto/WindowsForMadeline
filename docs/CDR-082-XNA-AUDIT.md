# CDR-082 — Approved XNA private cache and static preflight

2026-10-03. Primary. Developer confirmed readonly private caching/audit of3 previously inspected system XNA assemblies. No original/target assembly execution, game/GUI/real input/audio/Steam, installation/assets access or downloads. Acceptance/upload pending.

Later explicit readonly system-dependency grant resolves the inspection-scope gate below; [system findings](CDR-082-SYSTEM-DEPENDENCIES.md). Current cached-boundary classification adds a ninth own check group. Older4/8-check results are historical. Runtime safety remains unproven.

## Latest cached-only boundary classification

Framework/Graphics each retain53managed/9PInvoke/73MemberReference/11indirect counts. All73 member occurrences have mscorlib metadata scope, not73 unknown external libraries. Six of9 PInvoke methods also have Native implementation flags/nonzero local code RVAs, with empty imported-module names. Remaining3 declared imports are kernel32/MSVCR100 pointer encode/decode and encoded-null functions. No target invoked; scope is not resolved behavior.

Earlier nativeMethodBoundaries=0 excluded PInvoke-marked methods counted first. It is NOT no native code on the graph. New overlap counters expose6 native-RVA boundaries; native bodies and11 indirect targets remain unanalyzed. Startup-looking declarations are not device/GUI safety proof.

Raw names/tokens/import details only go to fresh scoped JSON in Git-ignored cache, never stdout/public records. Escaped output, links and existing-file overwrite are rejected. Ninth own lexical-output negative check reads no cache. Double-click 审查缓存XNA依赖.cmd: new XNA_BOUNDARY_CLASSIFICATION rows show6nativeRva/6emptyModule/mscorlib73 for the mixed libraries, Game0/no root (not general safety).

Measured cache cmd exit0/9own groups; full gate exit0, retained317/probe26/closure13/compile-report4/isolation45/isolation-report4/adapter-audit3/framework-report4/XNA9/window0. No system rereads/new dependencies/native or original execution/Player/assets. Diagnostics only, pending acceptance/upload. Next relevant gate: internal native/indirect initializer behavior, not repeating dependency searches.

## Role and actual evidence

Fixed GAC3 candidates copied into Git-ignored local-cache/cdr-082-xna, original hash checked before/after source and destination, PE identity version4.0.0.0/token842cf8be1de50553 matched. No other runtime dependency copied/read. Public scripts/tools/report contain no original code or DLL bytes.

Cache absence is resolved, but runtime safety is NOT established:

| Cached assembly | IL-only | Module initializer | Native methods | PInvoke methods |
| --- | --- | --- | --- | --- |
| Framework | false | present | 102 | 122 |
| Game | true | absent | 0 | 11 |
| Graphics | false | present | 36 | 39 |

Framework and Graphics are mixed managed/native modules, not purely managed value-type libraries. Metadata includes MSVCR100/kernel32/user32 and (Framework) xinput1_3 imports. Game declares System.Windows.Forms/System.Drawing/GamerServices/Input.Touch references; Framework/Graphics declare Microsoft.VisualC10.0.0.0. These files were not newly inspected or resolved. Imports/reference presence does not establish actual invocation on the intended input slice, but module/native initialization cannot be assumed safe by avoiding explicit device polling.

Actual target loading/initializer/native side effects and dependency closure remain unreviewed. Under the human stop-on-doubt condition, original input/time smoke remains unexecuted. This is neither an original-input pass nor a failed runtime attempt.

### Cached initialization graph follow-up

Without additional dependency reads, the own auditor now conservatively traverses local IL method tokens from each module initializer. Framework and Graphics each have 1 root, 53 visited managed methods, 9 PInvoke method boundaries, 73 MemberReference occurrences and 11 indirect-call occurrences; native-method boundaries and unresolved bodies are 0. Game has no module-initializer root, so this traversal is empty, not a general safety claim.

All branches and local function-pointer references are included without feasibility analysis. MemberReference tokens and indirect/native targets are not resolved; the73 occurrences are not necessarily73 distinct external methods. Implicit type initialization and native loader effects are not covered. Counts are potential boundaries, not observed invocations. In particular, zero native-method boundaries does NOT mean no native initialization: PInvoke and indirect boundaries remain. This narrows the gate to initialization-related targets but does not establish runtime safety.

## Tools and acceptance

Cache-ApprovedXna.ps1 fixed3 file/hash/identity guard, own PE-only XnaPreflight auditor and dedicated 审查缓存XNA依赖.cmd. Auditor bounds file/metadata/IL graph sizes, rejects cache escape/links, reads no referenced assembly, does not Assembly.Load/execute target. Four self checks use only own tool metadata/scope/load absence and a throwing initializer inspected without invocation, not the original library. Ordinary verification runs these own checks only; no XNA cache/system reads without opt-in.

Double-click 审查缓存XNA依赖.cmd: expect3 metadata lines matching table and3 XNA_INITIALIZER_GRAPH lines matching the follow-up, XNA_STATIC_AUDIT_COMPLETED runtimeSafetyEstablished=false originalTestExecuted=false, exit0. Static audit completed is NOT runtime verified. Safe aggregate artifacts/cdr-082-xna-cache/audit.json; private binaries and PE details remain ignored. 验证当前版本.cmd preserves prior gate plus4 owned metadata checks, window probes0. No character demo added.

## Next boundary

Measured after graph update: dedicated cached audit exit0, self-checks4/4; full no-window gate exit0, retained317/317, synthetic probe26, closure13, compile-report4, isolation45, isolation-report4, adapter-audit3, framework-report4, owned-XNA4, window probes0. No target execution or runtime pass. Only own code and source-free records are eligible for the local commit; cache/binaries remain ignored.

Continue only after clarifying scope for readonly inspection of the relevant system dependency/native initialization closure, or an alternative explicit runtime plan. Do not load mixed-mode modules or copy/resolve additional dependencies silently. No new dependency install/download/FNA substitution, no source edits or full-game initialization. Additional static review must determine relevant module/load effects before the authorized original input/time test can run; approval to cache3 files is not blanket native or GUI permission.
