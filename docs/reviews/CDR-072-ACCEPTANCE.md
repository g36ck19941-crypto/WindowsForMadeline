# CDR-072 Acceptance

State: implementation complete and ready for developer acceptance; not uploaded.

## Delivered

- First horizontal wall collision stores nonzero incoming speed for four fixed ticks.
- Clearing the blocking side restores the stored speed.
- Opposite input cancels retention; continuous blocking expires it.
- Ordinary jump and external velocity effects cancel stale retention before applying new motion.
- Immutable snapshots expose retained speed/ticks and stable events expose each outcome.
- Seven generated cases and the cumulative demo cover retain, stability, restore, reverse/jump/external-launch cancellation and expire.

## Evidence

- Release solution build: 0 warnings, 0 errors.
- Player focused suite: 45/45.
- Complete regression set: 878/878.
- Generated demo: retained speed `90`, initial window `4`, restored speed `90`, one retained event, one restored event, deterministic replay `true`.
- Game/GUI launch: none. Installation access/write: none. Tracked local reference or commercial content: none.

## Acceptance procedure

1. Double-click `演示当前进度.cmd`.
2. Find CDR-072 in the Player explanation and confirm retained speed `90`, window `4`, restored speed `90`, and `WallSpeedRetained/Restored=1/1`.
3. Double-click `验证当前版本.cmd`; expect 0 build warnings/errors, Player 45/45 and total 878/878.
4. Confirm no game or visible product GUI starts.

## Project role and limits

This prevents a brief wall touch from permanently discarding horizontal momentum, making movement around wall edges more continuous and diagnosable. It does not establish complete Player parity; corner correction, one-way platforms, ducking, holdable modifiers, special environments, audio, animation and human-feel evaluation remain separate work.
