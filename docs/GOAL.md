# Current project objective

Updated: 2026-10-03. Developer-authorized replacement for the former self-designed gameplay roadmap. This file is the project objective; it does not itself modify Codex thread goal state.

## Outcome

In C:/supermadeline/CelesteDesktopRuntime, reconstruct a local original-code-led Madeline and supported interaction-entity runtime for Windows desktop without launching Celeste/Everest. Prioritize recovering and integrating original behavior dependencies instead of creating another approximate gameplay implementation. Preserve independently verified resource parsing, animation/rendering and anonymous desktop geometry tools. Old gameplay is retired and recoverable only through Git history.

## Ordered near-term work

1. CDR-080: developer accepted old-gameplay retirement/tooling preparation on 2026-10-03; published f804168ff2655c043d60b19749151ecc6181dc03 on its independent branch.
2. CDR-081: authorized read-only identity/dependency inspection completed; original-backup candidate found, but clean provenance and dependency closure remain unproven. Its XNA references are unresolved within inspected root/orig. Developer acceptance pending before the next recovery/compile stage.
3. CDR-082: build a minimal dependency-closed original behavior core in a Git-ignored local workspace. Verify compilation feasibility and report compatibility failures; do not execute the full game entrypoint.
4. CDR-083: bind original animation/resource names to local resource readers and verify controlled offline stepping and offscreen frames. Keep behavior, asset resolution and rendering evidence separate.
5. CDR-084: implement thin desktop environment adapters for time, injected inputs, geometry and presentation without replacing original behavior with invented rules.

## Longer-term work

After the Player dependency chain works, integrate Theo Crystal, Glider, Spring, Refill, Water, Bumper, Puffer and Seeker one at a time using recovered original dependencies and isolated diagnostics. Verify creation, animation, collisions and interaction; then seek separately authorized visible desktop/input acceptance. Level maps and mod-resource extension interfaces remain future scope, not current implementation.

## Completion evidence and iteration

Success requires a verified local original behavior core, original-resource binding, reproducible offline tests, actual offscreen outputs and separately authorized visible desktop acceptance. Decompilation or compilation alone is not completion. Do not promise perfect parity. Report unsupported dependencies rather than silently substituting guessed gameplay.

Work in dependency order, maintain phase-specific logs and bilingual plain-language progress/acceptance records, and retain short recoverable Git commits. Explain each update's role and expected result. Continue within authorized offline scope; stop to ask when permissions, source choice, feasibility or scope are materially uncertain.

## Boundaries

Follow AGENTS.md and current handoff. No game/Everest/full-game entrypoint launch, visible GUI, real input, installation writes or unapproved installation reads. Cached source inspection is local only; fresh installation access must have explicit scope. Recovered source/IL, commercial assets, original binaries and commercial-derived build outputs stay in Git-ignored local storage and never enter GitHub, logs, CI, public packages or tracked sources. GitHub receives only our tools/adapters/synthetic tests/summary records after developer acceptance, on independent branches; do not update remote main without authorization.

No automatic acceptance. CDR-060 remains historically unaccepted and CDR-061 unauthorized; neither revives the retired gameplay path. If context integrity degrades, stop, record a full next-window handoff and ask the developer to change windows.
