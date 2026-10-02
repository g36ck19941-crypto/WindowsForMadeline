# Player Upward Corner Correction Calibration

## CDR-073 scope

CDR-073 calibrates one Normal-movement collision rule. When an upward move is partially blocked, Player searches no farther than four integer pixels to either side. The current horizontal travel direction is checked first; if a candidate horizontal path and the unfinished upward path are clear, Player applies exactly one horizontal correction and retries only the remaining rise.

`Actor.MoveXExact` is the minimal Simulation.Core primitive for that correction. It moves a requested integer amount without consuming or resetting the existing horizontal subpixel remainder, still obeys Solid collision order and remains valid only inside an active fixed simulation step.

The immutable Player snapshot exposes `UpwardCornerCorrectionX`. A nonzero result emits exactly one `UpwardCornerCorrected` event carrying the original blocking Solid ID. A successful correction does not also emit `VerticalBlocked` and does not cancel upward speed or the variable-jump window.

## Deterministic rules

- Search distance is bounded to `1..4` pixels.
- Direction preference is the sign of current horizontal speed; if speed is zero, nonzero horizontal input is preferred, otherwise facing is used.
- Both the horizontal path and every remaining upward pixel must be clear before movement is applied.
- Downward collision never invokes this rule.
- Failure to find a valid correction preserves ordinary vertical-block behavior.

## Limits

This task does not implement downward or dash corner correction, one-way platforms, ducking, holdable movement modifiers, special environments, audio, animation or human-feel acceptance. The four-pixel project rule is bounded behavior evidence, not a claim that every commercial-build edge case is exact. Player fidelity remains `partial`.
