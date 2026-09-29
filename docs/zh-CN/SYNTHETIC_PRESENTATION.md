# CDR-030 合成 DirectComposition 呈现

CDR-030 新增隔离的呈现层，输入是不可变、预乘 Alpha 的 BGRA32 帧。平台无关控制器分别记录上传、提交、完成和像素变化；Windows 后端使用从不显示的 HWND、D3D11 纹理、`IDCompositionSurface`、`Commit` 和 `WaitForCommitCompletion`。

## 新增功能

- 程序生成棋盘格，并明确执行预乘 Alpha。
- 支持 DPI 换算和负数虚拟桌面原点，但不读取真实桌面布局。
- 分别记录 `RENDER_TEXTURE_UPLOADED`、`RENDER_SUBMITTED`、`PRESENTER_PRESENTED`、`PRESENTED_PIXELS_CHANGED`，不混称。
- 设备丢失时销毁后端、重建、最多重试一次，并记录异常类型、消息、HResult、stack、inner 和恢复动作。
- 优先使用硬件 D3D11，失败后显式回退 WARP；两者都不读取素材，也不推进模拟。

## 验证证据

- Release 构建：0 警告、0 错误。
- Rendering 专项：20/20。
- 完整离线/隐藏回归：318/318。
- 原生隐藏测试：两张程序生成帧实际完成 D3D11 上传、DirectComposition `Commit` 和 `WaitForCommitCompletion`。
- 累计演示：3 次 Present、2 次像素变化、商业素材字节 0，并且没有 `HUMAN_VISIBILITY_CONFIRMED`。

设备丢失使用可注入后端测试，因为故意破坏真实显示设备会造成干扰；原生测试负责证明真实 Windows 路径，可注入测试负责证明恢复顺序和详细诊断。

## 具体验收方式

1. 双击根目录 `演示当前进度.cmd`。
2. 确认页面明确显示 CDR-016 是已单独通过的只读验证，并说明 CDR-017 至 CDR-019 只是未分配的保留编号。
3. 查看 CDR-030 棋盘格与健康链表格：应显示 3 次 Present、2 次像素变化以及 `human_visible=false`。
4. 双击 `验证当前版本.cmd`。最后应出现 `CDR-030 HIDDEN VERIFICATION PASSED`，Rendering 专项应为 `20/20`。

两个入口都不会启动 Celeste/Everest，也不会读取安装目录。进度演示只打开本地 HTML；原生 DirectComposition 验证使用隐藏窗口，不进行真实桌面观察。

## 它在项目中的作用

简单说，这一步证明项目已经能把一张安全、不可变的图片送进真实 Windows 图形提交链，并在出错时知道具体坏在上传、提交还是完成等待。后续把已验证的角色动画帧接进来时，可以复用这一层，而且渲染快慢不会反过来改变物理模拟。

它还没有显示 Madeline，没有使用商业精灵，没有查看桌面窗口，也没有证明人眼能在桌面上看到像素。`PRESENTER_PRESENTED` 只表示 DirectComposition 已处理提交；只有另行授权的可见会话才能写入 `HUMAN_VISIBILITY_CONFIRMED`。

## 验收后开始的功能

开发者已于 2026-09-29 验收 CDR-030，可在出站审计后上传独立分支。CDR-031 已另行授权，但只允许匿名几何、DPI、可见表面与速度；可见 GUI、标题、内容、截图和输入仍然禁止。
