# Player One-Way Platform Calibration

## CDR-074 scope

CDR-074 adds static one-way-platform geometry to Simulation.Core and connects it to Player Normal movement. A one-way platform is ignored by horizontal and upward movement. Downward movement blocks only when the actor approaches the platform top from above; touching the top completes landing in that same fixed tick.

Player may receive an explicit generated `DropThroughPressed` input while standing only on a one-way platform. It records the selected platform ID, applies a minimum downward speed and ignores that platform until the actor clears it or the 12-tick safety window expires. Ordinary Solids are never ignored by this state.

The immutable snapshot exposes `GroundedOneWayPlatformId`, `DropThroughPlatformId` and `DropThroughTicksRemaining`. Stable events distinguish `OneWayPlatformLanded`, `OneWayDropThroughStarted`, `OneWayDropThroughCompleted` and `OneWayDropThroughExpired`.

## Deterministic rules

- Static one-way platforms have immutable IDs and integer rectangles.
- Registration order resolves equal one-way contacts.
- Upward and horizontal movement never collide with one-way geometry.
- Downward collision requires horizontal overlap and crossing the platform top from above.
- Exact top contact lands immediately instead of leaving downward speed for another tick.
- Drop-through targets one platform ID and cannot bypass an ordinary Solid.
- Clearing the selected platform rearms drop-through; a bounded timeout is explicit rather than silent.

## Limits

This task does not implement moving one-way platforms, platform lift velocity, special platform variants, ducking, dash interactions, commercial map loading, live input, animation, audio or human-feel acceptance. The 60 px/s drop speed and 12-tick safety window are deterministic project calibration values, not a complete commercial-build parity claim. Player fidelity remains `partial`.
