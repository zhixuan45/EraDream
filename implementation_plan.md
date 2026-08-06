# Unity 全量迁移实施计划

## 目标与边界

将当前 Godot 4 C# 项目的可用功能和全部场景迁移至 `unityproj/eradream-unity/`，使该目录成为独立、可在 Unity 2022.3.62f3 中打开的 uGUI + TextMeshPro 工程。迁移以既有 Godot 行为、场景节点层级、脚本挂载、信号行为、JSON 数据格式和资源包约定为准，不再新增与迁移无关的玩法或内容。

当前 Unity 原型已经迁移了一部分核心、服务层、剧情播放器和编辑器代码；它尚未具备完整 Unity 工程元数据、场景、预制体，以及 Godot 端的扩展系统、完整养成模块、资源管理与编辑器节点覆盖。本次任务允许重写 `unityproj/eradream-unity/` 内现有实现，以行为对齐优先。

迁移过程中保留 Godot 源码、资源及文档作为对照，不删除仓库根目录现有 Godot 工程。`.claude/settings.local.json` 为用户本地未跟踪配置，不纳入迁移或提交。

## 架构约定

1. `Assets/Scripts/Core/` 只放纯 C# 领域模型、算法与 JSON 序列化，不依赖 `UnityEngine`。
2. `Assets/Scripts/Services/` 承担文件、设置、资源加载、安全区、通知与响应式屏幕适配等 Unity 平台能力。
3. `Assets/Scripts/RuntimeEngine/` 与 `Assets/Scripts/StoryEditor/` 使用 uGUI 和 TextMeshPro 表现，不把领域状态藏在视图组件内。
4. Unity 2022.3 使用 `com.unity.nuget.newtonsoft-json` 作为唯一 JSON 实现；普通模型使用 `[JsonProperty]` 保留既有 snake_case 字段，剧情节点通过受控的 `JsonConverter` 读写 `node_type`，保证与现有 `.era` / `.json` 剧本兼容。
5. 单个 C# 文件不超过 1000 行；复杂控制器使用 partial 或职责明确的协作类拆分。新增和迁移的关键兼容逻辑带简洁中文注释。
6. Unity 资产序列化固定为 `Force Text`。Unity 2022.3 的 `.unity`、`.prefab` 和 `.asset` 是 Unity YAML 文本格式，不按 XML 手写。
7. 场景与 Prefab 通过 Unity Editor 生成器调用 `AssetDatabase`、`PrefabUtility` 和 `EditorSceneManager` 生成，再由 Unity 写出 YAML、GUID、fileID 和 `.meta`，避免人工拼接引用。
8. Godot 的 8 个 Autoload 迁移到 `BootstrapScene` 的 `AppServices` 常驻对象，通过 `DontDestroyOnLoad` 提供设置、响应式布局、通知、调试控制台、游戏状态和扩展服务。

## 影响范围与并行划分

| 工作流 | 对照来源 | 目标写入范围 | 验收重点 |
| --- | --- | --- | --- |
| 纯数据与养成 | `scripts/Game/Models/`、`scripts/Game/Modules/`、`scripts/Game/GameManager.cs` | `Assets/Scripts/Core/Models/`、`Assets/Scripts/Game/` | 属性、回合、训练、背包、商店、事件和存档状态可独立运行 |
| 服务与应用流 | `scripts/Core/`、根目录各 Screen | `Assets/Scripts/Services/`、`Assets/Scripts/Screens/` | 设置、文件对话框、资源加载、安全区、通知、命名/存档/菜单路由完整 |
| 运行时剧情 | `scripts/StoryPlayerEngine.cs`、`scripts/CharacterSprite.cs` | `Assets/Scripts/RuntimeEngine/` | 全部节点可链式解释，打字机、选项、音频、背景、立绘、变量、预览结束行为一致 |
| 编辑器与资源库 | `scripts/StoryEditor/`、`scripts/ExtensionEditor/`、`scripts/Core/Extensions/` | `Assets/Scripts/StoryEditor/`、`Assets/Scripts/Extensions/` | 节点图增删连线、导入导出、角色/贴纸/资源库、行为扩展注册可用 |
| 全量场景与 Prefab | `scenes/*.tscn`、`resources/theme_main.tres`、`Shaders/*.gdshader` | `Assets/Scenes/`、`Assets/Prefabs/`、`Assets/Shaders/`、`Assets/Editor/` | 15 个来源场景逐项闭环，节点、布局、脚本、动态 UI、转场和资源引用完整 |
| Unity 工程整合 | `project.godot`、迁移 README | `ProjectSettings/`、`Packages/`、Build Settings | Unity 2022.3.62f3 可识别、Force Text 生效、首场景与构建场景顺序正确 |

