# CDR-082 — Local original recovery and compile feasibility

2026-10-03, Primary. Tooling/offline verified, developer acceptance pending; original-core compilation partial/blocked. Local branch codex/cdr-082-local-compile-probe. Not uploaded.

## Role and outcome

Recover original local code and expose dependency gaps without inventing replacement gameplay. This is tooling/diagnostic progress, not a new playable character or independent original core.

CDR-081 was accepted and published/read back at 35015d8b50632483ea24ee75908cce5ecaac69ee on codex/cdr-081-assembly-identity; main unchanged.

Previously authorized original candidate orig/Celeste.exe SHA-256 1A1E117ADD967C0F26AD470A49D4FF442435209265BF1FDDA623821D797E80B5 matched before/after recovery. Candidate not officially authenticated. Existing ILSpy 11.1.0.9782 recovered 918 C# files under ignored local-cache/cdr-082, separate from old modded cache; no tool downloads.

Own compile-only SDK Library project selected 15 original Player/Actor/Solid/Monocle seeds. No generated project/resources/build events or original-code execution; no external packages, directory build imports or analyzers. Experimental net45-to-net8 retargeting, not validated compatibility. Restore 0, build 1; deduplicated compiler locations CS0234=11, CS0246=355. No supplied XNA references/FNA substitution. Seed dependencies are incomplete, so failures do not establish a broken installation.

## No-window repair and verification

The initial retained gate executed its legacy hidden-window rendering test, contrary to current no-GUI scope; the issue was disclosed and execution paused. Developer authorized gate repair. The repaired gate uses 12 reviewed suites: 313/313 cases. Entire Rendering.Tests suite (20 cases) excluded, not reported as passes; no Desktop --real-readonly selection. Solution compilation is not test/runtime execution.

26 synthetic probe checks, four compile-summary checks, two junction guards, three identity-summary probes pass. Windows PowerShell verification cmd exit 0; no-open progress, recovery cancel and generated missing-candidate cmd probes pass. Release build 0 warnings/errors. Chinese instructions are UTF8 text loaded by ASCII/CRLF cmds; PS5-compatible absolute-path guard. Repair did not re-read installation or rerun recovery; no game/GUI/input/recovered execution, installation writes or downloads.

## Manual acceptance

1. Double-click 验证当前版本.cmd. Expect CDR082_VERIFIED: retainedCases=313, syntheticProbeChecks=26, compileReportChecks=4, windowProbesExecuted=0, then OFFLINE_VERIFIED. This does not compile/run commercial code or require an installation.
2. For strictly no-GUI summary generation, set CDR_DEMO_NO_OPEN=1 and CDR_NO_PAUSE=1 before invoking 演示当前进度.cmd. Review the Chinese acceptance document and source-free artifacts/cdr-082-real/summary.json: 918 recovered, 15 selected, compileEstablished=false, recoveredCodeExecuted=false. Default double-click opens an HTML explanation page only; agents must not do so under current no-GUI permission.
3. 本地恢复并检查编译.cmd is explicit opt-in, not ordinary verification. It requests the previously selected installation, validates exact source hash and pinned cached tool, writes recovery/build only into ignored local-cache. It may repeat the known blocked compile (exit 2); cancel is 0, setup/identity failure 3. Never runs outputs. Missing cached tool stops without download; an explicit existing pinned path can be supplied through CDR_ILSPY_DLL in sandbox environments.

Never share source/assets/local-cache. Evidence docs/evidence/CDR-082.json contains counts/codes/safety flags only. No automatic acceptance, CDR-082 upload or main update.

## Next functionality and gate

Determine existing XNA/.NET Framework reference availability under a newly agreed read-only search scope, or separately authorize evaluating existing FNA compatibility. No silent substitution/system-wide search/new dependency download. Actual dependency-closed compilation remains incomplete; no CDR-083 resource stepping, original code execution or visible runtime authorized.
