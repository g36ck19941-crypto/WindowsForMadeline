# CDR-074 ready for acceptance: Player one-way platforms

Added immutable static one-way-platform geometry to Simulation.Core and a filtered Player vertical-movement path. Upward and horizontal movement pass through. Downward movement lands only from above, including exact top contact in the same tick. Registration order remains deterministic and ordinary Solids keep their existing authority.

Player can enter an explicit platform-addressed drop-through state. The selected platform is ignored until Player clears it or a 12-tick safety window expires; ordinary Solids are never ignored. Immutable snapshots expose standing/drop-through identity and time remaining. Stable events distinguish landing, start, completion and expiration.

Eight generated core cases and nine Player cases bring Simulation.Core to 42/42, Player to 59/59 and the full Release gate to 901/901 with 0 warnings/errors. The cumulative generated demo passes upward, drops from y=0, lands on the lower platform at y=19, records start/completion/landing `1/1/1`, rearms and replays identically.

No game, GUI, live input, installation/local-reference access or commercial bytes were used. Fidelity remains `partial`: moving/special one-way platforms, ducking, dash interaction, original maps, commercial numeric parity and human-visible desktop behavior remain unverified. CDR-074 is local and unuploaded pending developer acceptance.