## 场景全量迁移矩阵

仓库实际包含 15 个 `.tscn`。其中 10 个迁移为独立业务 `.unity` 场景，3 个嵌入式功能界面迁移为 Prefab，`DebugConsole` 迁移为 Bootstrap 常驻 Prefab，`TestRunner` 的职责迁移到 Unity Test Framework。这里的“全量”指每个来源场景都有可追踪的等价产物，不代表所有界面都必须错误地登记为 Build Scene。

| Godot 来源 | Unity 目标 | 迁移形式 |
| --- | --- | --- |
| `WelcomeScreen.tscn` | `Assets/Scenes/WelcomeScene.unity` | 启动业务场景 |
| `LoadingScreen.tscn` | `Assets/Scenes/LoadingScene.unity` | 异步转场场景与弹幕 Prefab |
| `MainMenuScreen.tscn` | `Assets/Scenes/MainMenuScene.unity` | 主菜单业务场景 |
| `NamingScreen.tscn` | `Assets/Scenes/NamingScene.unity` | 命名业务场景 |
| `SaveSlotScreen.tscn` | `Assets/Scenes/SaveSlotsScene.unity` | 存档业务场景与存档卡 Prefab |
| `StorySelectorScreen.tscn` | `Assets/Scenes/StorySelectorScene.unity` | 剧本选择场景与列表项 Prefab |
| `StoryPlayerScreen.tscn` | `Assets/Scenes/StoryPlayerScene.unity` | 剧情播放场景及角色、选项 Prefab |
| `SimulationMainScreen.tscn` | `Assets/Scenes/SimulationScene.unity` | 养成主场景 |
| `EditorScreen.tscn` | `Assets/Scenes/StoryEditorScene.unity` | 运行时节点编辑器场景与节点 Prefab |
| `ExtensionEditorScreen.tscn` | `Assets/Scenes/ExtensionEditorScene.unity` | 扩展编辑器场景与分区 Prefab |
| `TrainingMenuUI.tscn` | `Assets/Prefabs/Simulation/TrainingMenu.prefab` | 养成场景动态覆盖层 |
| `InventoryUI.tscn` | `Assets/Prefabs/Simulation/InventoryModal.prefab` | 养成场景动态覆盖层 |
| `ScoutingUI.tscn` | `Assets/Prefabs/Simulation/ScoutingModal.prefab` | 养成场景动态覆盖层 |
| `DebugConsole.tscn` | `Assets/Prefabs/System/DebugConsole.prefab` | Bootstrap 常驻覆盖层 |
| `TestRunner.tscn` | `Assets/Tests/EditMode/`、`Assets/Tests/PlayMode/` | 测试职责等价迁移，不进入玩家构建 |

Unity 额外增加 `Assets/Scenes/BootstrapScene.unity`，承接 Godot Autoload 生命周期并加载 `WelcomeScene`。所有场景切换统一经过场景路由服务；Godot 侧误写的 `StoryPlayerEngine.tscn` 不照抄，统一指向 `StoryPlayerScene`。

## 执行步骤

1. 为每个并行工作流派发 `gpt-5.6-terra-lowcache` 的 `high` 推理代理，要求只修改各自互斥的目标路径，并保留迁移来源与行为差异说明。
2. 主线程审查各代理产物，统一命名空间、序列化约定、依赖方向和文件行数；合并任何跨模块接口。
3. 固化 `ProjectSettings/ProjectVersion.txt` 为 `2022.3.62f3`，设置 `Force Text`、可见 meta 文件、1280x720 基准分辨率、横竖屏适配与产品版本。
4. 编写并运行 Unity Editor 资产生成器，创建 Bootstrap、10 个业务场景、动态 UI Prefab、Canvas/EventSystem、脚本引用、Build Settings 和主题资产。
5. 将 7 个 Godot Shader 逐项迁移为 Unity UI/TMP Shader 或行为等价实现，并在场景中验证材质引用；不允许静默遗漏。
6. 使用本机 `C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe` 以 batchmode 执行首次导入、脚本编译、场景生成、EditMode/PlayMode 测试。
7. 打开或批处理加载每个业务场景，检查缺失脚本、丢失引用、Canvas/EventSystem、场景路由、动态 Prefab 实例化和控制台异常。
8. 审查最终差异，确认没有修改 Godot 源工程或用户本地 `.claude/` 配置，然后提交迁移结果。

