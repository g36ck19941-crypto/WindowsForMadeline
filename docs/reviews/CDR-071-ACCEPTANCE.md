# CDR-071 Acceptance

State: implementation complete and ready for developer acceptance; not uploaded.

## Delivered

- Ordinary Player jumps inherit bounded velocity from a moving Solid.
- Horizontal lift is clamped to `[-250,250]` units/s.
- Upward lift is clamped to `-130` units/s; downward lift is not added.
- The immutable Player snapshot exposes the applied lift and emits one stable `LiftVelocityApplied` diagnostic for a nonzero vector.
- Five generated cases and the cumulative generated demo cover zero, inherited, clamped and rejected lift.

## Evidence

- Release solution build: 0 warnings, 0 errors.
- Player focused suite: 38/38.
- Complete regression set: 871/871.
- Generated moving-Solid demo: applied lift `(250,-130)`, event count `1`, deterministic replay `true`.
- Game/GUI launch: none. Installation access/write: none. Tracked local reference or commercial content: none.

## Acceptance procedure

1. Double-click `演示当前进度.cmd`.
2. In the generated report, find the CDR-071/Player explanation and confirm it reports applied lift `(250,-130)` and `LiftVelocityApplied=1`.
3. Double-click `验证当前版本.cmd`; expect 0 build warnings/errors, Player 38/38 and total 871/871.
4. Confirm no game or visible product GUI starts.

## Project role and limits

This makes a jump from a moving surface carry that surface's momentum instead of feeling detached. It also makes the exact carried velocity visible in tests and diagnostics. It does not establish complete Player parity: ducking, held-object modifiers, environment variants, special jumps, audio, animation and human-feel evaluation remain separate work.
