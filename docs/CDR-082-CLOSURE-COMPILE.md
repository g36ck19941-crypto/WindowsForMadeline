# CDR-082 — Existing XNA/net472 original-source compilation

2026-10-03. Primary. Explicit developer authorization: already-found XNA and .NET Framework 4.7.2 references, local ignored original-source dependency completion and compilation only; no downloads/install/execution. Tooling verified, original-core compilation still partial. No acceptance/upload.

## Role and observed outcome

Own CompileClosure uses installed SDK Roslyn libraries to parse the recovered cache, index declared types and conservatively follow identifier references from original Player/Actor/Solid. It emits Library/x86 with seven hash-validated, previously inspected references. No recovered project/build hooks/resources, analyzers, invented stubs or original-source edits. Closure is conservative, not a minimality/semantic-isolation proof. Engine/startup dependencies may be included as source, never executed.

918 available source files; 795 selected. Two repeated attempts had identical selected-source digest 89EF35562F630A23B2663E15C96678F1326521AD43B0A2D23156029E1A5FFC51. XNA missing-namespace errors eliminated; current emission fails with CS0103=13 and CS0246=5 (18 errors). This is not a successful compiled core. Compared with previous seed probe: 15 selected, 366 deduplicated error locations; the probes differ in selection/references/framework, so this is progress, not an isolated benchmark.

16 errors mention Steamworks/Steam identifiers; remaining two identify AppId_t. Original candidate previously referenced Steamworks.NET version10.0.0.0 in orig. No Steamworks metadata reference added because the new permission only names XNA/framework references. Do not substitute guessed interop, use modded root DLL, or call Steam APIs.

net472 references differ from original net45 target. Hash/identity agreement and reduced errors do not prove runtime/API/native readiness. No generated successful commercial assembly; failed output removed only at its exact run-contained file path. All recovered selections and detailed logical-file/line diagnostics stay in ignored local-cache, not public logs/reports.

## Verification and manual acceptance

Current ordinary gate: 317 retained cases, 26 probe checks, 10 generated closure/emit checks, four compile-summary checks; all no-window. Generated checks include dependency cycles/unrelated exclusion, compile-only positive/negative emission and cache boundary. Own Release tool builds zero warnings/errors; separate from failed original compilation.

Double-click 验证当前版本.cmd; expect closureChecks=10 and windowProbesExecuted=0. Optional opt-in 使用已有引用检查原版编译.cmd repeats only this authorized cached-source/seven-reference compilation (not recovery/game access), returns2/COMPILE_BLOCKED while dependency absent. It never runs outputs. Default progress cmd opens an HTML explanation page; agents use CDR_DEMO_NO_OPEN=1/CDR_NO_PAUSE=1 instead. New panel describes795/18 and is not character animation.

No new installation read/write, game/GUI/input/commercial execution or dependency downloads. Only own code/tests/aggregate records tracked; source/IL/resources/builds/private diagnostics ignored. Bilingual reports/evidence retain failed outcome.

## Next gate

Seek explicit permission to read-only reference the previously inspected orig/Steamworks.NET.dll version10.0.0.0 as compiler metadata, with hash/identity check, and continue local compile-only attempts. No Steamworks API/native loading or Steam/game startup. Additional dependencies/errors are not presumed fixed by this one reference. CDR-083 stepping and product runtime still unauthorized. Tooling acceptance/upload remains separate.
