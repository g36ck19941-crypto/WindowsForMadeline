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
