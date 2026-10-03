# CDR-075 — Ducking and Safe Rise

Owner: Primary. Generated-only offline implementation; acceptance pending.

The Player can reduce its height while keeping its feet anchored, slow while ducking, and refuse standing when a Solid occupies its headroom. Release or ordinary jump checks full standing clearance first. Immutable snapshots and three stable events expose the actual shape and blocker.

Core gains one transactional registered-actor height operation. Six Core and thirteen Player cases bring focused totals to 48/48 and 72/72; the full Release gate passes 920/920. The nine-tick demo slides into a low tunnel, refuses rise twice, then restores standing after the ceiling moves. Heights 6/11, feet y=11, events 1/2/1, no overlap, identical replay. Both cmd entries pass without report opening or pause.

CDR-074 exact accepted commit `9e07d966e8398eb011536d87fa5af95e5f7e4ce3` was freshly reverified at 901/901, audited and published/read back at `codex/cdr-074-player-one-way-platforms`. Main remains `d237277e10af090cf60ec015c22174565e6cdc0a`.

No game, visible GUI, live input, installation/local-reference access or commercial bytes. Ducking remains partial; dash/climb integration and commercial offsets/feel are deferred. CDR-075 is not uploaded. CDR-076 is a proposal only; CDR-060/CDR-061 gates remain unchanged.
