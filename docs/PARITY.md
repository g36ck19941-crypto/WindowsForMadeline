# Fidelity and Parity Matrix

> Superseded current-state notice (CDR-080): Player, Simulation.Core, all eight self-designed Entity modules and App have been removed. All corresponding rows below are historical, not current implementations or parity claims. Original local runtime integration is unstarted. Resource, animation and rendering tests remain tooling evidence only.

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
| Catalog animation presentation | asset_exact | exact_offline | partial | unstarted | CDR-032 generated catalog-to-frame timing/composition/presentation passes; selected install format was validated separately, but no real pixels are persisted or visibly shown |
| Theo Crystal | asset_exact | partial | partial | unstarted | CDR-040 generated pickup/carry/throw/collision matrix passes; selected-install format was validated separately, but original numeric parity, asset animation and visible integration are not established |
| Glider | asset_exact | partial | partial | unstarted | CDR-041 generated pickup/carry/fall-limit/Player-application/throw/flight matrix passes; selected-install format was validated separately, but formal App orchestration, original numeric parity, asset animation and visible integration are not established |
| Spring | asset_exact | partial | partial | unstarted | CDR-042 generated four-direction lifecycle and Player/Theo/Glider application matrix passes; original contact/numeric parity, formal App routing, asset animation and visible integration are not established |
| Refill | unstarted | partial | partial | unstarted | CDR-043 generated collection/resource-restore/cooldown/respawn and Player-application matrix passes; original contact/respawn parity, formal App routing, asset animation and visible integration are not established |
| Water | unstarted | partial | partial | unstarted | CDR-044 generated overlap/enter/submerged/drag/buoyancy/swim-limit/exit and Player-application matrix passes; original surface/contact/numeric parity, formal App routing, asset animation and visible integration are not established |
| Bumper | unstarted | partial | partial | unstarted | CDR-045 generated circular contact/radial launch/fallback/cooldown/rearm and Player-application matrix passes; original contact/numeric/path parity, formal App routing, assets, audio and visible integration are not established |
| Puffer | asset_exact | partial | partial | unstarted | CDR-046 generated bounded swim/warning/explosion/respawn and Player-application matrix passes; selected-install format was validated separately, but original movement/contact/timing/numeric parity, special interactions, formal App routing, animation/audio and visible integration are not established |
| Seeker | unstarted | partial | partial | unstarted | CDR-047 generated patrol/detection/chase/windup/dash/hit/wall-stun/recovery matrix passes; original navigation, obstacle avoidance, damage/bounce behavior, numeric parity, formal App routing, assets/audio and visible integration are not established |
| Offline App orchestration | n/a | partial | exact_offline | unstarted | CDR-050 lifecycle, one-step-per-tick ordering, effect routing and failure isolation pass with generated inputs; visible host, live input, real assets and human visibility are not established |
| Headless App host | n/a | exact_offline | exact_offline | unstarted | CDR-051 fixed 60 Hz scheduling, bounded catch-up, generated input, cancellation and disposal pass; visible GUI, live input, desktop observation, real assets and human visibility are not established |
| Levels/maps | deferred | contracted | deferred | deferred | CDR-060 compiled immutable `IWorldContentProvider`; synthetic provider only, no map parser |
| Data-only Mod assets | contracted | n/a | deferred | deferred | CDR-060 compiled bounded `IAssetSourceProvider`; synthetic provider only, no directory access |
| Story/cutscenes | unsupported | unsupported | unsupported | unsupported | Outside current product goal |
| Executable Everest Mod behavior | unsupported | unsupported | unsupported | unsupported | No DLL/script/runtime-hook execution |
