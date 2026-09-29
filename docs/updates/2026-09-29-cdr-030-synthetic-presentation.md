# CDR-030 update — hidden DirectComposition presentation

CDR-030 adds a platform-neutral presentation controller and a Windows D3D11/DirectComposition backend for generated premultiplied BGRA32 frames. Upload, submit, commit completion and changed-pixel evidence are separate events; recoverable device loss is bounded to one recreation/retry with full exception detail.

The native hidden test presented two generated frames through a never-shown HWND, D3D11, `IDCompositionSurface`, `Commit` and `WaitForCommitCompletion`. The rendering suite passes 20/20 and all 318 regressions pass with 0 build warnings/errors.

The cumulative report now shows a generated presentation checkerboard, restores the separately passed CDR-016 milestone and explains that CDR-017 through CDR-019 are unassigned reserved numbers. No commercial bytes, real installation access, visible GUI, desktop observation or human-visibility claim is included.
