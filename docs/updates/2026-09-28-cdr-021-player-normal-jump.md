# CDR-021 update — generated Normal/Jump behavior

CDR-021 adds a platform-independent player controller for run, friction, air control, gravity, fast fall, coyote time, jump buffering and variable jump. It consumes generated input snapshots only and emits immutable tick snapshots and explicit movement events.

The cumulative offline demo now includes a 24-tick run-and-jump trace and an identical replay check. Thirty focused cases bring the complete offline regression count to 249 with a clean Release build.

This is new offline player behavior. Its fidelity state is `partial`: publicly verifiable values and order are covered, while complete shipped-build behavior, live input, rendering and desktop integration remain outside this task. CDR-021 remains local pending developer acceptance.
