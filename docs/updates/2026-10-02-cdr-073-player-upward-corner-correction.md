# CDR-073 ready for acceptance: Player upward corner correction

Added deterministic upward-only corner correction to Normal Player movement. When an upward move is blocked, the controller searches no farther than four integer pixels, checks the current horizontal travel direction first, applies one explicit horizontal correction and retries only the remaining rise. Downward collisions never use this rule.

`Actor.MoveXExact` provides the minimal Simulation.Core support needed to preserve an existing subpixel remainder while applying the integer correction. The Player snapshot exposes the applied horizontal offset and `UpwardCornerCorrected` identifies the original blocking Solid. One new core case and five Player cases cover midpoint remainder preservation, left/right correction, direction preference, the four-pixel bound and downward exclusion.

Release builds with zero warnings/errors. Simulation.Core passes 34/34, Player passes 50/50 and all 884 regressions pass. The cumulative program-generated demo moves from `(0,4)` to `(1,2)`, reports correction `+1`, emits one correction event, preserves upward speed and replays identically.

No game, GUI, live input, installation access or tracked reference content was used. Fidelity remains `partial`: this task does not establish downward or dash corner correction, one-way platforms, crouching, carry-specific movement or complete original feel. CDR-073 remains local and unuploaded before developer acceptance.
