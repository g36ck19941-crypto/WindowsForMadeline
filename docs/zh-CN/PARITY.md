# 还原度与证据矩阵（中文镜像）

> 英文规范：`docs/PARITY.md`。状态值保持英文；如有差异，以英文版为准。

## 状态含义

- `unstarted`：尚未实现。
- `contracted`：已有来源或行为合同，但还没有实现。
- `partial`：已实现一部分，仍有已知缺口。
- `exact_offline`：确定性测试与已定义参考证据完全匹配。
- `asset_exact`：受支持安装配置所需源帧和元数据验证完全一致。
- `integrated_verified`：素材、模拟和合成呈现已连接并通过验证。
- `human_accepted`：已有单独授权的人眼可见验收。
- `deferred`：已经预留架构扩展接口，但有意推迟实现和支持承诺。
- `unsupported`：明确不属于当前产品范围。

任何项目都不能从 `unstarted` 直接跳到 `human_accepted`；每一层证据都必须独立成立。

| 能力 | 素材 | 行为 | 集成 | 人工 | 说明 |
| --- | --- | --- | --- | --- | --- |
| 显式安装结构验证 | asset_exact | exact_offline | partial | n/a | 指定安装 CDR-016 只读通过；尚未接入 App |
| AssetWorker 协议/生命周期 | n/a | exact_offline | partial | n/a | 真实 Worker 生命周期通过；尚未接入 App 或解析器 |
| Atlas `.meta` 元数据读取器 | asset_exact | exact_offline | partial | n/a | 指定安装 CDR-016 通过；其他版本未验证 |
| Atlas `.data` 图页解码器 | asset_exact | exact_offline | partial | n/a | 指定安装重复指纹通过；尚未连接渲染器 |
| Madeline 身体动画 | unstarted | unstarted | unstarted | unstarted | 只使用正版安装 |
| Player 头发 | unstarted | unstarted | unstarted | unstarted | 程序化节点和遮罩 |
| Normal/Jump | n/a | partial | partial | unstarted | CDR-021 合成输入逐 tick 通过；公开参数/顺序已覆盖，完整发行版行为和呈现未验证 |
| Dash/Wall/Climb | n/a | partial | partial | unstarted | CDR-022 合成 traversal 通过；转角修正、单向平台、移动墙提升、阻挡器和高级技巧仍未验证 |
| 移动 Solid 携带/LiftBoost | n/a | partial | unstarted | unstarted | CDR-020 核心合同/测试通过；原版参数与顺序一致性尚未建立 |
| 合成 Windows 呈现 | n/a | exact_offline | partial | unstarted | CDR-030 隐藏 D3D11/DirectComposition 提交和恢复测试通过；尚未连接素材动画或进行可见观察 |
| 匿名桌面几何 | n/a | exact_offline | partial | unstarted | CDR-031 合成跟踪和已授权的 Windows 汇总快照通过；尚未放置 Overlay 或进行可见观察 |
| 目录动画呈现 | asset_exact | exact_offline | partial | unstarted | CDR-032 程序生成目录的选帧/合成/呈现通过；指定安装格式已另行验证，但没有保存或可见显示真实像素 |
| Theo Crystal | asset_exact | partial | partial | unstarted | CDR-040 生成输入的拿起/携带/投掷/碰撞矩阵通过；指定安装格式已另行验证，但原版数值、素材动画和可见集成尚未建立 |
| Glider | asset_exact | partial | partial | unstarted | CDR-041 生成输入的拿起/携带/缓降请求/Player 应用/投掷/滑落矩阵通过；指定安装格式已另行验证，但正式 App 组装、原版数值、素材动画和可见集成尚未建立 |
| Spring | asset_exact | partial | partial | unstarted | CDR-042 程序生成的四方向生命周期及 Player/Theo/Glider 应用矩阵通过；原版接触/数值、正式 App 路由、素材动画和可见集成尚未建立 |
| Refill | unstarted | partial | partial | unstarted | CDR-043 程序生成的收集/资源恢复/冷却/重生及 Player 应用矩阵通过；原版接触/重生数值、正式 App 路由、素材动画和可见集成尚未建立 |
| Water | unstarted | partial | partial | unstarted | CDR-044 程序生成的重叠/进入/浸没/阻力/浮力/游动限速/离开及 Player 应用矩阵通过；原版水面/接触/数值、正式 App 路由、素材动画和可见集成尚未建立 |
| Bumper | unstarted | partial | partial | unstarted | CDR-045 程序生成的圆形接触/径向弹飞/中心回退/冷却/重新武装及 Player 应用矩阵通过；原版接触/数值/轨迹、正式 App 路由、素材、声音和可见集成尚未建立 |
| Puffer | asset_exact | partial | partial | unstarted | CDR-046 程序生成的有界游动/预警/爆炸/重生及 Player 应用矩阵通过；指定安装格式已另行验证，但原版轨迹/接触/时间/数值、特殊交互、正式 App 路由、动画声音和可见集成尚未建立 |
| Seeker | unstarted | partial | partial | unstarted | CDR-047 程序生成的巡逻/发现/追逐/蓄力/冲刺/命中/撞墙眩晕/恢复矩阵通过；原版寻路、复杂地形避障、伤害/反弹、数值、正式 App、素材声音和可见集成尚未建立 |
| 关卡/地图 | deferred | deferred | deferred | deferred | 未来 `IWorldContentProvider`；当前无解析器 |
| 纯数据 Mod 素材 | deferred | n/a | deferred | deferred | 未来 `IAssetSourceProvider`；当前不访问目录 |
| 剧情/过场 | unsupported | unsupported | unsupported | unsupported | 不属于当前产品目标 |
| 可执行 Everest Mod 行为 | unsupported | unsupported | unsupported | unsupported | 不执行 DLL/脚本/运行时钩子 |
