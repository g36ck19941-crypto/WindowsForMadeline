# CDR-082 — Restricted execution environment proposal

Date: 2026-10-04. Owner: Primary. State: **design only / pending review**.
English is normative; [Chinese review copy](zh-CN/CDR-082-RESTRICTED-EXECUTION-PLAN.md).

## Role and current evidence

Build a test compartment before attempting original input/time initialization: unwanted access must be prevented, not merely logged after it happens. This is infrastructure planning, not a new character, animation or gameplay feature. The full objective in GOAL.md is unchanged.

Baseline: branch `codex/cdr-082-local-compile-probe`, HEAD `4b531fd368e318ac9c7cb57627bf338a8d210694`, initially clean. Existing reports establish compile-only output and own-process supervision; they do not establish original runtime compatibility. Cached mixed-mode XNA initializer analysis still has two unclassified native bodies and eleven indirect-call sites per mixed library. No original/XNA loading or native execution was performed for this proposal. Prior test results are historical, not fresh evidence for containment.

## Decision

Evaluate **AppContainer plus pre-start mitigations and a Job Object** as the first candidate, not as an already approved sandbox. It may be unsuitable for x86 .NET Framework 4.7.2/mixed-mode XNA. If any mandatory restriction cannot be enforced, stop; never retry in an ordinary process, relax controls, substitute FNA or change original behavior code silently.

