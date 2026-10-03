# CDR-081 acceptance — identity and dependency inspection

State: offline implementation verified; developer acceptance pending; not uploaded. Owner: Primary.

## What changed and its role

A self-authored, read-only metadata tool now distinguishes the modded current executable chain from an original-backup candidate and lists exact/mismatched/missing/framework-uninspected dependencies. It answers which code source reconstruction should use before attempting to assemble a behavior core. This is diagnostic/tooling functionality, not a new playable character.

CDR-080 was accepted by the developer and freshly reverified at 298/298, then published/read back as f804168ff2655c043d60b19749151ecc6181dc03 on codex/cdr-080-original-recomposition. Remote main stayed d237277e10af090cf60ec015c22174565e6cdc0a. CDR-081 changes are on a separate local branch and are not published.

## Verification

Release build: zero warnings/errors. Inventory 35/35 synthetic cases; retained plus new suites total 333/333 across 13 suites. Two generated junction-rejection probes and three progress-report probes additionally pass. Reports with no identity file are explicitly uninspected; unsafe execution-claimed reports are rejected. Real authorized metadata scan inspected four candidate slots and 24 managed graph nodes; a repeated read verified identical hashes for those 24 nodes. Game assembly code was never loaded/executed. Reports exclude source/IL/commercial assets/install paths.

Observed source: root Celeste.dll is mod-bearing/.NET 8 and matches the existing decompilation cache. orig/Celeste.exe is a .NET Framework 4.5 candidate without the tested Mod markers. It contains the relevant types but has three XNA references missing from the checked root/orig directories. Framework/global locations and native ABI compatibility were not inspected; this is not a claim that XNA is absent from the entire computer. No official baseline authenticates the candidate.

## Concrete acceptance

1. Double-click 演示当前进度.cmd: see dated source inspection rows and clear statements that no runnable character exists.
2. Double-click 验证当前版本.cmd: expect CDR081_VERIFIED suites=13 passed=333 junctionChecks=2 reportChecks=3. No installation input/read or visible GUI.
3. Optional authorized repeat: double-click 检查原版来源与依赖.cmd, supply the same legitimate installation, then review progress again. It does not open a GUI, decompile source or start the game. Cancel with empty input; failures include phase/code and redacted details.

Both current progress/verification launchers are tested with no-open/no-pause settings. The inventory launcher is tested with generated and authorized inputs separately; Chinese output/CRLF is preserved.

## Next feature after acceptance

Proposed CDR-082: explicitly choose orig/Celeste.exe as the inspected-but-not-officially-authenticated source; authorize local-only recovery/decompilation and minimal compilation feasibility, with original code/binaries/derived builds ignored and unuploaded. First surface framework/XNA compatibility constraints rather than silently replacing them. No recovered-code execution, GUI, live input, installation writes or dependency download is implicitly authorized.
