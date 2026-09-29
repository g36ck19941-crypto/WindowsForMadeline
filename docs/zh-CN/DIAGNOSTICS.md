# 诊断合同（中文镜像）

> 英文规范：`docs/DIAGNOSTICS.md`。事件 ID 和字段名保持英文；如有差异，以英文版为准。

## 1. 记录格式

生产诊断日志每行写一个 JSON 对象，必需字段如下：

```json
{
  "timestampUtc": "2026-09-21T00:00:00.0000000Z",
  "runId": "uuid",
  "eventId": "ASSET_FRAME_DECODED",
  "subsystem": "AssetWorker",
  "severity": "Info",
  "stage": "decode-page",
  "outcome": "succeeded",
  "durationMs": 1.25
}
```

可选但必须受长度限制的字段包括：`requestId`、`tick`、`entityId`、`assetId`、`sequence`、尺寸、步幅、数量、指纹、进程退出码和恢复动作。

异常记录还要包含 `exceptionType`、`message`、`hresult`、`stack`、递归深度受限的 `inner`、最后完成阶段和 `recoverable`。日志不得包含原始像素、键盘输入文字、窗口标题或凭据。

## 2. 稳定健康链

以下事件分别证明不同事实，只有直接观察到对应事实时才能记录：

1. `INSTALL_SELECTED`：用户选择了目录。
2. `INSTALL_VALIDATED`：安装目录通过验证。
3. `ASSET_WORKER_READY`：素材进程就绪。
4. `ATLAS_INDEX_PARSED`：Atlas 索引解析完成。
5. `ATLAS_PAGE_DECODED`：Atlas 页面完成解码。
6. `ASSET_FRAME_DECODED`：目标帧完成解码。
7. `ASSET_VISIBLE_PIXELS_CONFIRMED`：帧包含可见像素。
8. `ASSET_CATALOG_READY`：素材目录可用。
9. `ENTITY_SPAWNED`：实体已在模拟中生成。
10. `SIMULATION_TICK_ADVANCED`：模拟 tick 已推进。
11. `ANIMATION_FRAME_RESOLVED`：动画帧已解析。
12. `RENDER_TEXTURE_UPLOADED`：纹理已上传。
13. `RENDER_SUBMITTED`：渲染命令已提交。
14. `PRESENTER_PRESENTED`：呈现器已 Present。
15. `PRESENTED_PIXELS_CHANGED`：提交给合成器的像素发生变化。
16. `HUMAN_VISIBILITY_CONFIRMED`：只能由明确人工验收流程写入，绝不能自动推断。

如果下游事件缺失，必须准确报告停在哪个边界。`PRESENTED_PIXELS_CHANGED` 只能证明合成器输入改变，不能证明人眼看到了角色。

线性成功链以外的渲染生命周期事件为 `RENDER_BACKEND_READY`、`RENDER_DEVICE_LOST` 和 `RENDER_DEVICE_RECOVERED`。可恢复设备丢失必须先记录失败阶段、HRESULT、完整有界异常链和恢复动作，再最多重建并重试一次。这些事件都不代表人眼可见。

桌面采集使用 `DESKTOP_SNAPSHOT_CAPTURED`，只包含序号、可见/过滤数量和受限 DPI 范围。不得包含源令牌、原生句柄、标题、内容、截图、输入或进程身份。

动画呈现使用 `ANIMATION_STATE_SELECTED`、`ANIMATION_STATE_TRANSITIONED`、`ANIMATION_FRAME_RESOLVED` 和 `ANIMATION_FRAME_COMPOSED`。帧事件可包含 SHA-256 指纹和尺寸，但不得包含像素载荷；解析或合成不代表已经提交渲染、完成 Present 或人眼可见。

Theo Crystal 使用 `THEO_PICKED_UP`、`THEO_CARRIED`、`THEO_HOLD_BLOCKED`、`THEO_DROPPED`、`THEO_THROWN`、`THEO_HORIZONTAL_BOUNCED`、`THEO_VERTICAL_BLOCKED`、`THEO_LANDED`、`THEO_BOUNCED`、`THEO_LIFT_CARRIED`、`THEO_LIFT_INHERITED`、`THEO_SQUISHED` 和 `THEO_EXTERNAL_VELOCITY_APPLIED`。事件只包含受限的实体/持有者/Solid ID、tick、整数位置与速度。Theo 事件只证明模拟发生了状态变化，不证明素材、渲染或桌面可见。

Glider 使用 `GLIDER_PICKED_UP`、`GLIDER_CARRIED`、`GLIDER_HOLD_BLOCKED`、`GLIDER_DROPPED`、`GLIDER_THROWN`、`GLIDER_HOLDER_FALL_LIMITED`、`GLIDER_OPENED`、`GLIDER_CLOSED`、`GLIDER_HORIZONTAL_BOUNCED`、`GLIDER_VERTICAL_BLOCKED`、`GLIDER_LANDED`、`GLIDER_BOUNCED`、`GLIDER_LIFT_CARRIED`、`GLIDER_LIFT_INHERITED`、`GLIDER_DESTROYED`、`GLIDER_SQUISHED` 和 `GLIDER_EXTERNAL_VELOCITY_APPLIED`。缓降事件只证明已经产生效果请求，不证明 Player 已经应用；任何 Glider 事件都不代表素材、渲染或人眼可见。

Spring 使用 `SPRING_ACTIVATED`、`SPRING_LAUNCH_ISSUED`、`SPRING_RETRACTED`、`SPRING_COOLDOWN_STARTED`、`SPRING_READY`、`SPRING_DISABLED`、`SPRING_ENABLED` 和 `SPRING_CONTACT_IGNORED`。发射事件包含目标身份，但本身不证明目标已经应用；Player、Theo 或 Glider 会分别记录外部速度已应用。Spring 事件不代表素材、渲染或人眼可见。

## 3. 错误分类

- `INSTALL_*`：目录选择、路径包含、文件缺失或版本不支持。
- `IPC_*`：协议版本、长度、超时、取消或 Worker 退出。
- `ATLAS_META_*`：头部、数量、路径、页面、裁剪或尾随数据。
- `ATLAS_DATA_*`：尺寸、游程、Alpha、截断或解压预算。
- `SPRITE_XML_*`：XML 预算、结构、重复、缺帧或元数据无效。
- `CATALOG_*`：允许列表、歧义、依赖缺失或指纹不匹配。
- `SIM_*`：无效状态、非有限数值或 tick/顺序不变量。
- `ENTITY_*`：特定实体不变量或不支持的交互。
- `DESKTOP_*`：采集、拓扑、DPI 或隐私过滤。
- `RENDER_*`：设备、上传、步幅、Alpha、提交、Present 或设备丢失。
- `APP_*`：启动、暂停、关闭和恢复。

每个错误码只能由一个子系统拥有。App 可以显示错误，但不能改变其含义。

AssetWorker 监管会返回稳定操作码、受限协议细节码、状态、观察到的退出码和恢复动作。进程兜底事件为 `ASSET_WORKER_PROCESS_FAILED`，只向 stderr 写一行受限 JSON，绝不与 stdout 的二进制 IPC 混合。

## 4. 保存与支持包

- 默认日志按大小和代数限制，保存在用户本地应用数据目录。
- 支持包只能由用户明确操作生成，只包含日志、版本清单和哈希。
- 商业像素、源 Atlas、原始窗口列表和用户输入必须排除。
- 所有日志记录应用构建版本、合同版本和复现问题所需的系统/渲染后端摘要。
