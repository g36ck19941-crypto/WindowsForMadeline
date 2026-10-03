# Current Handoff

Updated: 2026-10-03. Owner: Primary. Local branch: codex/cdr-081-assembly-identity.

## Objective and verified publication

Thread goal is active and follows docs/GOAL.md. Original-code-led local reconstruction replaces the retired self-designed gameplay route. Player, Simulation.Core, eight Entity modules and App were deleted; Git history remains recoverable at local archive/self-designed-logic-before-removal-20261003 / e0ba567529dfc014bdeb15c5b46cbb306f045fd7.

Developer accepted CDR-080 and explicitly authorized CDR-081 read-only identity/dependency inspection of the previously selected legitimate installation. Accepted CDR-080 passed a fresh 298/298 gate and outbound path audit, then was published/read back as f804168ff2655c043d60b19749151ecc6181dc03 at codex/cdr-080-original-recomposition. Remote main remains d237277e10af090cf60ec015c22174565e6cdc0a. Current CDR-081 is not accepted or uploaded.

## CDR-081 measured state

Self-authored tools/CelesteDesktop.AssemblyInventory statically reads PE/CLR metadata/hashes from fixed root/orig slots and validated direct DLL reference names. No Assembly.Load, original IL extraction/execution, native loading, game/GUI/input or installation writes. Reports hold identities/counts/fixed type presence/dependency facts only, not commercial bytes or real installation paths.

Root Celeste.dll matches the old cached SHA and is mod-bearing/.NET 8 (263 Mod-type indicators). orig/Celeste.exe is a no-tested-Mod-marker/.NET Framework 4.5 candidate, hash 1A1E117ADD967C0F26AD470A49D4FF442435209265BF1FDDA623821D797E80B5; no official baseline authenticates it. Relevant qualified types are present; Actor is Celeste.Actor. Three XNA references are unresolved within checked root/orig, not asserted missing system-wide. Combined graph: 24 nodes, 230 reference edges; all 24 managed hashes stable on repeated read. Framework/global locations and native ABI readiness are uninspected.

35 focused synthetic cases, 333 total cases across 13 suites, two generated junction rejection probes and three report probes pass; Release has zero warnings/errors. Root progress/verification cmd entries passed with no-open/no-pause; inspection cmd passed cancel and authorized real input, including Windows PowerShell 5. No real GUI was opened. Initial sandbox DLL denial was resolved using narrowly authorized read-only metadata escalation, not game execution.

Current progress report is summary-only and does not re-read installation. It consumes a dated local report if available; absent reports are uninspected. No runnable original character exists. docs/evidence/CDR-081.json is a commercial-payload-free summary; raw reports and old recovered source stay ignored. See docs/ASSEMBLY_IDENTITY.md and bilingual CDR-081 acceptance/update records.

## Next gate and constraints

Await developer acceptance of CDR-081 and source choice/explicit CDR-082 permission to recover/decompile the inspected original candidate and attempt a minimal local compile. Do not perform that under the narrower identity-read authorization. Do not choose modded source silently or replace missing engine/gameplay logic with guessed controllers. Recovered source/IL/assemblies/assets and commercial-derived builds stay ignored/local; never GitHub or public packages.

No full-game boot, original code execution, GUI/live input, install writes, new dependency download or unapproved external reference search. CDR-075 remains unaccepted/unuploaded and retired; CDR-060 historical acceptance pending and CDR-061 unauthorized, not active gameplay continuations. Do not self-accept or update remote main.

Offline gate: tools/Verify-CDR081.ps1 or 验证当前版本.cmd; CDR_DEMO_NO_OPEN=1 and CDR_NO_PAUSE=1 for automatic entry checks. Inspection is opt-in via 检查原版来源与依赖.cmd; no installation path is stored. Context coherent; if coherence degrades stop and create a full next-window prompt.
