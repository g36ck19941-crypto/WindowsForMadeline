# 架构说明（中文镜像）

> 英文规范：`docs/ARCHITECTURE.md`。如有差异，以英文版为准。

## 1. 依赖方向

```text
App ───────────────┬─> Install
                   ├─> AssetWorker.Client ─IPC─> AssetWorker.Process
                   ├─> Desktop
                   ├─> Animation ──> Rendering
                   ├─> Rendering
                   └─> Simulation + Entity modules

Install ─────────────> Contracts
AssetWorker ─────────> Contracts
Desktop ─────────────> Contracts
Animation ───────────> Contracts + Rendering
Rendering ───────────> Contracts
Entity modules ──────> Simulation.Core
Simulation.Core ─────> Contracts
Contracts ───────────> nothing
```

禁止循环依赖。Simulation 不能接触文件路径、位图、HWND、WPF/WinForms 类型、时钟或操作系统 API。

## 2. 进程边界

`CelesteDesktop.AssetWorker` 是单独运行、权限尽可能小的进程。它只接收已选择的根目录和允许列表中的请求，通过带版本的 IPC 信封返回不可变描述或受大小限制的 BGRA32 数据。

Worker 必须：

- 只读打开源文件；
- 将所有路径规范化并限制在已批准根目录内；
- 不跟随重解析点；
- 限制文件、条目、尺寸和解压预算；
- 支持请求超时和取消；
- 绝不写入游戏目录；
- 失败时返回结构化错误，不发布部分成功结果；
- 不发送任意源文件或未请求的商业字节。

如果 Worker 退出、超时或违反协议，App 将其视为 `asset_unavailable`；模拟和设置功能仍应可用。

## 3. 数据边界

### 素材合同

- `AssetSourceFingerprint`：受支持游戏配置及必需源文件的长度/哈希摘要。
- `AtlasEntryDescriptor`：规范 ID、页面、裁剪矩形、未裁剪帧及已验证原点数据。
- `Bgra32Frame`：宽、高、步幅、不可变像素数据和内容指纹。
- `AnimationDescriptor`：规范动作、顺序帧 ID、延迟、循环/跳转语义和明确元数据。
- `EntityAssetCatalog`：按实体划分的允许素材，不把所有名称压平成全局字典。

### 模拟合同

- 不可变 tick 输入；
- 明确的角色/实体状态；
- 带运动分类的有序 Solid 快照；
- 语义化视觉和声音事件；
- 不直接操作纹理、音频或桌面。

## 4. 更新与渲染流程

```text
桌面采集（受限频率） ──> 不可变世界快照
输入快照 ─────────────> 固定 60 Hz 调度器
世界 + 输入 ──────────> 确定性模拟 tick
模拟快照 ─────────────> 动画选择
已验证素材目录 ───────> 帧解析
呈现快照 ─────────────> 渲染器
渲染器 ───────────────> DirectComposition Present
```

渲染延迟不能反向影响模拟时间。UI 调度器的时间不能推进动画或实体状态。

CDR-030 又把呈现拆成两层：`CelesteDesktop.Rendering` 负责不可变帧验证、有序健康事件、指纹变化判断和有界恢复；`CelesteDesktop.Rendering.Windows` 只负责隐藏 HWND、D3D11 和 DirectComposition COM 资源。Windows 后端不能依赖 Simulation、Player、磁盘素材或实时输入。DirectComposition 提交完成不等于人眼可见。

CDR-032 在已验证目录与 Rendering 之间加入 `CelesteDesktop.Animation`。它只消费调用方提供的固定 tick 和不可变目录帧，处理循环/末帧/直接 goto，合成受限透明画布，再单向提交给 Rendering。它不引用 Simulation、Desktop、文件系统或实时输入，因此渲染耗时和呈现失败都不能推进或改写物理。

## 5. 实体隔离

每个实体模块公开自己的状态、确定性更新、碰撞响应和语义效果。跨实体行为通过窄交互合同表达，App 不得通过类型判断集中处理。某个可选实体禁用或失败时，不能改变 Player 规则，也不能阻止其他实体加载。

CDR-040 用 `CelesteDesktop.Entity.Theo` 落实这条规则。模块只依赖 `Simulation.Core`，接收不可变的持有者/动作快照，并自行拥有状态和事件。Player 交互通过测试中的请求/快照矩阵验证，不建立 Player 到 Theo 的项目依赖。某个 Theo 被夹坏后只停止自己的控制器，Player 和另一个 Theo 仍可继续。

## 6. 预留扩展接口

`docs/EXTENSIONS.md` 预留 `IAssetSourceProvider` 和 `IWorldContentProvider` 概念边界，使未来可以接入只读 Mod 素材和规范化关卡/地图描述，而不让这些格式耦合到 Simulation 或 App。

当前只定义架构合同，不实现提供器接口、插件加载器、地图解析器或 Mod 读取器。

## 7. 当前阶段不做的内容

- 当前阶段的关卡、房间、剧情或地图加载；
- 任意 Everest 运行时钩子、可执行 Mod 代码或代码驱动皮肤；
- 自动打包或重新分发 Celeste 素材；
- 实时捕获游戏进程；
- 没有证据记录却宣称完整还原。
