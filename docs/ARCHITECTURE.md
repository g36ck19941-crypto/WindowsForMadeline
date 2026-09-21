# Architecture

## 1. Dependency direction

```text
App ───────────────┬─> Install
                   ├─> AssetWorker.Client ─IPC─> AssetWorker.Process
                   ├─> Desktop
                   ├─> Rendering
                   └─> Simulation + Entity modules

Install ─────────────> Contracts
AssetWorker ─────────> Contracts
Desktop ─────────────> Contracts
Rendering ───────────> Contracts
Entity modules ──────> Simulation.Core
Simulation.Core ─────> Contracts
Contracts ───────────> nothing
```

Circular dependencies are forbidden. Simulation cannot see file paths, bitmaps, HWNDs, WPF/WinForms types, clocks or operating-system APIs.

## 2. Process boundary

`CelesteDesktop.AssetWorker` is a separate process with the smallest practical privileges. It receives a selected root plus an allowlisted request and returns immutable descriptors or bounded BGRA32 payloads through a versioned IPC envelope.

Worker guarantees:

- opens source files read-only;
- canonicalizes every path under the approved root;
- follows no reparse point;
- enforces file, entry, dimension and decompression budgets;
- has request timeout and cancellation;
- never writes the game directory;
- returns structured failure instead of partial success;
- never sends arbitrary source files or unrequested commercial bytes.

App treats worker exit, timeout and protocol violation as `asset_unavailable`; simulation and settings remain usable.

## 3. Data boundaries

### Asset contracts

- `AssetSourceFingerprint`: supported game profile plus required source-file length/hash summaries.
- `AtlasEntryDescriptor`: canonical ID, page, trim rectangle, untrimmed frame and validated origin data.
- `Bgra32Frame`: width, height, stride, immutable payload and content fingerprint.
- `AnimationDescriptor`: canonical action, ordered frame IDs, delay, loop/goto semantics and explicit metadata.
- `EntityAssetCatalog`: per-entity allowlisted visuals without global name flattening.

### Simulation contracts

- immutable tick input;
- explicit actor/entity states;
- ordered solid snapshot with motion classification;
- semantic visual and sound events;
- no direct texture, audio or desktop operations.

## 4. Update and rendering flow

```text
Desktop capture (bounded rate) ──> immutable world snapshot
Input snapshot ──────────────────> fixed 60 Hz scheduler
World + input ───────────────────> deterministic simulation ticks
Simulation snapshot ─────────────> animation selection
Validated asset catalog ─────────> frame resolution
Presentation snapshot ───────────> renderer
Renderer ────────────────────────> DirectComposition Present
```

Rendering delay cannot feed back into simulation time. UI dispatcher timing cannot advance animations or entity state.

## 5. Entity isolation

Each entity module exposes state, deterministic update, collision response and semantic effects. Cross-entity behavior is expressed through narrow interaction contracts rather than type inspection in App. A disabled or failed optional entity cannot alter Player rules or prevent other entities from loading.

## 6. Deliberate non-goals

- levels, rooms, story or map loading;
- arbitrary Everest runtime hooks or code-driven skins;
- automatic bundling or redistribution of Celeste assets;
- live game-process capture;
- claims of complete parity without recorded evidence.

