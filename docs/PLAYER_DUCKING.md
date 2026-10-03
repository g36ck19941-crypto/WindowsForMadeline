# CDR-075 — Player Ducking and Safe Unduck Clearance

## Contract

This generated-only calibration adds ducking to Player Normal movement. Explicit `DuckHeld` input starts ducking only when grounded and not requesting an ordinary jump. Downward aim is still a separate fast-fall input; ducking never implicitly requests one-way drop-through.

The controller records its initial standing height. Default duck height is `min(6, max(1, standingHeight - 1))`; the ordinary 8-by-11 generated Player becomes 8-by-6. Callers may select a positive duck height no larger than standing height. A one-pixel actor cannot reduce further and does not enter ducking.

Core `TryResizeHeightKeepingBottom` requires a registered actor and active simulation step. It constructs and validates the complete candidate rectangle before mutation, refuses ordinary Solid overlap, preserves the bottom and width, and leaves both subpixel accumulators untouched. One-way geometry does not obstruct upward expansion. Squished actors cannot resize.

While grounded and ducking, horizontal speed approaches zero at project calibration friction 500 px/s² without ordinary run acceleration. Facing still follows horizontal input. Airborne input cannot initiate ducking; an already ducked actor can retain its shape in air while held. This task does not add crawling or commercial-build numeric parity.

Release or an ordinary jump request attempts standing restoration before movement. A blocked candidate keeps the ducked rectangle, produces `UnduckBlocked` with the blocking Solid ID, and prevents an ordinary jump until standing clearance is available. Ordinary jump buffering remains bounded by existing rules. Clearance is checked at the start of each Normal tick; if movement clears a ceiling later in that tick, restoration occurs at the next Normal tick. Restoration emits `UnduckCompleted`. Duck entry emits `DuckStarted` and cancels stored wall speed; ducked collisions do not create wall-speed retention.

## Evidence and limits

Immutable Normal snapshots expose `Ducking`, `CollisionBounds` and `UnduckBlockingSolidId`. Core snapshots capture the changed height as a value. Each blocked rise attempt is recorded explicitly, including repeated attempts.

Six Core cases cover foot preservation, transactional Solid refusal, subpixel preservation, immutable snapshots, step/membership and invalid heights. Thirteen Player cases cover duck/friction, clear/blocked/repeated rise, cleared ceilings, safe/blocked jumps, airborne initiation rejection, one-way grounding, snapshot/replay and invalid settings. Focused results are Core 48/48 and Player 72/72.

The nine-tick demo slides beneath a generated ceiling, records height 6 with feet y=11, rejects rise on ticks 7 and 8, then restores height 11 on tick 9 after the ceiling moves. Entry/blocked/completion counts are 1/2/1; no overlap or foot drift occurs and replay is identical.

This remains `partial`. Dash/climb duck interactions, special platforms, original collision-box offsets and complete feel, live input, commercial animation/audio and human visibility are not established. No game, GUI, install or local-reference access is part of this task.
