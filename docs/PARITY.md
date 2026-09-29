# Fidelity and Parity Matrix

## Status vocabulary

- `unstarted`: no implementation.
- `contracted`: source/behavior contract exists but implementation is absent.
- `partial`: some behavior exists; known gaps remain.
- `exact_offline`: deterministic tests match the defined reference evidence.
- `asset_exact`: required source frames and metadata validate exactly for a supported installation profile.
- `integrated_verified`: asset, simulation and synthetic presentation are connected and verified.
- `human_accepted`: separately authorized human-visible acceptance exists.
- `deferred`: an architectural extension seam is reserved, but implementation and support commitments are intentionally postponed.
- `unsupported`: deliberately outside the current product.

No row may jump from `unstarted` to `human_accepted`; each applicable evidence layer is independent.

| Capability | Asset | Behavior | Integration | Human | Notes |
| --- | --- | --- | --- | --- | --- |
| Explicit install structure validation | asset_exact | exact_offline | partial | n/a | Selected-install CDR-016 passed read-only; no App integration |
| AssetWorker protocol/lifecycle | n/a | exact_offline | partial | n/a | Real Worker lifecycle verified; no App or parser integration |
| Atlas `.meta` metadata reader | asset_exact | exact_offline | partial | n/a | Selected-install CDR-016 passed; other versions remain unverified |
| Atlas `.data` page decoder | asset_exact | exact_offline | partial | n/a | Selected-install repeat fingerprint passed; no renderer connection |
| Madeline body animation | unstarted | unstarted | unstarted | unstarted | Original install only |
| Player hair | unstarted | unstarted | unstarted | unstarted | Procedural nodes and masks |
| Normal/Jump | n/a | partial | partial | unstarted | CDR-021 generated-input ticks pass; public constants/order covered, full shipped-build behavior and presentation unverified |
| Dash/Wall/Climb | n/a | partial | partial | unstarted | CDR-022 generated traversal passes; corner correction, jump-throughs, moving-wall boosts, blockers and advanced techniques remain unverified |
| Moving-solid carry/LiftBoost | n/a | partial | unstarted | unstarted | CDR-020 contract/tests pass; original-game parameter/order parity is not yet established |
| Synthetic Windows presentation | n/a | exact_offline | partial | unstarted | CDR-030 hidden D3D11/DirectComposition commit and recovery tests pass; no asset animation or visible observation |
| Anonymous desktop geometry | n/a | exact_offline | partial | unstarted | CDR-031 generated tracking and authorized aggregate-only Windows snapshot pass; no overlay placement or visible observation |
| Theo Crystal | unstarted | unstarted | unstarted | unstarted | Hold/throw/collision interactions |
| Glider | unstarted | unstarted | unstarted | unstarted | Hold/fall/launch interactions |
| Spring | unstarted | unstarted | unstarted | unstarted | Player and supported entities |
| Refill | unstarted | unstarted | unstarted | unstarted | Respawn/cooldown |
| Water | unstarted | unstarted | unstarted | unstarted | Volume behavior |
| Bumper | unstarted | unstarted | unstarted | unstarted | Radial launch/cooldown |
| Puffer | unstarted | unstarted | unstarted | unstarted | Swim/explosion/launch |
| Seeker | unstarted | unstarted | unstarted | unstarted | Deferred complexity |
| Levels/maps | deferred | deferred | deferred | deferred | Future `IWorldContentProvider`; no current parser |
| Data-only Mod assets | deferred | n/a | deferred | deferred | Future `IAssetSourceProvider`; no current directory access |
| Story/cutscenes | unsupported | unsupported | unsupported | unsupported | Outside current product goal |
| Executable Everest Mod behavior | unsupported | unsupported | unsupported | unsupported | No DLL/script/runtime-hook execution |
