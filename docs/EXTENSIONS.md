# Deferred Content Extension Contracts

## Current status

Level/map restoration and read-only Mod asset access are deferred capabilities, not rejected capabilities. This document reserves stable architectural seams only. It does not add runtime implementation, scan a Mod directory, parse a map, execute Mod code or expand the current authorization.

## Asset source provider seam

The future asset pipeline may accept implementations of a narrow conceptual contract:

```text
IAssetSourceProvider
  DescribeSource() -> AssetSourceDescriptor
  Resolve(AssetRequest) -> AssetSourceEntry | NotFound | Rejected
  OpenRead(AssetSourceEntry, ReadBudget) -> bounded read-only content
```

Required properties:

- the user explicitly selects or enables every source root;
- source identity, priority, version and fingerprint are explicit;
- all paths remain canonical and contained under the approved root;
- reads occur through AssetWorker budgets and diagnostics;
- provider precedence is deterministic and ambiguity is an error;
- no provider returns executable behavior, arbitrary filesystem handles or unbounded streams;
- commercial bytes remain local and are never committed or distributed.

The built-in Celeste installation source will eventually be one provider. A future Mod asset provider may expose data-only sprites, atlases, metadata or other allowlisted resources without loading the Mod's assemblies or scripts.

## World content provider seam

The future simulation host may accept immutable world descriptions through another conceptual contract:

```text
IWorldContentProvider
  DescribeWorlds() -> WorldCatalogDescriptor
  LoadRoom(WorldId, RoomId, ContentBudget) -> RoomDescriptor | Rejected
```

`RoomDescriptor` may eventually describe bounded geometry, spawn points, supported entity placements and stable source provenance. It must not contain callbacks, executable scripts, renderer objects, filesystem handles or arbitrary runtime types.

The provider boundary lets a future module restore selected level/map structure without coupling map formats to `Simulation.Core`. Simulation continues to consume normalized immutable solids, triggers and supported entity descriptors.

## Explicitly not implemented now

- no compiled provider interfaces or plugin loader;
- no Celeste map, room or story parser;
- no Mod directory discovery or asset resolution;
- no Everest DLL, Lua, script or arbitrary code execution;
- no compatibility promise for existing Mods;
- no new filesystem, GUI or game-install authorization.

## Future gates

The compiled contracts may be introduced only in the later CDR-060 task after core asset and simulation contracts stabilize. Concrete level/map and Mod asset providers are separate tasks with synthetic fixtures first, explicit security review and fresh authorization before any real installation or Mod directory is read.
