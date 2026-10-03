# CDR-082 — Local managed isolation adapter

Subsequent own-adapter framework compilation is now established without execution; see [net472 preparation](CDR-082-ISOLATION-FRAMEWORK.md). Earlier net8-only compile scope below describes the initial delivery; actual original binding/runtime remain unproven.

2026-10-03. Primary owner. Explicit developer permission: design and implement local isolation adapters, no recovered-code execution, new assets or original behavior-source changes. Local/offline implementation; acceptance and upload pending.

## Role and implementation

RuntimeIsolation supplies a reproducible external context, not another Player or movement simulator. It owns no positions, velocities, collisions or gameplay rules. Its immutable step contains tick0-based elapsed time (tick/60), fixed float delta1/60, copied generated direction/button state and edge transitions. The bounded input sequence (1..4096 frames) cannot be mutated through the caller's list after construction. Exhaustion and closure fail explicitly without advancing. New sessions replay the same contexts.

Eight named requests (Steam, audio, window, graphics device, live input, asset read, file write, original code) always return an explicit denied decision. Invalid services fail rather than falling through. This cooperative policy cannot stop code bypassing the adapter; it is NOT a process/OS sandbox. No allow switch, runtime loader, native handle, graphics device, filesystem callback or original update delegate exists.

Independent managed module/tests target net8.0 and reference no game/XNA/Steam assembly. This does not establish a net472 original-framework bridge. OriginalBound remains false and BindingStatus is ORIGINAL_BRIDGE_NOT_BOUND. Original constructor/Engine static fields/Input static services/Level/resources are not connected. No source transformation occurred.

## Connection design and remaining feasibility

Eventual original integration needs a separately audited bridge to original time, input, assist/session/scene and resource services, avoiding normal Engine/game initialization. Original APIs are static/concrete, not implementations of our interfaces; merely creating this context does not replace those calls. Any reflection initialization or original-code execution can trigger original static initialization and requires explicit execution authorization. Any source/IL transformation must have separately approved precise scope; do not introduce guessed movement rules. Resource and graphics-native permissions remain separate. A meaningful next binding test must prove actual original state consumes these injected values, not just replay our own data.

## Verification and observable demo

- 45 own synthetic assertions: repeat replay, time/directions/button edges, copied input, count/bounds/unknown button rejection, 4096-step time drift check, exhaustion and disposed-session rejection, all8 denied services, managed-only dependencies and no game assembly loaded.
- Four summary probes: valid managed-only report, absent report marked not-inspected, false original-bound claim and wrong passed-count both rejected. Full no-window gate retains317/26 probe/13 closure/4 compile-summary cases separately; excluded Rendering20 remain excluded, not passes.
- Double-click 验证本地隔离适配.cmd: five generated context JSON records and eight denied decisions, then ISOLATION_VERIFIED passed=45 failed=0 originalBound=false recoveredCodeExecuted=false. Not character motion.
- 验证当前版本.cmd runs the reviewed synthetic suite alongside prior gate; no installation/source/resource access or original execution. 演示当前进度.cmd includes a simple Chinese isolation section; agents use CDR_DEMO_NO_OPEN=1 and CDR_NO_PAUSE=1.

artifacts/cdr-082-isolation/summary.json is generated from successful test output, never an acceptance flag. JSON event logs distinguish adapter context from simulation/entity/visibility. Source/library/cache commercial content stays ignored; this update contains own tools/tests/design and safe aggregates only.

## After acceptance

Define the precise bridge and initializer strategy with remaining compatibility/risk questions. Obtain specific permission before executing original code, reading new resources or transforming original source. No permission for Steam APIs, game entry, windows, real input or installation writes is implied. Do not upload until developer acceptance.
