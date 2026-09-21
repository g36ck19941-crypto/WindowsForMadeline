# 延后内容扩展合同（中文镜像）

> 英文规范：`docs/EXTENSIONS.md`。如有差异，以英文版为准。

## 当前状态

关卡/地图还原和 Mod 素材只读访问属于延后能力，而不是永久拒绝的能力。本文件只预留稳定架构接口，不增加运行实现、不扫描 Mod 目录、不解析地图、不执行 Mod 代码，也不扩大当前授权。

## 素材来源提供器接口

未来素材管线可以接入以下窄接口的实现：

```text
IAssetSourceProvider
  DescribeSource() -> AssetSourceDescriptor
  Resolve(AssetRequest) -> AssetSourceEntry | NotFound | Rejected
  OpenRead(AssetSourceEntry, ReadBudget) -> 受限制的只读内容
```

接口必须满足：

- 每个素材根目录都由用户明确选择或启用；
- 明确记录来源身份、优先级、版本和指纹；
- 所有路径经过规范化并限制在批准根目录内；
- 读取必须经过 AssetWorker 的预算和诊断；
- 来源优先级必须确定，有歧义时直接报错；
- 提供器不能返回可执行行为、任意文件系统句柄或无限流；
- 商业字节只留在本机，绝不提交或分发。

内置正版 Celeste 安装未来也会作为一个提供器。以后可以增加 Mod 素材提供器，只公开纯数据的精灵、Atlas、元数据和允许的资源，不加载 Mod 程序集或脚本。

## 世界内容提供器接口

未来模拟宿主可以通过另一个接口接收不可变世界描述：

```text
IWorldContentProvider
  DescribeWorlds() -> WorldCatalogDescriptor
  LoadRoom(WorldId, RoomId, ContentBudget) -> RoomDescriptor | Rejected
```

`RoomDescriptor` 未来可以描述受限制的几何、出生点、受支持实体放置和稳定来源信息。它不得包含回调、脚本、渲染对象、文件系统句柄或任意运行时类型。

这个接口使未来模块能还原选定关卡/地图结构，同时不让地图格式直接耦合到 `Simulation.Core`。模拟仍只接收规范化、不可变的 Solid、触发器和受支持实体描述。

## 当前明确不实现

- 不创建已编译的提供器接口或插件加载器；
- 不解析 Celeste 地图、房间或剧情；
- 不发现 Mod 目录或解析 Mod 素材；
- 不执行 Everest DLL、Lua、脚本或任意代码；
- 不承诺兼容现有 Mod；
- 不增加文件系统、GUI 或游戏安装访问授权。

## 未来门禁

只有核心素材和模拟合同稳定后，才能在 CDR-060 中加入已编译接口。具体关卡/地图和 Mod 素材提供器必须作为独立任务，先使用合成测试，并在读取真实安装或 Mod 目录前完成安全审查、再次取得明确授权。
