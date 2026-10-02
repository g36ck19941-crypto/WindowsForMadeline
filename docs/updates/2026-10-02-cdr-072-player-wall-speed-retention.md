# CDR-072 ready for acceptance: Player wall-speed retention

Added a deterministic four-tick horizontal wall-speed retention window. A first collision stores the incoming speed once; a cleared wall restores it, opposite input cancels it, and continuous blocking expires it without silently recreating the window in the same tick.

Immutable snapshots expose retained speed and remaining ticks. Stable events distinguish retain, restore, cancel and expire. Jump and external velocity effects cancel stale retention, preventing a later restore from overwriting an entity launch. Seven generated cases bring Player to 45/45 and the complete Release gate to 878/878. The cumulative demo retains speed 90 for four ticks and restores 90 with one retain/restore event each.

No game, GUI, live input, installation access or tracked local reference content was used. Player fidelity remains `partial` and the task is not uploaded before acceptance.

The developer accepted CDR-072 on 2026-10-02. Exact commit `cc77d1dd1c725ab37750cc0328cf1f2f8ead47f8` passed a fresh Release gate with Player 45/45, 878/878 total regressions and an outbound audit with zero forbidden paths, binary diffs, large blobs, real-install paths or diff-check findings. Two pushes to `codex/cdr-072-player-wall-speed-retention` failed because GitHub TCP 443 timed out, so publication remains pending and no remote SHA is claimed.
