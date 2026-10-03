# CDR-082 — Original-source library emission established

2026-10-03. Primary. Tooling/local compilation verified, developer acceptance pending, not uploaded. Minimal isolated behavior core and runtime integration remain unestablished.

Developer entry probes passed: original-cache compilation exit0, empty-input cancellation exit0, progress generation with browser opening disabled. Replaced the wrapper's Get-FileHash dependency with framework SHA256 streaming to support Windows PowerShell child environments without that module; no module downloads.

## Role and actual change

Explicit permission added the previously inspected orig/Steamworks.NET.dll as read-only compiler metadata, not runtime code. Before use require SHA-256 6C6B307E907294003014DA3ED4610E362A9B6EE4093A20E514B0E23454DA3085 and PE metadata identity Steamworks.NET10.0.0.0/neutral/unsigned. No Steam calls/loading/native library execution/download/install.

Using eight original-compatible reference identities (three XNA, four net472 BCL, one original Steamworks) and795 original recovered source files, Library/x86 emission succeeds with zero errors. No original edits, invented stubs, generated project/resources/hooks or analyzers. Original target net45 versus current net472 references remains experimental.

Static PE audit only: Player/Actor/Solid present;1614 type definitions,8 assembly references,0 embedded resources. Output SHA-2564954B40E17DD4886DEEBFB42CE1EB9723DC5E3C329C7D08605069AF06F980467 repeat-stable across measured emissions. Selected source digest unchanged89EF35562F630A23B2663E15C96678F1326521AD43B0A2D23156029E1A5FFC51. The output and all commercial source/IL/diagnostics remain ignored local-cache, never GitHub.

No Steamworks/Celeste/output assembly loaded into tool AppDomain; no recovered code execution, game/GUI/input, installation writes or dependencies downloaded. Only exact authorized orig Steamworks file read from selected installation; no re-decompile or other game installation reads. Compiler metadata file hashes validated before/after emission.

## What this does NOT establish

795-file conservative closure includes broad original engine/game dependencies, not a proven minimal isolated core. Compilation does not prove original numeric parity after decompilation, net45/net472 API/runtime/native compatibility, safe constructor/update isolation, resource readiness, character creation/animation/collision or human visibility. No commercially derived DLL shipped publicly.

## Verification and manual acceptance

Ordinary no-window gate:317 retained cases,26 probe checks,13 generated closure checks (including wrong Steam baseline rejection and compile-time initializer trap without execution),4 compile-summary checks; ancillary junction/report checks remain. Own tool build0 warnings/errors, distinct from original emission zero errors.

Double-click 验证当前版本.cmd: expect closureChecks=13 and windowProbesExecuted=0. Does not read install or compile commercial code.

Optional 使用已有引用检查原版编译.cmd asks the previously selected installation folder, reads only orig/Steamworks.NET.dll and already-authorized system refs, compiles cached sources only. Expect selected795/emittedTrue/COMPILE_EMIT_SUCCEEDED/exit0; cancel0, environment/baseline rejection3. Generated result remains ignored and is not run. Do not send DLL/source/cache.

For no-GUI progress, set CDR_DEMO_NO_OPEN=1/CDR_NO_PAUSE=1 before progress cmd; page separates current emission success from first15-file failed probe. Read artifacts/cdr-082-closure-real/summary.json:emitSucceeded=true,errorCounts empty,recoveredCodeExecuted=false,runtimeIntegrated=false.

## Next functionality and permission

First static CDR-083 preflight: map original startup/update/resource/platform/native call boundaries and plan a minimal isolated execution harness without launching the game. Any actual execution of recovered code, offscreen stepping/rendering, real assets/native library initialization or GUI needs separate explicit scope. Do not infer execution from compile permission or use original full-game entrypoint. CDR-082 acceptance/upload requires developer confirmation; main unchanged.
