# CDR-082 — Actual original time/input bridge plan

2026-10-03, Primary. Cached-source inspection only. No original execution, compilation retry, installation/assets read or original-source changes this turn. Own adapter/framework preparations remain implemented; this document is a proposed actual binding strategy, NOT an implemented or verified bridge.

## Concrete source observations

The chosen recovery has public static Input axes/buttons and MInput keyboard state fields. Engine delta/raw-delta/frame-counter setters are private. MInput keyboard instance setter and its initializer/virtual-input update helper are nonpublic. Binding queries use stored keyboard/gamepad states; VirtualButton implements original buffer/consumption/repeat logic. VirtualIntegerAxis is a concrete binding-backed class, not a pluggable injected-node abstraction. Neither its existence nor our own StepContext establishes a connection.

MInput's device update methods poll real keyboard/mouse/gamepad state. Its initialization and virtual-input updating are separate methods. This gives a possible state-injection seam but is NOT a proven no-device-access call graph. Engine.Scene accesses Engine.Instance; an arbitrary empty scene does not replace Level/session requirements. Input initialization requires Settings state. No original code excerpts are included.

## Proposed binding order, without gameplay rewriting

1. **Input/time only, no Player:** prepare a bounded original input context using explicit synthetic settings/bindings, set original delta counters through exact validated nonpublic setters, inject previous/current in-memory keyboard states, invoke only audited original virtual-input update logic. Preserve original button buffering/consumption; do not emulate it in our adapter.
2. Before invocation, audit initialization and the selected method/field graph for native/input/file/Steam/window calls, static constructors and type/member signatures. Private access must fail if signatures change; never fallback to guessed rules or the ordinary engine loop. Record the initial-state/cleanup obligations and single-use, process-isolated test plan. Private-member existence is not runtime compatibility proof.
3. Compare original observed axis/button/time state against a fixed fixture, including press/hold/release and original consumption. Do not report adapter replay as original output. This stage must return original-bound evidence only after actual authorized observation.
4. Separately resolve Level/session/assist and graphics-resource construction before Player. Resource reading/offscreen rendering/Player stepping remain later gates, not included in this input-only test.

The proposed route can be investigated without changing original behavior source, but its practical feasibility remains unverified. If initialization cannot be isolated, stop and report; do not edit original code silently or start the game.

## Forbidden shortcuts

No Celeste entry, Engine constructor/game loop, ordinary device polling, vibration, FMOD/audio, Steam APIs, graphics/window creation, saved-settings loading, installation writes, downloads or new assets. Do not substitute default no-op values to call the original input test a success. No runtime detours/source transformations are authorized.

## Permission needed for the next actual test

The current developer permission says not to execute recovered code. A narrow original input/time smoke test therefore requires a new explicit authorization: isolated local process, generated input, existing cached original library and already-approved dependencies only; audited input/time initialization and bounded steps; no Player creation/resources/game/GUI/real input/install access/native services. Further dependency or side-effect uncertainty must stop the run before invocation. This proposal does not grant that permission.

## Role and acceptance

This design replaces the vague instruction “connect the adapter” with concrete original state endpoints and initialization constraints. No new runnable feature or test pass is claimed here. Review the exact authorized next scope; current compile/synthetic evidence remains in the previous CDR-082 reports. Acceptance/upload for those deliveries remains pending. Original core/resource/frame/desktop outcome is unchanged and incomplete.
