# CDR-075 Acceptance

State: local implementation; developer acceptance and publication pending.

## Added functionality

- Registered-actor, active-step, transactional height resizing that preserves feet, width and subpixel remainders and rejects Solid overlap.
- Explicit Player Normal duck state, grounded friction, safe unduck clearance, safe ordinary-jump ordering and immutable shape/blocker evidence.
- Stable `DuckStarted`, `UnduckBlocked`, `UnduckCompleted` facts.
- Six Core and thirteen Player regression cases; a cumulative nine-tick generated demo with a dedicated plain-language section and per-tick table.

## Role in the project

The Player can now fit beneath a low ceiling without moving its feet, and cannot stand into an obstacle. This supplies body-size and headroom rules for later geometry and animation integration. It does not prove complete original feel or visible desktop behavior.

## Verification

Core 48/48; Player 72/72. The complete Release gate passed 920/920 with zero warnings/errors. The demo reports heights 6/11, feet y=11 on all nine ticks, blocked rise on ticks 7/8, standing recovery on tick 9, events 1/2/1, no overlap and identical replay. Both root launchers exited successfully with `CDR_NO_PAUSE=1` and `CDR_DEMO_NO_OPEN=1` under Windows PowerShell without showing GUI.

## Concrete acceptance

1. Double-click `演示当前进度.cmd` in the project root.
2. Inspect the CDR-075 dedicated section and nine-row trajectory. Confirm the values above and read its limits.
3. Double-click `验证当前版本.cmd`; require `CDR-075 OFFLINE VERIFICATION PASSED` and the complete passing counts.
4. Report a failing test name and exception or the last verification error if anything differs.

## Function after acceptance

Publish only the explicitly accepted CDR-075 commit to an independent branch after fresh verification/audit. Proposed next work is CDR-076 Dash one-way-platform contact and landing integration, which is not authorized yet. CDR-060 remains separately acceptance-pending; CDR-061 remains unauthorized.

## Boundaries

Generated inputs/geometry only. No game, visible product GUI, live input, install/local-reference access or commercial bytes. Developer-initiated demo may open the generated local HTML. CDR-075 does not calibrate dash/climb duck interactions, special platforms, original offsets, assets/audio or human visibility; fidelity stays partial.
