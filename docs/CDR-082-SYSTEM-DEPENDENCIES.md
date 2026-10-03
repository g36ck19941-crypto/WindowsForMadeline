# CDR-082 — Readonly XNA system dependency inspection

2026-10-03. Primary. Explicit human permission: inspect XNA-initialization-related system dependencies readonly; no execution, downloads, installation or game-directory access. Own tools and source-free aggregate evidence only. Acceptance/upload pending.

## Role in the project

Separates missing runtime components from unresolved initialization behavior. Finding the parts does not prove that loading them is safe or that original input/Player works. No new gameplay/runtime functionality is delivered.

## Fixed scope and measured findings

Eight managed slots in the modern Windows GAC and four x86 native slots in SysWOW64 were inspected using bounded PE/CLR metadata/import readers, with SHA256 checked before/after. All12 present; all hashes stable. No DLL copied, Assembly.Load/LoadLibrary/target execution, dependency recursion, asset or installation access. Managed identities observed match the declared names/versions/tokens of these slots; this is not binary provenance or runtime/API compatibility proof.

| Managed dependency | Version | Module initializer | PInvoke declarations |
| --- | --- | --- | --- |
| Microsoft.VisualC | 10.0.0.0 | absent | 0 |
| XNA GamerServices | 4.0.0.0 | absent | 7 |
| XNA Input.Touch | 4.0.0.0 | absent | 0 |
| System.Windows.Forms | 4.0.0.0 | absent | 625 |
| System.Drawing | 4.0.0.0 | absent | 692 |
| mscorlib | 4.0.0.0 | absent | 563 |
| System | 4.0.0.0 | absent | 459 |
| System.Core | 4.0.0.0 | absent | 184 |

All eight report ILOnly=true, machine I386; for managed AnyCPU libraries, I386 alone is not proof of x86-only runtime restriction. VisualC/System.Drawing token b03f5f7f11d50a3a; GamerServices/Touch token842cf8be1de50553; remaining framework libraries tokenb77a5c561934e089.

Native msvcr100/xinput1_3/kernel32/user32 files are I386, each with a PE entrypoint and no TLS directory declaration. Kernel32/user32 also declare delayed imports. An absent TLS directory or managed module initializer is NOT a no-side-effect proof: type initializers, native entrypoints, dynamic loading and indirect calls remain outside this inspection. Managed PE entrypoints may be CLR bootstrap declarations, not GUI/game startup evidence.

Declared downstream references/imports were recorded as names only, not opened or resolved. Missing physical files, loader resolution, function exports, initialization effects and actual intended-slice invocation are not established by these declarations. Do not recursively inspect the entire operating system or silently label declarations as runtime failures.

The prior cached XNA initializer graph still has unresolved indirect/PInvoke/member boundaries. This turn resolves the scoped availability question, not the execution-safety question. No original input/time test or Player was executed/created.

## Acceptance and reproduction

Double-click 检查XNA系统依赖.cmd: expect12 SYSTEM_DEPENDENCY rows, status=inspected, final SYSTEM_DEPENDENCY_AUDIT_COMPLETED targetExecuted=false runtimeSafetyEstablished=false, exit0. Source-free aggregate: artifacts/cdr-082-system-dependencies/summary.json. No system/game reads in ordinary 验证当前版本.cmd: it exercises8 own-tool checks only, including normal/delay import fixtures, malformed/unsupported descriptors, scope rejection and own PE inspection, alongside the existing no-window suites.

## Next work after acceptance

Measured verification: system-dependency cmd exit0 (12 inspected rows); full ordinary cmd exit0 with retained317/317, synthetic probe26, closure13, compile-report4, isolation45, isolation-report4, adapter-audit3, framework-report4, owned-XNA8. Window probes0; original/runtime pass not claimed. Local commit only, no remote upload or acceptance-state promotion.

Do not repeat dependency-search permission or assume blanket execution permission. Within the existing cached-file static scope, resolve relevant initializer boundary targets where possible and distinguish mandatory CRT/module initialization from device/graphics polling. Unresolved native/indirect effects still stop the original test. Any proposal to run those effects needs an explicit bounded decision, not automatic loading based on file presence. No new dependency downloads/copies, original source changes, GUI, game or installation access are implied.
