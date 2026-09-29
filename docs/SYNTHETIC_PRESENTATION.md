# CDR-030 Synthetic DirectComposition Presentation

CDR-030 adds an isolated presentation layer for immutable, premultiplied BGRA32 frames. The platform-neutral controller records upload, submit, completion and changed-pixel facts separately; the Windows backend uses a never-shown HWND, D3D11 texture upload, an `IDCompositionSurface`, `Commit` and `WaitForCommitCompletion`.

## Added behavior

- Program-generated checkerboard frames with explicit premultiplied alpha.
- DPI-aware bounded pixel geometry that preserves negative virtual-desktop origins without inspecting the real desktop.
- Ordered `RENDER_TEXTURE_UPLOADED`, `RENDER_SUBMITTED`, `PRESENTER_PRESENTED` and `PRESENTED_PIXELS_CHANGED` evidence.
- Recoverable device-loss containment with backend disposal, recreation, one retry and full exception detail.
- Hardware D3D11 with an explicit WARP fallback; neither path reads assets or advances simulation.

## Verification evidence

- Release build: 0 warnings, 0 errors.
- Rendering tests: 20/20.
- Complete offline/hidden regression: 318/318.
- Native hidden smoke: two generated frames reached D3D11 upload, DirectComposition `Commit` and `WaitForCommitCompletion`.
- Generated cumulative demo: 3 presents, 2 changed-pixel events, 0 commercial bytes and no `HUMAN_VISIBILITY_CONFIRMED` event.

The device-loss tests use an injected backend because deliberately removing a real display device is disruptive. The native test verifies the real Windows path; the injected tests verify deterministic recovery and diagnostic contents.

## Manual acceptance

1. Double-click `演示当前进度.cmd`.
2. Confirm the report explicitly shows CDR-016 as a separate passed read-only validation and labels CDR-017 through CDR-019 as unassigned reserved numbers.
3. Inspect the CDR-030 checkerboard and health-chain table. It must show 3 presents, 2 pixel changes and `human_visible=false`.
4. Double-click `验证当前版本.cmd`. It must end with `CDR-030 HIDDEN VERIFICATION PASSED` and the rendering suite must report `20/20`.

Neither launcher starts Celeste/Everest or reads the installation. The progress demo opens only its local HTML report; the native DirectComposition verification itself uses a hidden window and performs no real-desktop observation.

## Role in the project

This task proves that the project can take a safe, immutable image and move it through a real Windows graphics submission path while reporting exactly where a failure occurred. It creates the rendering foundation that later connects verified character animation frames without letting rendering timing change physics.

It does not show Madeline, use commercial sprites, inspect desktop windows, or prove that a person can see pixels on the desktop. `PRESENTER_PRESENTED` means DirectComposition processed the commit; only a separately authorized visible session may produce `HUMAN_VISIBILITY_CONFIRMED`.

## Next gate

CDR-030 remains local until developer acceptance. A visible checkerboard observation, CDR-031 real desktop geometry, and any live GUI session each require explicit authorization before execution.