## 风险与决策点

0. Unity 2022.3 的内置 `System.Text.Json` 不具备本迁移所需的多态和 DOM API。已在首次 batchmode 编译中确认该阻断，统一替换为 Unity 官方 Newtonsoft JSON 包；不混用两套序列化器，以免存档、扩展包与剧本产生字段差异。

1. Unity 版本锁定为 2022.3.62f3，且本机已确认该 Editor 存在。包版本、项目设置和 YAML 均以此版本实际导入结果为准。
2. Godot 的动态 UI 与场景节点不能逐行机械映射。迁移需要同时复刻 `.tscn` 静态层级与脚本动态生成内容，不能把“存在 `.unity` 文件”当成场景迁移完成。
3. Godot `GraphEdit` 无 Unity 一一对应组件，采用现有运行时 uGUI 节点画布方向，但必须保留节点增删、拖拽、连线、载入、保存和预览行为。
4. 文件对话框和外部资源访问在 WebGL 与移动端不可完全等价。桌面端优先支持原流程，平台受限能力通过明确失败提示处理。
5. Godot Shader 不能直接复制到 Unity。每个 Shader 必须有迁移记录、Unity 实现或经批准的行为替代，并在对应场景中验证。

## 完成标准

1. `unityproj/eradream-unity/` 是可被 Unity Hub 2022.3.62f3 打开的完整项目，包含 `Assets`、`Packages` 和 `ProjectSettings`，并启用 Force Text YAML。
2. 核心剧本 JSON 和角色数据能序列化、反序列化，并覆盖所有既有节点类型。
3. 主菜单、存档/命名、剧情选择、剧情播放、编辑器与养成主界面均有可达入口。
4. 原有扩展行为模型与资源包读取逻辑在 Unity 端有等价实现或明确的兼容层。
5. 15 个 Godot 来源场景均在场景迁移矩阵中有等价 Unity Scene、Prefab 或 Test Framework 产物；10 个业务场景和 Bootstrap 被 Unity 实际加载验证。
6. Unity batchmode 完成无脚本编译错误、无 Missing Script、无丢失对象引用，EditMode/PlayMode 测试通过。
7. 7 个 Godot Shader 均有 Unity 迁移实现或明确、可验证的等价行为记录。
8. 无单文件超过 1000 行，且迁移关键兼容逻辑有简洁中文注释。

## 剧情演出与编辑器持久化补全（已批准）

本轮在 Unity 迁移分支中补齐 Godot 时期已经存在且影响实际使用的剧情问题；不修改 Godot 源工程，也不改变既有剧本 JSON 的字段名称。

1. 统一剧情播放器和编辑器预览的逻辑舞台坐标。角色使用归一化横向位置（左 0.25、中 0.5、右 0.75），底部 Pivot 和统一的逻辑立绘高度；`CustomX`/`CustomY` 以 0 到 1 的舞台坐标解释，保证不同尺寸容器中的相对落点一致。
2. 背景切换改为双层背景。`Cut` 立即替换，`Fade` 在 `Duration` 内让旧图淡出、新图淡入；未知方式回退为 `Fade`，并保留 `BackgroundNodeData.TransitionType` 与 `Duration` 的兼容数据格式。
3. 新增独立的编辑器自动保存控制器。它订阅节点图、角色库与贴纸库的变更事件，采用脏标记加防抖写入；显式保存、暂停、退出和禁用时强制落盘 `story.json`、`characters.json`、`stickers.json` 与 `project.uma`。
4. 项目打开时同步加载剧情、角色和贴纸数据；项目创建时使用原子 JSON 保存生成初始文件。场景生成器负责绑定剧情播放器的双背景和编辑器自动保存控制器。
5. 通过 Unity 2022.3.62f3 batchmode 重新生成场景并进行脚本编译、Missing Script 扫描；必要时补充编辑模式测试。
