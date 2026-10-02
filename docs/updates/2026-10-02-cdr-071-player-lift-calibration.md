# CDR-071 ready for acceptance: Player moving-platform jump inheritance

After accepting CDR-070, the developer authorized the next development step. Because CDR-060 remains separately acceptance-pending, CDR-061 is not started. The additive behavior-calibration lane begins with one bounded Player Normal/Jump rule: moving-Solid lift velocity inheritance.

Only project-owned behavioral facts, implementation, tests and summaries may be tracked. Local reference source and paths remain excluded.

The implementation now applies horizontal lift up to `250` units/s, upward lift up to `130` units/s and rejects downward lift. The immutable snapshot and `LiftVelocityApplied` diagnostic expose the applied vector. Player tests pass 38/38 and the full Release gate passes 871/871. The generated demo deterministically reports `(250,-130)` and one event. The task remains unuploaded pending developer acceptance.
