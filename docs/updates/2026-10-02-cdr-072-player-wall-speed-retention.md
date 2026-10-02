# CDR-072 ready for acceptance: Player wall-speed retention

Added a deterministic four-tick horizontal wall-speed retention window. A first collision stores the incoming speed once; a cleared wall restores it, opposite input cancels it, and continuous blocking expires it without silently recreating the window in the same tick.

Immutable snapshots expose retained speed and remaining ticks. Stable events distinguish retain, restore, cancel and expire. Jump and external velocity effects cancel stale retention, preventing a later restore from overwriting an entity launch. Seven generated cases bring Player to 45/45 and the complete Release gate to 878/878. The cumulative demo retains speed 90 for four ticks and restores 90 with one retain/restore event each.

No game, GUI, live input, installation access or tracked local reference content was used. Player fidelity remains `partial` and the task is not uploaded before acceptance.

The developer accepted CDR-072 on 2026-10-02. Exact commit `cc77d1dd1c725ab37750cc0328cf1f2f8ead47f8` passed a fresh Release gate with Player 45/45, 878/878 total regressions and an outbound audit with zero forbidden paths, binary diffs, large blobs, real-install paths or diff-check findings. The first two pushes timed out on GitHub TCP 443; the third exact push succeeded and was read back at `codex/cdr-072-player-wall-speed-retention`. Remote `main` remained `d237277e10af090cf60ec015c22174565e6cdc0a`, and the branch contains no CDR-073 work.
