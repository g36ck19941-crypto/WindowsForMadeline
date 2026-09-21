# Project Memory

Last verified: 2026-09-21

- 用户要求从全新项目角度实现原版 Madeline 与 Theo、Glider、Spring 等交互实体；不还原关卡。
- 产品运行时不得启动 Celeste/Everest，而是只读解析用户正版安装中的 Atlas、Sprites 元数据和后续明确批准的音频数据。
- 项目必须拆成小模块，细化结构化日志，并使解析、模拟、桌面几何、渲染和 App 生命周期故障可独立定位。
- 外部 `solstice23/desk-madeline` 只作为格式可行性与桌面架构参考；不得复制其 `CelesteAtlas`、`Player`、`PetWindow` 或其他实现。
- Celeste 官方公开源码仅覆盖仓库中公开代码，商业游戏和素材不在其 MIT 范围内；商业字节只允许保留在用户本机。
- Legacy 仓库 `C:\supermadeline\DesktopSummit` 在迁移决策时为分支 `feature/ds015h-hidden-runtime-poc`、HEAD `12d21321acc37ca366d2c449155fd4bfb78a7d92`、工作树干净。Legacy 未被删除或修改。
- 新仓库初始阶段只建立合同和计划；尚未读取游戏安装、启动 GUI、生成缓存或实现产品代码。
- 用户指定 `https://github.com/g36ck19941-crypto/WindowsForMadeline` 作为远程版本仓库。更新默认先本地提交，只有用户确认有效后才推送并附变更解释；不得上传侵权素材。2026-09-21 的最小写入测试在独立分支 `codex/connection-test-20260921` 成功，远程提交 `df00e2c`，远程 `main` 保持 `d237277` 未变。
- 用户于 2026-09-21 确认连接与写入测试有效；经完整出站审计后，项目基础记录发布到独立分支 `codex/cdr-001-foundation`，远程提交 `5a1dd1f`。该确认只完成 CDR-002，不等同于 CDR-001 架构验收。
- 每次更新必须明确说明新增功能；没有新增运行能力时必须直说。应尽量提供人工验收或演示，但 GUI、真实桌面和真实游戏安装仍受各自授权门禁约束。
