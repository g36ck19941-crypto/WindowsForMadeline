# CDR-082 — Latest update: own startup log

2026-10-05. Primary owner, baseline9ca53dd, branch codex/cdr-082-local-compile-probe. Scope: existing own RestrictedProcessProbe (new StartupLog), explicit verification wrapper/entry explanation, ProgressDemo and project records. No runtime replacement, memory contents/dump, original/XNA/Steam, GUI/input/audio/network/device calls, installation access, system/ACL changes/download/install. Full GOAL unchanged; no acceptance or upload.

## Module, feature and role

This changes startup monitoring, not gameplay/assets. It records a bounded ordered timeline with sequence, parent-clock elapsed milliseconds, source, event and bounded detail. Sources distinguish parent observations, received child files and owned-child OS debug events. Module load order is preserved instead of only a sorted set. Name lookup failure is recorded explicitly (no handle, Win32 error, filtered name or budget), not mistaken for no load.

Use when a test exits before readiness: compare its last observed step and preceding events across revisions, avoid confusing a monitor assertion with the child's crash, and retain cleanup evidence. This does not itself fix startup or prove a role/animation exists. Parent receipt times are not internal child execution times. A final module name is not automatically the faulty module. Native stack/memory are not captured.

## Limits and validation

256 retained timeline events; excess events counted, not unbounded output. Only allowlisted source names/event identifiers and ASCII bounded details (no paths/addresses/free-form payload). Existing128 image-name and512B phase-file limits unchanged. Eight own log checks: numbering, image order, source separation, unknown source, backwards time, path payload, invalid event name, retained/dropped budget. They start no child. Existing phase checks and control probe remain separate. Wrapper validates actual sequence/timing/source before summary generation. Progress rejects inconsistent count/source/name summaries.

## Actual observation and unfinished work

Current own net8 still ends before Ready with final C0000409/A. The new timeline also records an earlier C0000005 event; this is an observed exception sequence, not a newly established root cause. Current component name lookups return Win32 error5; load events are retained with unavailable-name reasons, not falsely reported as no loads. Earlier21 known names/coreclr observation remain historical evidence, not current named observations. No permission/configuration workaround is attempted.

Explicit own entry remains PARTIAL/2. Native control6, phase protocol8 and startup-log8 are distinct own checks, not a managed/original pass. New page section explains only this update; cumulative historical content remains below. Existing full regression is separately checked without window tests.

Verification: progress12 passed. First sandboxed Full attempt exit1 at existing synthetic junction creation (Access denied, Invoke-OriginalCompileProbe.ps1:128). A specifically approved retry of the same existing offline gate succeeded exit0:317 retained, XNAown13/process8/isolation45+audit3/probe26/closure13 and existing report checks; window0/Rendering20excluded. No ACL/config or test change; the first run did not pass, the approved retry did. This regression does not cover/fix the new net8 startup failure; its explicit entry still2. Named debug-image lookup under the own-probe context remains denied; no weaker-profile probe retry.

## Acceptance and next function

Run root 验证受限进程原型.cmd. Inspect RESTRICTED_STARTUP_LOG and STARTUP_TRACE (sequence/observedMs/source/event/detail), and ignored managed-startup.json: monitor exceptionSource=parent-evidence-check versus nativeExceptionSource=owned-child-debug-event; no native crash stack claim. PARTIAL is expected until startup is fixed. Regenerate progress and read the new latest-update section; agent verification uses no-open mode.

Next under the same scope: use timeline evidence to decide a targeted own-startup investigation; no automatic weaker profile, name-access bypass or repeated unchanged runs. Own legacy-framework execution remains a separate pending scope decision, original loading still gated. No commercial content uploaded.
