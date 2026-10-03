# CDR-082 — Own synthetic test process monitoring

2026-10-03. Primary. Existing isolation-adapter design scope; own fixed console children only, no recovered/XNA execution/source edits/assets/game directory/GUI. Pending acceptance/upload.

## Role in the project

Prepares reliable reporting for a later separately permitted test that may exit before managed exception handling. Parent distinguishes completion, early exit, missing completion, invalid protocol, oversized output and timeout. NOT an original runtime monitor, OS sandbox, native containment or behavior proof.

## Reviewed boundary and evidence

Fixed own DLL/apphost; no arbitrary executable/assembly argument or shell, no visible console/input/target load. Known startup/profiler hook variables removed from child, runtime diagnostics disabled; no claim that this prevents filesystem/network/device access. Generated started/context-ready/completed phases only: context-ready is NOT real original input/actor initialization. Parent/child reject observed original/XNA assembly loads.

Fresh run ID/exact phase sequence+shape,1024characters per message,2048 retained per stdout/stderr; overflow drains/discards instead of blocking.3second observation after spawn,5second bounded cleanup/drain waits. Only the Process instance started by this call can be terminated, not arbitrary PIDs. Records own PID/exit/actual monotonic duration/last validated phase/message count/output budget/termination/exited. Exceptions include stage/type/message/HResult/stack/bounded inner/path redaction; no raw child payload.

| Own generated case | Expected outcome | Last validated stage |
| --- | --- | --- |
| Complete | completed / exit0 | completed |
| EarlyExit | unexpected-exit / exit23 | started |
| MissingComplete | incomplete / exit0 | context-ready |
| Malformed | protocol-rejected | started |
| WrongRunId | protocol-rejected | started |
| FloodStdout | output-budget-rejected | started |
| FloodStderr | output-budget-rejected | started |
| Hang | timeout / owned child terminated | context-ready |

EarlyExit is own Environment.Exit, not actual native failure; Hang is own delay. Expected negative outcomes count as monitor verification, not successful runtime work.

## Acceptance and next function

Double-click 验证测试进程监控.cmd:8rows and PROCESS_GUARD_VERIFIED passed=8 failed=0 allOwnedChildrenExited=true originalCodeExecuted=false processSandboxEstablished=false, exit0. Summary/cases: artifacts/cdr-082-process-guard/summary.json and cases.json. Ordinary 验证当前版本.cmd includes this reviewed suite without target/system/cache reads or windows.

Measured dedicated/full cmd exit0,8own cases, retained317/probe26/closure13/compile-report4/isolation45/isolation-report4/adapter-audit3/framework-report4/XNA13, window probes0. All owned child instances observed exited.

Next original input/time bridge and native initialization gate remain unproven. This monitor deliberately cannot load original code. Do not replace its fixed child without separate boundary review or treat process health/complete messages as evidence of Player/animation/pixels. No automatic acceptance/upload/GUI permission.
