# CDR-074 Acceptance

State: implementation complete and ready for developer acceptance; not uploaded.

## Delivered

- Immutable static one-way-platform geometry and snapshots in Simulation.Core.
- Upward and horizontal pass-through, downward landing from above and same-tick exact-top landing.
- Explicit Player drop-through targeting one platform, with clear/rearm and bounded-expiration outcomes.
- Ordinary Solids remain blocking during drop-through.
- Immutable Player evidence and four stable one-way-platform event kinds.
- Eight generated Simulation.Core cases and nine generated Player cases.

## Evidence

- Release solution build: 0 warnings, 0 errors.
- Simulation.Core focused suite: 42/42.
- Player focused suite: 59/59.
- Complete regression set: 901/901.
- Generated demo: upward passage `true`; drop from y=0; lower-platform landing at y=19; start/completion/landing diagnostics `1/1/1`; rearm `true`; deterministic replay `true`.
- Game/GUI/live-input launch: none. Installation/local-reference access: none. Commercial bytes: none.

## Acceptance procedure

1. Double-click `演示当前进度.cmd`.
2. In the Player explanation, find CDR-074 and confirm upward passage is true, y=0 drops to y=19 on `one-way-lower`, event counts are `1/1/1`, and rearm is true.
3. Double-click `验证当前版本.cmd`; expect 0 build warnings/errors, Simulation.Core 42/42, Player 59/59, total 901/901 and `CDR-074 OFFLINE VERIFICATION PASSED`.
4. Confirm no game or product GUI starts; only the developer-triggered generated HTML report may open.

## Project role and limits

This adds the platform rule needed for layered desktop traversal: Player can rise through a ledge, stand on it and deliberately fall to the next layer. It also makes every drop-through outcome inspectable. It does not establish moving/special one-way platforms, ducking, dash interaction, original map loading, commercial numeric parity, animation/audio or human-visible desktop behavior.

## After acceptance

After explicit developer acceptance, freshly verify and audit the exact CDR-074 acceptance commit, then publish only that commit to an independent branch. The recommended next bounded task is CDR-075 Player ducking and safe unduck-clearance calibration, generated-only and offline. CDR-075 is not authorized yet. GUI, live input, game/install/local-reference access, commercial bytes and CDR-061 remain forbidden.
