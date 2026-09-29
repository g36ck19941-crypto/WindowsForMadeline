# Celeste Desktop Runtime

一个面向 Windows 的独立桌面角色运行时：在不启动 Celeste 或 Everest 的前提下，只读解析用户正版 Celeste 安装中的角色与交互实体资源，并在桌面环境中运行独立、可验证的角色与实体模拟。

## 产品边界

- 还原原版 Madeline，以及 Theo Crystal、Glider、Spring、Refill、Water、Bumper、Puffer 等明确纳入范围的交互实体。
- 当前核心目标不实现关卡、剧情或完整游戏；为未来关卡/地图和纯数据 Mod 素材保留隔离接口，但当前不读取或执行它们。
- 产品运行时不得启动、注入或修改 Celeste/Everest。
- 游戏安装目录只读；商业素材不得进入 Git、公开构建或分发包。
- 行为、素材、呈现分别验收；任一层通过都不能推断其他层通过。
- 外部项目仅用于理解格式事实和架构取舍，不复制其实现。

## 当前状态

CDR-016、CDR-020、CDR-021、CDR-022 与 CDR-030 已由开发者验收并上传各自独立分支，远程 `main` 未改变。CDR-031 已在本地完成：新增匿名可见表面几何、DPI 与速度，并通过合成回归和一次已授权的汇总式真实只读验证。它不读取标题、内容、截图、输入或身份信息，也不证明人眼可见。

## 开发者双击入口

- `演示当前进度.cmd`：生成程序化 Atlas/XML、Actor/Solid 场景、玩家输入、棋盘格与匿名桌面几何，实际经过 CDR-012/013/014/015/020/021/022/030/031，再打开本地 HTML 报告。
- `验证当前版本.cmd`：执行 Release 构建、CDR-010 至 CDR-031 的 337 项回归及一项已授权的真实桌面匿名汇总验证；不读取标题、内容、截图或输入，不访问真实安装，也不打开可见窗口。
- `验证真实安装兼容性.cmd`：显式选择正版安装后执行 CDR-016 只读核验；不启动游戏或 GUI，证据写在项目目录。
- `tools/*.ps1` 保留给代理和 CI，不再要求开发者手动输入。

开始任何工作前阅读：

1. `AGENTS.md`
2. `docs/handoffs/CURRENT.md`
3. `TASKS.md`
4. `docs/ARCHITECTURE.md`
5. `docs/DIAGNOSTICS.md`
6. `docs/INSTALL_VALIDATION.md`
7. `docs/ASSET_WORKER.md`
8. `docs/ATLAS_METADATA.md`
9. `docs/ATLAS_DATA.md`
10. `docs/SPRITE_XML.md`
11. `docs/ASSET_CATALOG.md`
12. `docs/SIMULATION_CORE.md`
13. `docs/PLAYER_NORMAL_JUMP.md`
14. `docs/PLAYER_DASH_WALL_CLIMB.md`
15. `docs/SYNTHETIC_PRESENTATION.md`
16. `docs/DESKTOP_GEOMETRY.md`
