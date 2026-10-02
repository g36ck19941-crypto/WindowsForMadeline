# Player Normal/Jump Calibration

## CDR-071 scope

CDR-071 calibrates one missing Normal/Jump rule: a jump from a moving Solid inherits a bounded part of the Solid-carried Actor lift velocity.

The project-owned rule is:

- inherit horizontal lift velocity, clamped to `[-250, 250]` units per second;
- inherit upward lift velocity, clamped to at most `130` units per second upward;
- do not add downward lift velocity to a jump;
- apply the bounded lift after the ordinary horizontal boost and vertical jump speed are selected;
- expose the actually applied lift in the immutable snapshot and emit one stable diagnostic only when the applied vector is nonzero.

This specification records behavioral facts only. Decompiled source, IL, original control-flow layout, original comments, installation paths and commercial content remain in ignored local storage and are not project inputs.

## Limits

This task does not calibrate ducking, holding modifiers, low-friction/core/space variants, jump-throughs, special jumps, audio, animation or human feel. CDR-072 separately adds one bounded wall-speed-retention rule. Player fidelity remains `partial`.
