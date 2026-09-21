# 显式安装验证（中文镜像）

> 英文规范：`docs/INSTALL_VALIDATION.md`。如有差异，以英文版为准。

## 本次新增功能

CDR-010 只验证调用方明确传入的目录。验证器没有发现或枚举目录的能力，也不会打开文件内容。

`celeste-windows-foundation-v1` 配置只检查以下三个结构标记：

- `Celeste.exe`
- `Content/Graphics/Atlases/Gameplay.meta`
- `Content/Graphics/Sprites.xml`

本阶段不会猜测 Atlas `.data` 页面。CDR-012 将先解析能够确定页面的元数据，后续再依据实际证据验证页面。

## 能力边界

`InstallVerifier` 只通过 `IInstallFileSystem` 获得：

- 规范化路径；
- 某一个明确绝对路径的元数据状态。

接口没有目录枚举、文件内容读取或自动扫描电脑的方法。系统适配器只使用 `File.GetAttributes`。

验证器会拒绝：

- 空白、相对、无效、缺失或不是目录的根路径；
- 根目录、父目录或必需文件上的重解析点；
- 绝对、穿越、格式错误或重复的配置路径；
- 缺失、不可访问或类型错误的必需项目；
- 规范化后逃出所选根目录的任何路径。

失败结果不会发布规范化根目录，只返回稳定的 `INSTALL_*` 错误码、阶段和受限相对路径，不在问题记录中暴露用户绝对路径。

## 安全说明

本验证只是结构预检，不是永久文件系统权限。未来 AssetWorker 每次真正打开资源时仍必须重新执行包含关系和重解析点检查，防止验证后路径发生变化。

## 具体验收方式

在仓库根目录执行：

```powershell
.\tools\Verify-CDR010.ps1
```

预期结果：

- Release 构建显示 0 个警告、0 个错误；
- 15 项纯合成测试全部显示 `PASS`；
- 最后一行是 `RESULT total=15 passed=15 failed=0`。

该命令只使用内存中构造的虚拟文件状态，不会检查真实 Celeste 安装，也不会启动 GUI。

## 验收后开始的功能

验收 CDR-010 后，只开始 CDR-011：独立 AssetWorker 的有界 IPC、生命周期、超时、取消和崩溃恢复。真实安装读取仍要等到 CDR-016 再单独授权。
