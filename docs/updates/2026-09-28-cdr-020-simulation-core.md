# CDR-020 update — deterministic Actor/Solid kernel

CDR-020 adds a platform-independent fixed-60-Hz simulation core with whole-pixel bounds, decimal subpixel remainders, ordered per-pixel collision, moving-Solid carry/push, LiftSpeed, explicit squish events and immutable snapshots.

The cumulative generated-data demo now includes a 13-tick Actor/Solid trace and verifies an identical replay. This is new offline runtime behavior, but it is not Madeline behavior parity and does not render a character.

No game, GUI, real desktop, installation, asset or live input is involved. The task remains local pending developer acceptance.
