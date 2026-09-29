# CDR-032 Offline Asset-to-Animation Presentation

CDR-032 connects a validated immutable `EntityAssetCatalog` to deterministic animation selection, transparent-canvas composition and the existing isolated Rendering presenter. It operates entirely in memory and offline.

## Added behavior

- Resolves catalog frames from fixed, monotonically increasing 60 Hz ticks.
- Supports looping, final-frame hold, direct `goto` transitions and a bounded transition chain.
- Resets timing only when the requested animation state changes.
- Applies sprite origin, optional position and horizontal flip onto a bounded transparent BGRA32 canvas.
- Emits separate `ANIMATION_STATE_SELECTED`, `ANIMATION_STATE_TRANSITIONED`, `ANIMATION_FRAME_RESOLVED` and `ANIMATION_FRAME_COMPOSED` facts.
- Records structured failure code, stage, exception type/message/HResult/stack/inner and recovery action.
- Passes immutable composed frames to Rendering; neither animation nor rendering references or mutates simulation.

## Verification evidence

- Release build: 0 warnings, 0 errors.
- Animation suite: 28/28; total regression: 365/365.
- Cumulative generated demo: one parsed/catalogued two-frame player animation, 8 fixed ticks, 8 offline presents, 3 changed composed frames and identical replay.
- Visible GUI, live input, game/install access, installation writes and persisted commercial bytes: 0.
- `human_visible=false`; offline presentation is not human-visible acceptance.

## Manual acceptance

1. Double-click `演示当前进度.cmd`.
2. Inspect the CDR-032 table. Ticks 0–2 must use `demo/player/idle00`, ticks 3–5 use `idle01`, and ticks 6–7 loop to `idle00`; the report must show 8 presents, 3 pixel changes and deterministic replay.
3. Confirm the report labels all pixels as program-generated and says rendering does not drive physics.
4. Double-click `验证当前版本.cmd`. The Animation suite must report `28/28` and the final line must be `CDR-032 OFFLINE VERIFICATION PASSED`.

Neither launcher reads the Celeste installation. The demo launcher opens only its generated local HTML when run by the developer; the verification launcher opens no visible GUI.

## Role in the project

This task is the bridge between “we decoded safe animation frames” and “the renderer can show the correct frame at the correct simulation tick.” The bridge accepts snapshots in one direction only, so a slow renderer cannot change movement or collision results.

It does not display the original Madeline on the desktop, persist commercial frames, read live input or prove visible behavior. Real character presentation still needs later App assembly and a separately authorized visible acceptance session.

## Next gate

The developer accepted CDR-032 on 2026-09-29 and authorized independent-branch publication after a fresh gate and outbound audit. CDR-040 Theo Crystal is separately authorized for generated offline geometry/input and verified contracts only; visible GUI and installation access remain forbidden.
