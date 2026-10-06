# CDR-082 M2 — readonly method boundaries and adaptation design

2026-10-06. Primary owner. Baseline5d73cab on codex/cdr-082-local-compile-probe. Developer accepted M1/publication record and authorized source-cache method analysis/design only. This delivers that bounded design, NOT the full M2 type/dependency closure or a runtime feature. No recovered-source edits/compilation/execution, new references/assets, process probes, GUI or installation access.

## Role in the project

Identify where original input rules and time processing attach to the platform before implementing a modern adapter. This avoids mistaking ordinary initialization or a method named “clear input” for a device-free path. Original behavior remains the goal; no guessed substitute logic, old-framework host or silent platform replacement.

## Findings and proposed contracts

1. Input acquisition, stored state and original consumption must be separate. Proposed input contract carries prior/current key/button/axis/trigger/connection/disabled/focus state plus explicit binding configuration. Preserve original buffering, repetition, consumption, overlap and ordering; do not duplicate these rules in our input adapter. Exact methods/overloads and locations stay in the ignored private map.
2. Raw elapsed time, scaled time and frame state are distinct. Proposed time contract carries both time values/rates and frame identity, preserving original float operations and order. Freeze and input buffering do not necessarily use the same time value. An adapter-only60Hz trace would not prove original logic consumes it.
3. Ordinary engine/settings startup is excluded from the proposed first input/time slice: it reaches window/graphics, device, file or service boundaries. Constructors, base registration and static/instance initializers must be audited BEFORE later execution. A null-input/cleanup path still contains device requests; denial must be explicit, never silent success.
4. Proposed first derived slice is original input consumers plus an explicit time/scheduling boundary and preset bootstrap, no Player/assets yet. Player/scene/animation and desktop entities remain later ordered requirements, not removed from GOAL. Source transformation would need a separately authorized ignored derived copy, unchanged original baseline and per-change provenance. None is implemented now.
5. Type/platform compatibility is unresolved. No modern replacement/runtime version is selected as proven compatible; no XNA/FNA/MonoGame/legacy helper substitution. Enum/state/numeric APIs and implicit/module initialization need readonly evidence before selecting references or compiling a derived slice.

## Measured evidence

- Eleven selected cached source files;423 method/constructor/operator declarations,87 properties,266 field initializer declarations;1927 lexical invocation/construction sites,51 inside lambda/local-function bodies. Counts are declarations/sites, not runtime calls, unique methods reached or eager initializations. Abstract declarations are included; implicit base calls, indexers/events/operators/property dispatch are not a resolved call graph.
- Selected-source digest15F7147AE0D8301CAEB13A4F4C29B0662A50C6B6B98202AD1310ED6208972D26. Selected hashes stable pre/post and all11 match the previously delivered M1 private hash map. This does not certify official provenance or prove untouched unrelated files.
- Eight own synthetic parser assertions, actual root cmd exit0, two negative path cases exit3, eleven own summary/display checks passed. Own parser and ProgressDemo builds have zero warnings/errors. No original compilation occurs: analyzer returns before the old compile branch and uses syntax parsing only, no compilation/semantic model/reference metadata binding.
- Current HTML generated directly by own summary tool, no browser: latest marker method-design-v1. Page generation reads existing reports only. Full verification NOT run because its process-probe suites are outside current authority; old restricted/framework fixture suite marker updated but suite not rerun. Past runtime test counts are historical.

## Local-only detail and public boundary

Latest method map: local-cache/cdr-082-method-design/bba107f9ae414fe68daea0899609bbce/methods-local-only.json. Source-derived review: local-cache/cdr-082-method-design/7f17ecced79f4409b97a847484d20d01/ADAPTATION-REVIEW-LOCAL-ONLY.md. Both verified Git ignored. Signatures, expression names, assignments, references and source locations are private. Public summary artifacts/cdr-082-method-design/summary.json contains counts/hashes/safety flags only. No commercial source, IL, assets, binaries or installation paths enter this update.

No precise semantic callees, transitive reachability/minimal closure, platform side-effect safety or modern compatibility established. Deferred syntax is explicitly marked; location alone is not proof of execution. Restricted host startup remains unresolved historical evidence. Original Player not created.

## Acceptance and next gate

Double-click 检查迁移方法边界.cmd: expect METHOD_DESIGN_COMPLETED,11files/sourceHashesStable=true/originalExecuted=false, exit0 and private report location. Read the Chinese mirror for interpretation. Existing 演示当前进度.cmd shows the latest explanation; it opens a browser for the human, not used by the agent. No character demonstration claimed. Failure: share METHOD_DESIGN_FAILED only, not private source-derived details.

Next proposed gate M2-T: explicitly authorize readonly metadata review of previously cached XNA references and current project type contracts; details local, public aggregate only. No system/installation/new dependencies/source changes/compiler/target loading/probes/GUI. Determine state/enumeration/numeric compatibility and initializer unknowns, then propose exact derived transformations and M3 scope. Do not jump to compilation while required type evidence is missing. No new permission inferred from this report or task acceptance.

Accepted5d73cab uploaded/read back at5d73cab92500b552f1e637b640050ff5cb2a7f0e on independent branch; main remains d237277e10af090cf60ec015c22174565e6cdc0a. This M2 update remains unaccepted/unuploaded. Whole GOAL incomplete; do not manually edit accepted state.
