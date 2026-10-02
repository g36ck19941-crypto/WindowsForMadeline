# Data-only Extension Provider Contracts

CDR-060 compiles the previously conceptual extension seams into immutable contracts. It does not implement a map parser or a Mod reader.

## Asset source contract

`IAssetSourceProvider` exposes only three operations: describe a source, resolve a typed logical resource ID, and return bounded copied bytes. Descriptors carry a stable provider ID, explicit source kind and priority, version, length and SHA-256 facts. Results are explicitly `Found`, `NotFound` or `Rejected`.

The public contract exposes no path, stream, file handle, callback, assembly or executable resource kind. A future real provider must still run behind the separately secured AssetWorker boundary.

## World content contract

`IWorldContentProvider` describes bounded worlds and loads one requested immutable room under caller-provided budgets. A room may contain only:

- integer room bounds;
- rectangular Solid placements;
- named spawn points;
- placements of the currently supported entity kinds;
- provider/world/room identity and a source SHA-256 fact.

It contains no script, behavior callback, renderer object, filesystem object or format-specific map data. Simulation remains unaware of the source format.

## Current boundary

CDR-060 uses synthetic providers only. No Celeste map, Mod directory, game installation, DLL, Lua or script is read or executed. CDR-061 and CDR-062 remain separate tasks requiring their own authorization and security gates.
