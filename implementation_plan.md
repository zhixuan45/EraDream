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
## Godot 剧情编辑器与播放器修复实施计划（待批准）

### 已确认问题与目标

本章节只修改 Godot 工程 `C:\Users\JuziD\godot\Eradream`，不覆盖上方 Unity 迁移计划。用户提供的截图确认：预览窗口固定为 1280x720，而正式播放按实际窗口尺寸分别计算背景、立绘和对话 UI，造成同一剧情在小窗与全屏中的角色、文本框落点明显不一致。

本次要交付以下可用功能：

1. 预览与正式播放共用 1280x720 设计画布、等比缩放与居中留边逻辑；背景、立绘、贴纸和对话 UI 的相对落点保持一致。
2. 背景节点可设置并实际执行 Cut、Fade、Slide；新增独立过场节点，支持黑幕淡入淡出、白闪、左滑、右滑。
3. 背景可进入可视化编辑，支持拖动定位、滚轮缩放、右键结束；编辑画面必须显示简短操作提示“拖动定位 · 滚轮缩放 · 右键结束编辑”。
4. 导入背景、音频、立绘和字体后，已经打开的相关节点下拉列表立即刷新并保留当前选择。
5. 编辑器工程自动保存与游戏进度自动保存同时修复；保存失败不再伪报成功，启动时能够恢复有效自动存档。
6. 对话和叙述支持项目默认字体、节点字体覆盖、节点打字速度、文本完整显示后的自动推进等待时间；自动播放只在文本完成后计时。
7. 新增独立音效节点，并允许对话/叙述直接绑定音效；音效支持文件、音量、是否等待播放结束，非阻塞音效可与文本并行。

### 数据与兼容约定

1. `ProjectMetadata` 增加 `DefaultFontFile`、`DefaultTypewriterSpeed`、`DefaultAutoAdvanceDelay` 与 `AutoPlayEnabled`。空字体与小于等于零的节点数值均表示继承项目默认值。
2. `BackgroundNodeData` 增加 `OffsetX`、`OffsetY`、`Scale`、`TransitionDuration`。新字段均提供默认值，旧 JSON 缺失字段时按零偏移、1 倍缩放、0.5 秒淡入处理。
3. `DialogueNodeData` 与 `NarrativeNodeData` 增加 `FontFile`、`TypewriterSpeed`、`AutoAdvanceDelay` 及可选音效字段；已有 `VoiceFile` 会在对话开始时真正播放。
4. 新增 `TransitionNodeData` 和 `SoundEffectNodeData`，并注册到 `BaseNodeData` 的多态 JSON 判别器。旧剧本不含新节点，加载行为不变。
5. 设计画布固定为 1280x720。旧立绘和贴纸使用现有字段解释方式，但映射统一基于设计画布；背景变换新增字段不影响旧背景节点。
6. 存档写入继续使用临时文件替换；`GameManager.SaveGame` 改为返回成功状态，只有成功时更新最近存档路径和成功提示。

### 实施步骤与文件边界

1. 播放器与场景：拆分 `StoryPlayerEngine` 的演出逻辑，建立设计画布、双背景层、过场遮罩、背景编辑、字体应用、打字/自动推进状态、语音与音效播放器池；同步调整 `StoryPlayerScreen.tscn` 和 `StoryPreviewUI.cs`。连续过场会取消旧 Tween，避免旧回调推进到新节点。
2. 编辑器节点与资源：扩展背景、对话、叙述节点 UI；新增过场与音效节点；编辑器侧栏加入节点入口；资源管理器在导入成功后发出类型化刷新事件，背景、音频、字体和立绘选择器订阅并刷新。
3. 编辑器持久化：`EditorScreen` 引入脏标记与约 2 秒防抖自动保存，节点字段修改、拖动、缩放、连线、删改、导入资源、角色与贴纸编辑都会标脏；切项目与退出树时强制保存。`StoryNodeManager`、角色、贴纸和项目元数据统一改为可报告结果的原子写入。
4. 游戏持久化：`GameManager` 采用脏标记与防抖写盘；剧情数值节点、养成行动、回合结算、结局、场景切换和退出前都请求或强制刷新自动存档。启动时优先加载有效 `autosave.sav`，失败时回退备份或开始新游戏。修复剧情触发错误指向的 `StoryPlayerEngine.tscn`，改为实际存在的 `StoryPlayerScreen.tscn`。
5. 测试与验证：补充 JSON 旧剧本加载和新字段往返测试、自动保存成功/失败测试、剧情数值变更保存测试；在 1280x720、1920x1080、超宽和非 16:9 尺寸分别验证预览与正式播放的一致性。

### 验收标准

1. 同一剧本在预览和正式全屏中，背景焦点、角色脚底、贴纸和对话框相对位置一致；窗口变化只产生等比缩放和留边。
2. Fade、Cut、Slide、黑幕、白闪、左右滑动可在编辑器中配置并在播放器中可见；快速连续切换不会残留旧背景或旧动画回调。
3. 背景和立绘可视化编辑时可见操作提示，拖动/滚轮/右键操作能正确回写并自动保存。
4. 导入素材无需关闭重开节点即可在选择器中看见新文件；字体目录可导入并可被项目默认与节点覆盖选择。
5. 自动播放严格在打字完成后开始计时；节点可覆盖打字速度和停留时间；选择与阻塞音效不会被自动跳过。
6. 非阻塞音效与对话、叙述同步播放；阻塞音效结束后才继续剧情；BGM、语音、音效不会互相截断。
7. 编辑器修改后无需手动保存即可恢复；游戏修改状态、剧情奖励和结局状态可在重启后恢复；保存失败会明确提示且不会显示为保存成功。

### 已批准的移动端与全屏补充

1. 设计画布仍固定为 1280x720，但运行时以当前可用视口为准计算等比缩放、居中偏移和安全区，不以设备物理分辨率直接重算剧情坐标。横屏、刘海屏、超宽屏与非 16:9 屏幕均保留一致构图。
2. 桌面端全屏采用无边框窗口化模式，并使用实际可用显示区域计算画布占用比例；任务栏、窗口边框和系统安全区域不得进入设计坐标计算。
3. 移动端对话推进支持点击；可视化编辑支持单指拖动定位、双指缩放，右键结束编辑在移动端改为明确的完成按钮。操作提示会随输入设备显示相应文本。
4. 所有触控目标保持适合手指点击的最小尺寸；自动播放、选择按钮和手势编辑在窗口尺寸变化、屏幕旋转后重新应用画布变换。
