# CDR-082 — Read-only environment feasibility inventory

2026-10-04, Primary owner. User requested the next development step after the bounded inventory gate was explained. Scope: fixed OS/framework registry records, eight Windows/CLR file existence/version slots, self-authored inspector and progress reporting. No original/XNA loading, GUI, game-directory access, system changes, feature enabling, downloads or device/API denial probes. Baseline `705b566`, branch `codex/cdr-082-local-compile-probe`. Full GOAL unchanged.

## Role

Separate an absent OS/runtime prerequisite from unverified effective isolation. This is diagnostic infrastructure, not an implemented sandbox or character runtime. No original behavior source is changed.

## Measured evidence

- Registry OS build 26200, revision 9457, displayVersion 25H2, edition ID CoreCountrySpecific; OS and inspecting process 64-bit; environment-reported native architecture AMD64 (not an independent CPU probe).
- Both Registry32 and Registry64 framework Release values 533509: at least Framework 4.8.1 under the documented release detection method. This is runtime registration, not proof of an exact 4.7.2 runtime or mixed-mode compatibility. [Microsoft version detection](https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed).
- Eight fixed files present: System32/SysWOW64 kernel32.dll and userenv.dll, Framework/Framework64 v4.0.30319 clr.dll and mscorlib.dll. CLR file versions 4.8.9345.0. File-version labels need not equal the OS build; differences alone are not corruption evidence.
- Eight generated release-threshold checks passed without system reads. Actual inventory reads exactly three registry key views and eight file version/existence slots; no export enumeration, target library load or isolation API invocation.

## Feasibility by restriction

| Requirement | Current conclusion | Next evidence |
| --- | --- | --- |
| OS/version baseline | Version-eligible candidate, not effective support proven | Pre-start policy configuration and verification in own-only implementation |
| Framework/x86 runtime basis | Registration and x86 CLR files present | Restricted CLR startup and mixed-mode compatibility, separately authorized |
| Filesystem/registry | Unknown | Explicit effective access policy and generated denial fixtures |
| Network/IPC | Unknown | Capability policy and separately authorized local denial tests |
| GUI/input | Unknown | Pre-load GUI restriction plus separate keyboard/mouse/controller coverage |
| Audio/Steam/devices | Unknown | Identify enforceable routes; no blanket capability/DLL-name claim |
| Process lifetime/children | Unknown for the new sandbox | Owned Job assignment, escape limits and cleanup proof |
| Commercial output | Existing ignore/reporting boundary retained, sandbox enforcement unknown | Narrow input/scratch permissions and sanitized reporting proof |

Recommendation: no detected version/registration gap forces installation or download; continue only to a scoped own-only prototype after authorization. No row certifies the original safe to load. VM/Sandbox optional features, privileges, effective policies, exports and XNA compatibility were not inspected. Do not convert missing evidence into a pass or automatically enable a fallback.

## Deliverables and acceptance

`tools/Inspect-RestrictedEnvironment.ps1` has fixed read-only inputs, no target path arguments, reparse-point rejection, fresh per-run ignored reports and a latest summary for the progress page. Root `检查受限环境可行性.cmd` displays Chinese scope/limits first. It must not be added to ordinary verification because it reads host state; `-SelfTest` checks only generated release thresholds. No full suite or original runtime tests were rerun.

Verification: self-check exit0/8 cases; actual developer cmd exit0/COMPLETED isolationEstablished=false; progress cmd no-open/no-pause exit0. Generated HTML contains inventory-only-enforcement-unknown and both measured Release values; JSON fixed scope/false safety flags/eight unknown restriction rows verified. Root cmd ASCII/CRLF and Git whitespace/scope checked. This validates reporting, not enforcement.

Developer acceptance: run that cmd, expect ENVIRONMENT_INVENTORY_COMPLETED and COMPLETED isolationEstablished=false, then regenerate `演示当前进度.cmd` to see the environment summary. Missing files should be reported as missing, never repaired or installed. Summary location: artifacts/cdr-082-environment/summary.json; individual records retained alongside it. Original inputs/behavior/assets are never accessed by this tool.

## Next gate

Separately authorize a precisely scoped own-only containment prototype: process creation with pre-start GUI restrictions, owned Job/resource/lifetime settings and generated status/cleanup tests only. No original/XNA/Steam loading, live input/audio/network/device calls, GUI, profile/ACL/system changes, downloads or installation access. If configuration itself needs wider access or might create GUI, stop. This narrow prototype would NOT establish the entire sandbox; AppContainer profile/ACL enforcement and device denial would still require their own approval and evidence. Inventory acceptance is not target-execution authorization. Local-only, pending acceptance/upload.
