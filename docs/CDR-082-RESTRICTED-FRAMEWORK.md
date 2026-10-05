# CDR-082 — Own legacy-framework startup probe

2026-10-05. Primary. Baseline `1935cc3`, branch `codex/cdr-082-local-compile-probe`. This iteration is pending review, not original-runtime acceptance.

## Role and implementation

Startup-environment diagnostics, not gameplay: compile a small self-authored net472/x86 executable against the previously checked reference, then inspect its PE/CLR metadata before using the existing restricted parent. The same creation-time Win32k denial, suspended-start policy/Job queries, single-process limit, 512MiB commit limit, CPU hard cap20%, timeouts and owned cleanup remain unchanged. No fallback or policy relaxation.

The probe distinguishes environmental startup trouble from later original logic/resource failures. It neither hosts nor copies original behavior. The metadata audit establishes x86/net472, one mscorlib reference, zero resources and three declared Kernel32 P/Invokes. This is not a complete implicit native loader dependency audit or proof of exact .NET4.5/XNA compatibility. Installed Framework runtime servicing need not equal the target reference version.

## Actual outcome

Own compilation and metadata inspection passed, parent build zero warnings/errors. Startup-before-resume abort passed; Complete exited before Ready with no own phase recorded. Four subsequent scenarios were not run. Repeated unchanged-policy execution reproduced partial/exit2. Observed owned children exited with Job accounting zero. The OS process exit was `8007045A`; the last captured debug exception was `4000001F`, parameter0. These are distinct records, neither identifies the failing DLL or proves causation. Do not label the last debug event the root cause, or infer Main never executed from the missing file marker. No debug exception classification was changed in this iteration.

Expected: six own legacy startup/control cases. Actual: one passed, normal startup blocked; the runtime-success goal was not reached. Diagnostic implementation is delivered as partial. Dedicated outputs `framework-summary.json` and `framework-cases.json` remain ignored, separate from prior net8/native evidence. Resource limits queried, not stress tested; full file/network/input/audio/device confinement and original compatibility remain unproved.

## Review procedure and next feature

Double-click `验证旧框架受限启动.cmd`; it compiles only self-authored code using existing references, stops on first failed scenario, prints OWN_FRAMEWORK_SUMMARY, and preserves exit2 for partial. Provide this line and the own case's exit/stage/cleanup records if it differs. Do not send commercial files or dumps. `演示当前进度.cmd` shows the latest-only explanation from existing summary; it is not a character demonstration. Agent validation uses no-open generation.

Next proposed scope: diagnose the self-authored old-framework startup using existing own case logs and static debugger-event classification checks. Do not run original/XNA/Steam, relax restrictions, read new libraries or memory contents, create GUI, use real input/audio/network/devices, access game installation, modify system/ACL, download or install. New dependency or side-effect doubt requires stopping. Runtime/control changes need reviewed evidence rather than inferred permission.

Human reply template (substitute delivery commit): `验收 CDR-082 旧框架启动诊断工具〈提交〉并上传（保留启动失败结论）；授权下一步核对自有旧框架启动失败的现有日志与调试事件分类，保持现有限制，不运行原版/XNA/Steam、不读取新依赖、不打开GUI或访问游戏目录；遇到依赖或副作用疑点先停止。`

## Verification and publication boundary

Own summary renderer includes strict safety/outcome/budget checks; actual partial, missing summary and six malformed/unsafe fixtures are independently tested. Full no-window regression is recorded separately from the failing dedicated gate. No ordinary gate automatically starts this new legacy child. Exact final regression result is in current handoff.

Final tests: display20, no-open root0, phase8/log8 passed. Existing native control first recheck failed at report-file IO access; unchanged-policy repeat six passed. Preserve that intermittent failure, not a claim of consistent native success or known cause. Full first sandbox attempt1 at synthetic junction Access denied; specifically approved identical offline retry0 completed317 retained/26probe/13closure/45isolation/3audit/13XNA/8process and summary fixtures; window0, Rendering20 excluded. No ACL/test/policy changes. Dedicated framework cmd remained2; Full does not turn that into a pass.

Human accepted `1935cc3`; that accepted tree was outbound-audited and pushed/read back at `1935cc3aabd8029c578cdad30af6dfd81779de08` on the independent branch. Main unchanged. No commercial cache/assets/derived binaries or installation paths in its tracked outbound tree. This new iteration is not uploaded and does not mark CDR-082 accepted or the full goal complete.
