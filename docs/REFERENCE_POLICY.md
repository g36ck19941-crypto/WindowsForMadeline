# Reference and Provenance Policy

## Reference priority

1. Celeste official public source files for code actually published under that repository's license.
2. Read-only facts from a user-owned local installation, converted into behavior contracts or non-commercial summaries.
3. Deterministic or differential evidence produced by approved validation tools.
4. External fan projects as secondary evidence of feasibility and edge cases only.

## Clean implementation rule

- Write a short format or behavior specification before implementation.
- Implement against that specification in this repository's own types and module boundaries.
- Do not keep an external implementation open for line-by-line translation.
- Do not copy large comments, identifiers, class layouts, tests or control flow from `desk-madeline` or other fan projects.
- Record the source and confidence for every exact claim.
- If sources disagree, mark the capability `unknown` until resolved.

`solstice23/desk-madeline` establishes that direct Atlas reading and DirectComposition presentation are feasible. It is not the code base, architecture owner or fidelity oracle for this project.

For CDR-012, the public Crunch binary-format description supplied the packer's field vocabulary, while desk-madeline was used only as secondary confirmation of the `.meta` header and stored trim-offset interpretation. The local reader, contracts, budgets, validation flow and tests were independently designed. Real-install conformance remains unknown until CDR-016.

For CDR-013, the public Monocle mirror confirms that Packer metadata resolves page names to `.data` streams, but its mirrored version does not expose the Celeste-branch run decoder. The run layout was recorded as a local synthetic contract from secondary format observations. The decoder types, budgets, validation order, immutable output and tests were independently designed; no external source was copied. Real-install conformance remains unknown until CDR-016.

For CDR-040, the official public Celeste repository does not expose the Theo Crystal entity implementation. The module therefore uses an independently designed fixed-tick baseline and verified local Actor/Solid contracts. Generated Player/Solid/entity interaction tests establish determinism and isolation, not original numeric parity; behavior remains `partial` until stronger evidence is separately authorized and recorded.

For CDR-041, the official public Celeste repository likewise does not expose the shipped Glider entity implementation. Pickup, holder fall-limit effect, flight and collision tuning are independently designed over verified local contracts. The generated Player/Solid/Theo/two-Glider matrix establishes determinism, effect separation and failure isolation only; behavior remains `partial`.

For CDR-042, original commercial Spring contact rules, timing and launch values are not established by sufficient public facts. Four-direction effects and lifecycle timing are independently designed over the verified fixed-tick and generic-motion contracts. The generated Player/Theo/Glider/two-Spring matrix establishes deterministic routing and isolation only; behavior remains `partial`.

For CDR-043, original commercial Refill contact bounds, respawn timing and special variants are not established by sufficient public facts. Resource restoration and the fixed-tick lifecycle are independently designed over the verified Player resource and isolation contracts. The generated Player/two-Refill/Spring matrix establishes deterministic routing and isolation only; behavior remains `partial`.

For CDR-044, original commercial Water surface rules, contact thresholds, drag, buoyancy, swim tuning and special variants are not established by sufficient public facts. Rectangular overlap and motion tuning are independently designed over verified fixed-tick geometry and generic velocity contracts. The generated Player/two-Water/multiple-occupant/Refill matrix establishes deterministic routing and isolation only; behavior remains `partial`.

For CDR-045, original commercial Bumper contact radius, radial launch speed, cooldown duration, moving path, special variants, animation and audio are not established by sufficient public facts. Circular contact, deterministic direction normalization, coincident-center fallback and lifecycle timing are independently designed over verified fixed-tick and generic velocity contracts. The generated Player/Solid/two-Bumper matrix establishes deterministic routing and isolation only; behavior remains `partial`.

For CDR-046, original commercial Puffer swim path, proximity/explosion bounds, warning and respawn timing, launch tuning, special interactions, animation and audio are not established by sufficient public facts. Bounded horizontal swim, target locking, deterministic radial direction, coincident-center fallback and lifecycle timing are independently designed over verified fixed-tick and generic velocity contracts. The generated Player/Solid/two-Puffer matrix establishes deterministic routing and isolation only; behavior remains `partial`.