An AppContainer limits capabilities but permits some keyboard/mouse access and has less restrictive read access; it is not our complete input/filesystem policy. [Microsoft isolation reference](https://learn.microsoft.com/en-us/windows/win32/secauthz/appcontainer-isolation).

A Job Object provides group lifetime/resource management; UI restrictions cover specific operations, not all windows or devices. It is an additional containment component, not a file/network security boundary. [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects), [UI restrictions](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_ui_restrictions).

Win32k system-call denial is a candidate GUI restriction, not an audio/controller/network policy; compatibility must be tested without target libraries first. An audit-only setting does not enforce denial. [Microsoft mitigation reference](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-process_mitigation_system_call_disable_policy).

## Mandatory restrictions and proof gaps

All rows below are proposed requirements, **not implemented or verified**.

| Surface | Candidate control | Evidence required before original loading |
| --- | --- | --- |
| Game installation, personal files, registry | No game path in child configuration; separate identity and explicit access policy; narrow read-only staged input, private scratch | Effective permissions including broadly granted read access, links and inherited handles; deny generated out-of-scope fixtures without reading the installation |
| Runtime dependencies | Exact approved identities/hashes; controlled search paths and staged manifest; OS/CLR loader dependencies explicitly inventoried | No implicit GAC/PATH/current-directory fallback; unlisted load stops. Runtime/system reads need their own explicit scope; metadata permission is not execution permission |
| Network, remote IPC | No network capability; verified effective isolation, no network broker or inherited connection | Own local fixtures prove prohibited access denied; parent reporting channel cannot forward arbitrary requests |
| GUI and real input | Pre-start Win32k denial, no interactive desktop access; device-access policy additionally required | Denial must precede loader initialization. Window APIs, keyboard/mouse and XInput/controller paths need separate coverage; a hidden desktop or CreateNoWindow is insufficient |
| Audio, Steam and devices | No broker/endpoint/Steam service, no inherited device handles; effective device/library restrictions still to be designed | No capability flag is claimed to cover every API. Steamworks metadata-only grant does not permit loading/calling it. Blocking a DLL name alone is insufficient; unresolved routes remain unsupported |
| Other processes | Private Job, no breakaway, child-count limit, narrow handle inheritance; no process-launch broker | Own child launch/escape/parent-exit tests; restrictive setup before resuming child. Cleanup only owned objects, never name/PID-wide killing |
| Commercial data and output | Only explicitly authorized ignored-cache inputs, read-only; ignored per-run scratch; aggregate reporting | No raw code/IL/pixels/audio in public logs, Git or upload. Child logs untrusted, bounded and sanitized; no write to inputs or installation |

No arbitrary-code sandbox can make an unconditional promise of zero side effects. OS escape defects and missing device/loader enforcement are residual risks. In particular, this proposal currently has **no verified complete no-input/no-audio profile**; it does not authorize original execution.

## Proposed lifecycle and diagnostics

Trusted supervisor owns only this run's handles, identifiers and scratch directory. Reject links/reparse points, changed hashes, unexpected settings and unapproved inputs. Do not grant access to the whole repository or installation. Staging new dependency copies is a separate authorization, not a consequence of this plan.

Create restrictions before any target load or module initializer; use a suspended-start design so Job assignment and effective policy checks precede execution. Failure terminates the owned child without target loading. No target library in the supervisor. No injectable shell, command, target path or loader hook.

Proposed phases: policy-checked → own-probe-ready → dependencies-verified → target-load-requested → target-loaded → input-context-ready → preset-steps-completed → cleanup-confirmed. Log run ID, policy version, actual effective settings, last phase, elapsed time, exit/HResult/exception chain when available, timeout/output-limit reason and cleanup outcome. A crash before reporting remains unknown; absence of a log is not absence of a side effect. Preserve existing bounded pipes and deadlines, but review their compatibility anew. Phase labels never prove actor creation, visibility or movement.

Timeout/native abort handled by the supervisor and owned Job; prove every owned process exited. Scratch disposal and profile/ACL rollback require exact recorded ownership; failures reported, not silently ignored. No global ACL/firewall/policy changes or recursive deletion of broad directories.

## Alternatives

Plain child, restricted token or low integrity alone: insufficient for this contract. Monitoring alone: diagnostics only.

A headless VM without host shares, network, redirected input/audio or devices is a separate fallback study. It needs verified host facilities, guest dependencies and explicit setup permission; not assumed available or compatible. Windows Sandbox is **not** the current no-GUI route. Its configuration can disable networking and make shares read-only; AudioInput controls microphone input, not all playback. Do not launch it or generate an executable `.wsb` here. [Microsoft Sandbox configuration](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file).

## Ordered gates and authorization

1. **This delivery:** documents only. No code/config generation, process tests, OS inventory, policy/ACL changes, installation reads or target loading.
2. **Read-only feasibility inventory, if separately authorized:** fixed Windows version/architecture, current security/process facilities and approved CLR capabilities; no installation access, loading, installation/download or recursive dependency scan. Return supported/unknown/unsupported per matrix row and concrete implementation scope. Do not enable features.
3. **Implementation and own-only denial probes, separately authorized:** define exact local files and reversible profile/ACL changes first. Enumerate API attempts and generated fixtures. Any probe that might create a window, access live input/audio/network or alter system state needs explicit permission and verified preconditions; do not assume a failed attempt has no side effect. No original/XNA/Steam code.
4. **Original loader-only feasibility, separately authorized:** only after enforcement and compatibility evidence pass; exact cached manifests and initialization boundary reviewed. No Player, assets, devices or normal Engine startup. Stop on missing dependency/control or unexpected effect.
5. **Original preset input/time bridge, separately authorized:** cached inputs and original state buffering only, then separately scoped Player/resource/offscreen integration. Visible desktop acceptance remains a later gate.

Design acceptance grants none of gates 2–5 and does not change CDR-082 to accepted. Future OS inventory is a bounded feasibility check, not an endless dependency search.

## Concrete acceptance

Read this document or the Chinese mirror directly; no command or GUI demo is needed. Check that supervision and prevention are distinct, all prohibited surfaces have an explicit gate, unknown device/XNA compatibility is not presented as solved, and original loading is later than enforcement proof. This round validates documentation links, allowed file scope and Git whitespace only; no runtime tests rerun.

Expected result: a reviewable plan, **not** a running sandbox or original character. Next proposed feature is a read-only feasibility report under a new bounded authorization. Source/implementation/derived binaries remain private and ignored; this local documentation commit is not uploaded without acceptance.
