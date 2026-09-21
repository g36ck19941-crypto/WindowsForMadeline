# Fidelity and Parity Matrix

## Status vocabulary

- `unstarted`: no implementation.
- `contracted`: source/behavior contract exists but implementation is absent.
- `partial`: some behavior exists; known gaps remain.
- `exact_offline`: deterministic tests match the defined reference evidence.
- `asset_exact`: required source frames and metadata validate exactly for a supported installation profile.
- `integrated_verified`: asset, simulation and synthetic presentation are connected and verified.
- `human_accepted`: separately authorized human-visible acceptance exists.
- `unsupported`: deliberately outside the current product.

No row may jump from `unstarted` to `human_accepted`; each applicable evidence layer is independent.

| Capability | Asset | Behavior | Integration | Human | Notes |
| --- | --- | --- | --- | --- | --- |
| Madeline body animation | unstarted | unstarted | unstarted | unstarted | Original install only |
| Player hair | unstarted | unstarted | unstarted | unstarted | Procedural nodes and masks |
| Normal/Jump | n/a | unstarted | unstarted | unstarted | Fixed 60 Hz |
| Dash/Wall/Climb | n/a | unstarted | unstarted | unstarted | Exact tick/order evidence required |
| Moving-solid carry/LiftBoost | n/a | unstarted | unstarted | unstarted | Desktop windows adapt to Solid motion |
| Theo Crystal | unstarted | unstarted | unstarted | unstarted | Hold/throw/collision interactions |
| Glider | unstarted | unstarted | unstarted | unstarted | Hold/fall/launch interactions |
| Spring | unstarted | unstarted | unstarted | unstarted | Player and supported entities |
| Refill | unstarted | unstarted | unstarted | unstarted | Respawn/cooldown |
| Water | unstarted | unstarted | unstarted | unstarted | Volume behavior |
| Bumper | unstarted | unstarted | unstarted | unstarted | Radial launch/cooldown |
| Puffer | unstarted | unstarted | unstarted | unstarted | Swim/explosion/launch |
| Seeker | unstarted | unstarted | unstarted | unstarted | Deferred complexity |
| Levels/maps/story | unsupported | unsupported | unsupported | unsupported | Explicit non-goal |
| Arbitrary code-driven Everest Mods | unsupported | unsupported | unsupported | unsupported | Requires runtime hooks |

