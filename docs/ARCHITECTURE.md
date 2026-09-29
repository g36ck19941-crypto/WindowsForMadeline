# Architecture

## 1. Dependency direction

```text
App ───────────────┬─> Install
                   ├─> AssetWorker.Client ─IPC─> AssetWorker.Process
                   ├─> Desktop
                   ├─> Animation ──> Rendering
                   ├─> Rendering
                   └─> Simulation + Entity modules

Install ─────────────> Contracts
AssetWorker ─────────> Contracts
Desktop ─────────────> Contracts
Animation ───────────> Contracts + Rendering
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

CDR-030 splits presentation again: `CelesteDesktop.Rendering` owns immutable-frame validation, ordered health events, changed-fingerprint detection and bounded recovery; `CelesteDesktop.Rendering.Windows` owns the hidden HWND, D3D11 and DirectComposition COM resources. The Windows backend cannot reference Simulation, Player, assets on disk or live input. A completed DirectComposition commit is not human-visibility evidence.

CDR-032 adds `CelesteDesktop.Animation` between validated catalogs and Rendering. It consumes caller-provided fixed ticks and immutable catalog frames, resolves loop/final-frame/direct-goto timing, composes a bounded transparent canvas and submits it one-way to Rendering. It has no reference to Simulation, Desktop, filesystem or live input, so neither renderer timing nor presentation failure can advance or rewrite physics.

## 5. Entity isolation

Each entity module exposes state, deterministic update, collision response and semantic effects. Cross-entity behavior is expressed through narrow interaction contracts rather than type inspection in App. A disabled or failed optional entity cannot alter Player rules or prevent other entities from loading.

## 6. Reserved extension seams

`docs/EXTENSIONS.md` reserves conceptual `IAssetSourceProvider` and `IWorldContentProvider` boundaries. They allow future read-only Mod asset sources and normalized level/map descriptions without coupling those formats to Simulation or App.

These are architecture contracts only. No provider interface, plugin loader, map parser or Mod reader is implemented in the current phase.

## 7. Current non-goals

- current-phase levels, rooms, story or map loading;
- arbitrary Everest runtime hooks, executable Mod code or code-driven skins;
- automatic bundling or redistribution of Celeste assets;
- live game-process capture;
- claims of complete parity without recorded evidence.
