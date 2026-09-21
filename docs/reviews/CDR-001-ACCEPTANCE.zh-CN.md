# CDR-001 中文验收说明

## 本次更新新增了什么

CDR-001 新增的是项目架构、诊断、安全解析、参考来源、还原状态和任务顺序合同。

本阶段没有新增可运行功能，没有实现解析器或桌面角色，没有启动 Celeste/Everest、GUI，也没有读取真实游戏安装或加入任何商业素材。

## 验收 CDR-001 代表同意什么

验收表示你同意以下项目边界：

1. 产品运行时不得启动、注入、控制或修改 Celeste/Everest。
2. 商业资源只能留在用户自己的电脑上；后续解析工作放在独立、受限、只读的 AssetWorker 中。
3. 素材是否正确、角色行为是否正确、画面是否成功提交，以及人眼是否真的看到，必须分别验证，不能互相代替。
4. 本项目不还原关卡、地图和剧情；原版 Madeline 与任务书列出的交互实体仍属于目标范围。
5. 验收后只允许开始 CDR-010：使用纯合成文件系统测试安装目录验证器。
6. 读取真实游戏安装仍必须等到 CDR-016，并再次取得你的明确授权。

## 验收项目与证据

| 验收项目 | 可以核对的文件 | 当前结果 |
| --- | --- | --- |
| 模块和进程边界是否明确 | `docs/ARCHITECTURE.md` 第 1–5 节 | 已准备 |
| 解析、生成、模拟、提交、Present 和人眼可见是否被严格区分 | `docs/DIAGNOSTICS.md` 第 2 节 | 已准备 |
| 素材解析是否只读、受限并在失败时拒绝发布部分结果 | `docs/ASSET_SAFETY.md`、`docs/ARCHITECTURE.md` 第 2 节 | 已准备 |
| 是否禁止照抄外部项目实现 | `docs/REFERENCE_POLICY.md` | 已准备 |
| 素材、行为、集成和人工可见证据是否分别记录 | `docs/PARITY.md` | 已准备 |
| 任务顺序和下一步的纯合成范围是否明确 | `TASKS.md` 中的 CDR-010 | 已准备 |
| 旧 DesktopSummit 项目是否只读冻结并留有记录 | `PROJECT_MEMORY.md`、`docs/handoffs/CURRENT.md` | 已准备 |
| 是否没有引入产品代码、游戏读取、GUI 或商业素材 | 当前仓库文件树、`docs/updates/2026-09-21-project-foundation.md` | 已准备 |

## 五分钟人工验收方法

1. 查看 `docs/ARCHITECTURE.md`，确认模块依赖方向和独立 AssetWorker 符合你的预期。
2. 查看 `docs/DIAGNOSTICS.md`，确认素材解码、实体生成、画面 Present 和人眼可见是四件不同的事情。
3. 查看 `docs/ASSET_SAFETY.md`，确认禁止自动扫描 Steam、启动游戏、写入游戏目录或分发商业字节。
4. 查看 `docs/PARITY.md`，确认所有运行能力仍诚实标记为 `unstarted`，没有把规划冒充为完成。
5. 查看 `TASKS.md`，确认下一项工作只有 CDR-010 的合成安装验证。

如果希望自行核对仓库，可在 `C:\supermadeline\CelesteDesktopRuntime` 执行：

```powershell
git status --short
git ls-tree -r --name-only HEAD
git ls-tree -r --name-only HEAD | rg -i '\.(png|jpg|ogg|wav|bank|data|meta|xnb|dll|exe|zip|bin|cache)$'
```

预期结果：工作树干净；仓库只有文本和配置记录；最后一条素材与二进制扩展扫描没有结果。

## 验收后的权限边界

验收 CDR-001 只允许开始 CDR-010，不代表授权以下事项：

- 读取真实 Celeste 安装；
- 启动 Celeste、Everest 或任何 GUI；
- 进行真实桌面观察；
- 开始 CDR-011 或更后的任务；
- 上传或分发任何商业素材。

如果你接受以上边界，请回复：

> 验收 CDR-001，开始 CDR-010
