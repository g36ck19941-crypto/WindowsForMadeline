# CDR-072 ready for acceptance: Player wall-speed retention

Added a deterministic four-tick horizontal wall-speed retention window. A first collision stores the incoming speed once; a cleared wall restores it, opposite input cancels it, and continuous blocking expires it without silently recreating the window in the same tick.

Immutable snapshots expose retained speed and remaining ticks. Stable events distinguish retain, restore, cancel and expire. Jump and external velocity effects cancel stale retention, preventing a later restore from overwriting an entity launch. Seven generated cases bring Player to 45/45 and the complete Release gate to 878/878. The cumulative demo retains speed 90 for four ticks and restores 90 with one retain/restore event each.

No game, GUI, live input, installation access or tracked local reference content was used. Player fidelity remains `partial` and the task is not uploaded before acceptance.
