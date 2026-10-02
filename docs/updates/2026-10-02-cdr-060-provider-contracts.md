# CDR-060 — Compiled data-only provider contracts

## Added

- Added `IAssetSourceProvider` with explicit source identity, typed logical requests, bounded copied bytes and found/not-found/rejected results.
- Added `IWorldContentProvider` with immutable bounded world, room, Solid, spawn and supported-entity descriptors.
- Excluded paths, streams, file handles, delegates, assemblies, scripts and executable resource kinds from the public contract.
- Added 30 synthetic contract/provider tests and a cumulative generated provider demo.

## Role in the project

These contracts are the safe socket for future map and data-only Mod support. They prevent future source-format code from entering Simulation or carrying executable behavior with content. They do not parse any real map or Mod today.

## Acceptance

Double-click `演示当前进度.cmd`; CDR-060 must show 4 generated asset bytes, one world/room, 2 Solids, 1 spawn and 3 supported entities, with both over-budget requests rejected and replay identical. Then double-click `验证当前版本.cmd` and require `CDR-060 OFFLINE VERIFICATION PASSED` with 856/856.
