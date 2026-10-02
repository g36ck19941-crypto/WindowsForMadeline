# CDR-073 Acceptance

State: accepted by the developer on 2026-10-02; exact independent-branch publication authorized after fresh verification and outbound audit.

## Delivered

- Upward Normal movement searches at most four integer pixels around a blocking corner.
- Current horizontal travel direction is preferred deterministically before the opposite side.
- Exact horizontal correction preserves the existing subpixel remainder and retries only the unfinished rise.
- Downward collision is excluded, and an uncorrectable ceiling preserves ordinary block behavior.
- Immutable snapshots expose the applied offset; `UpwardCornerCorrected` identifies the original blocking Solid.
- One generated Simulation.Core case and five generated Player cases cover the new behavior and boundaries.

## Evidence

- Release solution build: 0 warnings, 0 errors.
- Simulation.Core focused suite: 34/34.
- Player focused suite: 50/50.
- Complete regression set: 884/884.
- Generated demo: `(0,4)` to `(1,2)`, correction `+1`, one correction event, upward speed preserved, deterministic replay `true`.
- Game/GUI launch: none. Installation access/write: none. Tracked local reference or commercial content: none.

## Acceptance procedure

1. Double-click `演示当前进度.cmd`.
2. Find CDR-073 in the Player explanation and confirm `(0,4)` to `(1,2)`, correction `1`, `UpwardCornerCorrected=1`, and preserved upward speed.
3. Double-click `验证当前版本.cmd`; expect 0 build warnings/errors, Simulation.Core 34/34, Player 50/50, total 884/884 and the final CDR-073 pass marker.
4. Confirm no game starts; only the developer-triggered generated HTML report may open.

## Project role and limits

This prevents a one-pixel platform edge from stopping an otherwise valid rise and makes the correction observable instead of hidden. It supplies a precise correction primitive for later movement calibration. It does not establish downward or dash correction, one-way platforms, ducking, holdable modifiers, special environments, audio, animation or complete original feel.

## After acceptance

Publish only the freshly verified and audited CDR-073 acceptance commit to its independent branch. CDR-074 is now authorized as a generated-only one-way-platform calibration: pass-through from below, landing/standing from above and explicit bounded drop-through state. Visible GUI, live input, game/install or local-reference access, commercial bytes and CDR-061 remain forbidden.
