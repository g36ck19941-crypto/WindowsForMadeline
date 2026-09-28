# CDR-022 update — generated dash, wall and climb behavior

CDR-022 adds a deterministic traversal state layer for eight-way dash, charge/cooldown/refill/attack windows, wall slide and jump, climb movement, stamina, tired/depleted states, climb jump, ledge hop and isolated infinite-resource assists.

The new 24-tick cumulative trace demonstrates a wall slide, wall jump, blocked dash and climb transition with decreasing stamina and identical replay. Forty-four traversal cases plus five new Simulation.Core cases bring the complete offline regression count to 298 with a clean Release build.

The trace also exposed and fixed zero-speed midpoint jitter in the subpixel accumulator. Fidelity remains `partial`; GUI, live controls, rendering, real-install access and unsupported edge mechanics are excluded. CDR-022 remains local pending developer acceptance.
