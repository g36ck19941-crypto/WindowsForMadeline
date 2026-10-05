# CDR-082 — Own restricted process prototype

## 2026-10-04 startup diagnostic follow-up (baseline c2496e2)

Same controls, no runtime substitution. Four fixed own phase files (512B bound, exact run/scenario/stage, contiguous sequence) record entry-file-written, assembly-check-completed, GUI-query-returned and Job-verified before ready. First marker avoids JSON/reflection/native calls, but still needs managed entry/path/file operations: absence does not prove Main never executed. Eight generated protocol checks cover absent, all four, first only, gap, wrong ID/mode/stage and over-budget.

Existing own debug events now retain up to128 distinct ASCII module basenames, not paths/addresses/memory bytes. Actual failed Complete attempt:21 image names including coreclr.dll, no first marker; unchanged C0000409/A, no ready, owned process exited, Job active0. Loading user32/gdi32/win32u libraries is not proof of GUI creation or API invocation. We have narrowed observed startup state, not established the failing call or root cause.

Reverified native6 and phase8; progress report checks now9 (adds invalid phase, image budget and contradictory image count). Root entry remains PARTIAL/exit2. New summary fields startupDiagnosticsVersion/phaseProtocolPassed/lastOwnPhase/observedCoreClrImage/loadedImageCount are validated before display. No original/XNA/Steam, GUI/device/input/audio/network calls added, no game/system configuration/ACL/download/install access.

Acceptance: run the same dedicated entry and look for RESTRICTED_STARTUP_DIAGNOSTICS with phase8, empty lastOwnPhase and coreClrImageObserved=True; regenerate progress page to see this limitation. Do not treat the8/6/9 own check counts as a managed-runtime pass. Proposed next decision is whether to verify a separately scoped own legacy-framework host under unchanged controls; current evidence cannot justify silently changing runtime or relaxing policy. No original load or upload.

2026-10-04. Primary owner. Baseline 0bf22bd, branch codex/cdr-082-local-compile-probe. **Partial: native control cases verified, own .NET 8 readiness blocked.** Developer authorization covers only own pre-start GUI policy, Job resources/lifetime and cleanup. No original/XNA/Steam, GUI creation, live input/audio/network/device calls, global configuration/ACL changes, installation access/download/install. Full GOAL unchanged.

## Role

Apply and check controls before resuming a child, then prove exit/cleanup. Unlike the earlier process monitor, this changes the new child's OS policy and Job membership. It is still not a complete sandbox, original runtime or Player feature.

## Implementation

Fixed own targets only, no arbitrary executable arguments. CreateProcess uses no console/window, no handle inheritance, Unicode allowlisted environment, suspended primary thread and a creation-time Win32k disable policy. Job settings queried back before resume: kill-on-last-handle-close, one active process, 512MiB total commit, CPU hard cap20%; breakaway not enabled. Query actual GUI policy (deny bit set, audit-only bit absent) and exact Job membership before resume; child independently checks deny/job flags before its ready record. No GUI-creation negative probe was attempted. [Microsoft process attributes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute), [Job resource structure](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information).

Owned-handle cleanup only, 3s observation/5s exit confirmation, bounded 2KiB atomic local report protocol, no shell or arbitrary child. Fresh ignored run directories retain reports. Debug events collected only for the process created here; exception code/parameter0 and image names recorded. Allocation metadata queries are confined to the owned child, no memory bytes, dumps, input/window content or unrelated process inspection. Debug image handles closed; OS owns debug thread/process handles on continued exit. [Microsoft debug event lifetime](https://learn.microsoft.com/en-us/windows/win32/api/debugapi/nf-debugapi-waitfordebugevent).

## Actual verification and failure isolation

1. Own net8 parent compiled with zero warnings/errors. The own net8 child exited before ready with C0000409; debug events show parameter0=A, exception emission image ntdll.dll, guard target unknown. A=10 is FAST_FAIL_GUARD_ICALL_CHECK_FAILURE in the [Microsoft SDK header](https://github.com/microsoft/win32metadata/blob/main/generation/WinSDK/RecompiledIdlHeaders/um/winnt.h). This identifies an indirect-call guard failure class, not the triggering call, root cause, a buffer overwrite, an ntdll defect or an XNA failure. Do not disable CFG, Win32k policy or system protection to force it through.
2. A separately selected self-authored CRT-free native control probe was compiled using already-installed MSVC14.39 tools, no downloads. Static import audit: only kernel32.dll, eleven allowed process/file/timing APIs; fixed hash checked before and during child creation. This probe is diagnostic, not a replacement for the original CLR/behavior runtime. It does not silently replace the failed net8 test.
3. Native probe six cases pass: BeforeResumeAbort, Complete, EarlyExit23, Hang3s, CloseJobAfterReady, ParentFailureAfterReady. Created children observed exited. Queryable Jobs report zero active processes; after last Job-handle close, accounting is null because that handle cannot be queried. Exit code0 in that case alone is not success: ready-without-complete plus closure reason and process exit prove this case. Parent-failure case is an injected managed exception, not an actual parent crash.
4. Separate managed attempt: pre-resume abort passes, Complete fails before ready; remaining four managed cases not run. Earlier uninstrumented and debug-instrumented managed starts both failed under this policy/environment/resource combination. No assertion that Win32k alone is the cause or that all net8 machines are incompatible.
5. Root entry reports PARTIAL and exit2. Six native control passes do not turn the managed failure green. Resources were configured/queried, not stress-tested to their limits. No original/framework472/XNA compatibility or AppContainer/file/network/device enforcement established.

## Deliverables and acceptance

Own source Native.cs/Program.cs/NativeChild.c; explicit Verify-RestrictedProcess.ps1; developer root 验证受限进程原型.cmd and Chinese explanation; progress page summary; ignored summary.json/cases.json/managed-startup.json/imports.txt and own binaries under artifacts/cdr-082-restricted-process. No commercial content.

Developer acceptance: run that cmd, see six named control cases plus managed startup result. Current expected outcome is PARTIAL/exit2 and the startup code/subcode, not a fully passed prototype. Regenerate 演示当前进度.cmd and confirm native6 and managed failure are separate, no original/sandbox claim. This entry is explicit-only and not added to the ordinary gate. Existing offline regression is separate from this partial outcome.

Verification evidence: explicit prototype entry exit2 as expected, no-open progress exit0; six report checks passed (actual partial, absent report, unsafe original flag, contradictory outcome, oversized report, outside-artifacts input). Existing no-window gate exit0:317 retained cases, XNA own13, process-monitor8, isolation45, probe26, closure13 and summary/audit checks; Rendering20 excluded, not passed. Own native imports/hash/ASCII-CRLF entry and tracked scope checked. Regression does not establish that the new managed startup works.

## Next work / remaining gate

Latest follow-up regression: Verify-CDR082 exit0 (317 retained, XNA own13, old process8, isolation45, synthetic probe26, closure13 and existing report checks; window0/Rendering20excluded). Dedicated new prototype exit2, phase8/native6, all created children exited; display9 and no-open entry0. One overlapping own build caused transient copy retries; sequential page generation repeated separately. Regression remains separate from failed managed startup.

Continue diagnosing the own managed startup while retaining current controls; do not progress into original loading. Current native code/image attribution does not locate the failing call. Any additional dependency, runtime substitution, OS/ACL change, device or GUI attempt needs review/authority before action; no download or automatic weaker-profile fallback. A later own net472/native host must have separately recorded compatibility evidence, never inferred from the kernel32 control probe. Acceptance/upload pending; no remote changes.
