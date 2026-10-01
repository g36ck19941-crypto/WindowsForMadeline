# CDR-047 Deterministic Seeker Entity

## Role in the project

CDR-047 adds the first multi-stage hostile entity state machine. A generated Seeker patrols, detects one eligible target, alerts, chases, winds up, dashes, reports a target-addressed hit or wall collision, becomes stunned and recovers. This provides a deterministic base for later App routing, animation and richer interaction without coupling Seeker to Player, rendering, desktop or files.

This milestone does not establish original commercial navigation, obstacle avoidance, damage/bounce behavior, timing, tuning, animation, audio or visible integration. Fidelity remains `partial`.

## Isolation and diagnostics

- Seeker owns generated patrol bounds, target lock, lost-sight grace, windup/dash direction, wall collision, stun and recovery state.
- A hit produces an immutable target-addressed fact; it does not claim Player death or application.
- A second Seeker advances independently.
- Stable events distinguish patrol, alert, chase, windup, dash, target hit, wall hit, stun, recovery, target loss, disable/enable and ignored targets.

## Manual acceptance

1. Double-click `演示当前进度.cmd`.
2. Open `artifacts\cdr-047-demo\index.html` and inspect the CDR-047 Seeker table.
3. Confirm one target hit, one wall hit, two stun/recovery cycles, one target-loss return and an independently patrolling second Seeker.
4. Double-click `验证当前版本.cmd`.
5. Confirm Release has 0 warnings/errors, Seeker reports `66/66`, total regressions are `750/750`, and the final line is `CDR-047 OFFLINE VERIFICATION PASSED`.

These steps prove only generated offline state behavior. They do not prove original assets, exact commercial behavior, Player death, formal App routing, live input or human-visible desktop output.

## Next gate

CDR-047 was accepted and exact commit `14a5142` was published and read back at `codex/cdr-047-seeker`. No later entity or visible/runtime integration may start automatically without new authorization.
