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
- `unsupported`：明确不属于当前产品范围。

任何项目都不能从 `unstarted` 直接跳到 `human_accepted`；每一层证据都必须独立成立。

| 能力 | 素材 | 行为 | 集成 | 人工 | 说明 |
| --- | --- | --- | --- | --- | --- |
| Madeline 身体动画 | unstarted | unstarted | unstarted | unstarted | 只使用正版安装 |
| Player 头发 | unstarted | unstarted | unstarted | unstarted | 程序化节点和遮罩 |
| Normal/Jump | n/a | unstarted | unstarted | unstarted | 固定 60 Hz |
| Dash/Wall/Climb | n/a | unstarted | unstarted | unstarted | 需要精确 tick/顺序证据 |
| 移动 Solid 携带/LiftBoost | n/a | unstarted | unstarted | unstarted | 桌面窗口适配为 Solid 运动 |
| Theo Crystal | unstarted | unstarted | unstarted | unstarted | 拿取/投掷/碰撞交互 |
| Glider | unstarted | unstarted | unstarted | unstarted | 拿取/下落/发射交互 |
| Spring | unstarted | unstarted | unstarted | unstarted | Player 和受支持实体 |
| Refill | unstarted | unstarted | unstarted | unstarted | 重生/冷却 |
| Water | unstarted | unstarted | unstarted | unstarted | 区域体积行为 |
| Bumper | unstarted | unstarted | unstarted | unstarted | 径向弹射/冷却 |
| Puffer | unstarted | unstarted | unstarted | unstarted | 游动/爆炸/弹射 |
| Seeker | unstarted | unstarted | unstarted | unstarted | 后置复杂实体 |
| 关卡/地图/剧情 | unsupported | unsupported | unsupported | unsupported | 明确不做 |
| 任意代码驱动 Everest Mod | unsupported | unsupported | unsupported | unsupported | 需要运行时钩子 |
