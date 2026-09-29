# CDR-032 离线素材动画呈现

CDR-032 把已经验证、不可变的 `EntityAssetCatalog` 接到确定性动画选帧、透明画布合成和现有 Rendering 呈现器。整个过程只在内存中离线运行。

## 新增功能

- 根据严格递增的固定 60 Hz tick 选择目录帧。
- 支持循环、非循环末帧停留、直接 `goto` 跳转及跳转次数上限。
- 只有请求的动画状态改变时才重置计时。
- 按精灵原点、可选位置和水平翻转，把帧放到受限透明 BGRA32 画布。
- 分别记录 `ANIMATION_STATE_SELECTED`、`ANIMATION_STATE_TRANSITIONED`、`ANIMATION_FRAME_RESOLVED` 和 `ANIMATION_FRAME_COMPOSED`。
- 失败记录稳定错误码、阶段、异常类型/消息/HResult/stack/inner 和恢复动作。
- 合成后的不可变帧单向交给 Rendering；动画和渲染都不引用或修改模拟状态。

## 验证证据

- Release 构建：0 警告、0 错误。
- Animation 专项：28/28；总回归：365/365。
- 累计程序生成演示：1 个经过解析和目录化的双帧 player 动画，8 个固定 tick、8 次离线 Present、3 次合成帧变化，重复运行完全一致。
- 可见 GUI、实时输入、游戏/安装访问、安装目录写入、商业素材持久化：0。
- `human_visible=false`；离线 Present 不等于人眼可见验收。

## 具体验收方式

1. 双击 `演示当前进度.cmd`。
2. 查看 CDR-032 表格：tick 0–2 应使用 `demo/player/idle00`，tick 3–5 使用 `idle01`，tick 6–7 循环回 `idle00`；页面应显示 8 次 Present、3 次像素变化和确定性重放。
3. 确认页面说明所有像素均由程序生成，并明确渲染不会反向驱动物理。
4. 双击 `验证当前版本.cmd`。Animation 专项应为 `28/28`，最后应显示 `CDR-032 OFFLINE VERIFICATION PASSED`。

两个入口都不读取 Celeste 安装。演示入口由开发者运行时只会打开生成的本地 HTML；验证入口不会打开可见 GUI。

## 它在项目中的作用

简单说，这一步接通了“已经安全解析出动画帧”和“渲染器在正确 tick 使用正确帧”之间的桥。数据只从模拟快照流向动画和渲染，因此即使渲染变慢，也不会改变移动或碰撞结果。

它还没有在桌面显示原版 Madeline，不保存商业帧，不读取实时输入，也没有证明人眼可见。真正角色显示仍需要后续 App 组装和单独授权的可见验收。

## 验收后开始的功能

CDR-032 验收后才允许上传。下一项计划是 CDR-040 Theo Crystal 独立实体模块，需要单独授权；验收 CDR-032 不会自动授权可见 GUI。
