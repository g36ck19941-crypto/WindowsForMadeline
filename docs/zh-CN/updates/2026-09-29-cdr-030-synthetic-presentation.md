# CDR-030 更新：隐藏 DirectComposition 呈现

CDR-030 新增平台无关呈现控制器和 Windows D3D11/DirectComposition 后端，输入只使用程序生成、预乘 Alpha 的 BGRA32 帧。上传、提交、完成等待和像素变化分别记录；可恢复设备丢失最多重建并重试一次，同时保留完整异常细节。

原生隐藏测试通过从不显示的 HWND、D3D11、`IDCompositionSurface`、`Commit` 和 `WaitForCommitCompletion` 实际提交两张生成帧。Rendering 专项 20/20、总回归 318/318，Release 0 警告/错误。

累计报告新增合成呈现棋盘格，补回已单独通过的 CDR-016，并说明 CDR-017 至 CDR-019 是未分配保留编号。没有商业素材、真实安装访问、可见 GUI、桌面观察或人眼可见声明。
