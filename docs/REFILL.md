# CDR-043 Deterministic Refill

## Role in the project

Refill is the first entity that restores Player-owned resources instead of changing movement directly. It observes an immutable generated contact snapshot, emits a target-addressed resource effect, and lets Player apply that effect through its own external-effects entry point. Refill never depends on Player.

This establishes the same one-way interaction rule used by Spring: an entity may request a bounded change, but it cannot reach into another controller and mutate private state.

## Implemented behavior

- `Available`, `Cooldown` and `Disabled` states.
- Player contact snapshots with target identity, position, current/maximum dash charges, current/maximum stamina and eligibility.
- Collection only when at least one supported resource is below its maximum.
- Target-addressed restoration of dash charges and stamina through `ExternalResourceEffect`.
- Fixed-tick cooldown, explicit respawn, disable/enable and release-to-rearm behavior.
- Stable `REFILL_*` event IDs, immutable snapshots, deterministic replay and per-entity isolation.
- Actual Player application plus Refill/Refill and Refill/Spring isolation coverage.

## Fidelity boundary

The shipped game's complete contact bounds, respawn timing and special Refill variants are not established by sufficient public facts. The current 150-tick default is an independently designed deterministic baseline. Behavior remains `partial`; no original numeric parity is claimed.

## Acceptance

1. Double-click `演示当前进度.cmd`.
2. Inspect the CDR-043 Refill table in `artifacts/cdr-043-demo/index.html`.
3. Confirm 16 ticks, 2 collections, 2 restore requests, 2 respawns and 2 actual Player applications. Player dash/stamina should change from `0 / 25` to `1 / 110` on each collection.
4. Double-click `验证当前版本.cmd`.
5. Confirm Release has 0 warnings/errors, Refill reports `42/42`, total regressions are `531/531`, and the final line is `CDR-043 OFFLINE VERIFICATION PASSED`.

This proves generated contact, resource routing, actual Player application, lifecycle and isolation. It does not prove formal App routing, original numeric parity, original pixels, visible desktop behavior, live input or installation access.
