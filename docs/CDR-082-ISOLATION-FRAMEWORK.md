# CDR-082 — Own adapter framework compile compatibility

2026-10-03. Primary. Continuation of authorized isolation implementation using already-approved four net472 reference assemblies; no recovered execution, new assets or original behavior changes. Acceptance/upload pending.

## Role and actual result

The managed context module can now also emit a net472 library, matching the experimental recovered original library's compile target. Removed two modern-only helper calls from our own source (null guard and generic enum validation), added explicit usings and an own conditional IsExternalInit compiler marker/target attribute. No gameplay implementation or original-source edit.

The opt-in compiler wrapper hashes the four fixed reference files against prior evidence before/after compilation; hashes both own source files before/after. It invokes installed SDK csc directly, nostdlib/deterministic/library, explicit2 files/4 references, no generated project, restore for this target, packages/resources/analyzers or install access. The own net8 synthetic suite builds first using offline configuration.

Measured: zero compile warnings/errors. Static PE audit verifies assembly identity, actual TargetFrameworkAttribute net472, referenced .NET identities4.0.0.0/neutral/tokenB77A5C561934E089, session/context types, no PInvoke methods or resources, and output not loaded into auditor AppDomain. Output references only mscorlib. Repeated outputSHA256:47E4D0D68DE4D5CFD84F3B643382B883DF269D444F5F4AD5B8FE1B836B24D81B. The generated net472 library was NOT executed. No original assemblies were recompiled or read in this update.

This proves own-code compile compatibility, not runtime ABI compatibility, original net45 parity or a bound original service bridge. OriginalBound/runtimeCompatibilityEstablished remain false. Adapter policy still not a security sandbox.

## Tests and acceptance

- Existing45 synthetic context/lifecycle/service tests still pass on net8. Three audit negative tests reject artifact escape, wrong assembly identity, truncated binary. Four generated summary tests cover valid compile-only, missing, false runtime claim, wrong target.
- Existing gate317/26/13/4 and isolation-summary4 remain separate; no windows or installation reads during ordinary verification. Ordinary gate does NOT retry system-reference compilation; new compiler entry is explicit opt-in only.
- Double-click 检查隔离适配旧框架编译.cmd; expect ISOLATION_NET472_COMPILED originalBound=false outputExecuted=false, exit0. Reads only already-approved4 system reference files. No game/assets/Steam/XNA reads.
- 验证当前版本.cmd checks own/synthetic tests. Progress report includes new old-framework compile-only section; agents suppress browser opening. Safe summary artifacts/cdr-082-isolation-framework/summary.json; own generated DLL ignored.

## Next function

Precisely design and validate the actual original static-time/input/scene bridge. No actual binding/initialization/run until its permissions and source strategy are clear. No original-source transformations, assets or original execution are implied by this compatibility milestone. Developer acceptance is still required before GitHub upload.
