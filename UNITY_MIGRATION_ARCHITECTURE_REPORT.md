# EraDream 从 Godot 到 Unity 的实现迁移报告

本文面向后续负责 Unity 实现的开发者或 AI。目标不是简单把 Godot 类名逐个翻译成 Unity 类名，而是记录 EraDream 当前工作树的真实行为契约、数据格式、模块边界、已知缺陷，以及建议的 Unity 目标架构和验收标准。

## 0. 阅读规则与事实边界

本报告基于以下项目的源码、场景、测试、文档和现有 Unity 对照工程整理：

- Godot 项目：`C:\Users\JuziD\godot\Eradream`
- Unity 对照项目：`C:\Users\JuziD\proj\eradream-unity`

必须区分三类信息：

1. **当前实际实现**：以当前工作树源码和实际测试数据为准。
2. **历史设计**：旧文档和 Git 历史中曾经存在、但当前可能已经删除或停用的方案。
3. **Unity 建议目标**：为了可维护性、安全性和跨平台而提出的新实现，不代表 Unity 工程已经完成。

当前 Godot 项目是 Godot 4.6.1 .NET / C# / .NET 8 项目。当前 Unity 工程已经有迁移骨架，但尚未完整实现双编辑器、剧情播放器、扩展包运行时和完整养成流程。不要把 Unity 现有文件当作已完成的功能清单。

当前工作树中还存在用户原有修改，例如 `project.godot`、`scripts/CharacterSprite.cs`、`scripts/StoryPlayerEngine.VisualEditing.cs` 等。本报告没有修改或回滚这些源码。

## 1. 项目整体结构

Godot 当前主要目录如下：

```text
project.godot
EraDream.csproj
scenes/
  WelcomeScreen.tscn
  LoadingScreen.tscn
  MainMenuScreen.tscn
  NamingScreen.tscn
  SaveSlotScreen.tscn
  StorySelectorScreen.tscn
  StoryPlayerScreen.tscn
  SimulationMainScreen.tscn
  EditorScreen.tscn
  ExtensionEditorScreen.tscn
  TestRunner.tscn
scripts/
  Core/
  Game/
  StoryEditor/
  ExtensionEditor/
  Tests/
resources/
audio/
Shaders/
translations/
docs/
```

主要分层可以理解为：

```text
场景和 UI
    ↓
流程编排与服务
    ↓
养成领域模块 / 剧情运行时 / 扩展运行时
    ↓
GameState、角色数据、行为注册表、资源代理
    ↓
文件系统、设置、存档、翻译、扩展包基础设施
```

Godot 的 `GameManager` 目前承担了较多职责，包括游戏状态生命周期、模块创建、回合推进、剧情跳转、签约池、行为 Hook、存档和 UI 刷新。Unity 迁移不建议原样复制一个不断扩大的全局 Manager，而应保留外部行为并拆分内部职责。

## 2. Godot 到 Unity 的总体目标架构

建议 Unity 使用一个明确的启动场景和服务组合根：

```text
BootstrapScene
  └── AppServices / ServiceCompositionRoot
        ├── SettingsService
        ├── FileSystemService
        ├── LocalizationService
        ├── ResponsiveService
        ├── ErrorNotificationService
        ├── SceneRouter
        ├── GameSession
        ├── TurnSystem
        ├── SaveService
        ├── ContentRegistry
        ├── CharacterRegistry
        ├── BehaviorRegistry
        ├── StoryRuntime
        └── ExtensionManager
```

这些服务应该由 Bootstrap 按固定顺序创建，并通过接口注入到 UI 和领域服务。可以使用 `DontDestroyOnLoad`，也可以使用常驻服务场景，但不要让 UI 依赖散落的静态单例初始化顺序。

推荐职责：

| Unity 服务 | 职责 |
|---|---|
| `GameSession` | 当前游戏状态、开始新游戏、加载状态、标记脏数据 |
| `TurnSystem` | 回合结算、回合开始、回合结束、结束判定 |
| `SaveService` | JSON、压缩、临时文件、校验、迁移、存档槽 |
| `SceneRouter` | 场景切换、返回场景、剧情启动上下文、预览上下文 |
| `ContentRegistry` | 内置和扩展内容索引、来源信息、优先级 |
| `CharacterRegistry` | 角色定义、模拟数据、角色资源根目录 |
| `BehaviorRegistry` | 行为 DSL、Hook、条件、动作、卸载 |
| `StoryRuntime` | 剧情数据读取和节点执行 |
| `ExtensionManager` | 扫描、验证、依赖、激活、停用、缓存 |
| `ResourceService` | 包内相对路径、Addressables、外部媒体、缓存和释放 |
| `EventBus` | 跨模块事件；用于降低 UI 与领域层耦合 |

Unity 的程序集建议最终拆为：

```text
com.eradream.core
com.eradream.game
com.eradream.story
com.eradream.extensions
com.eradream.editor
com.eradream.test
```

如果项目暂时不采用 UPM，也应按相同职责创建 Assembly Definition，避免所有代码继续进入一个默认程序集。

## 3. 启动流程、场景和路由

Godot 的 Autoload 包括 `GameManager`、`ExtensionManager`、`SettingsManager`、`ResponsiveManager`、`ErrorNotifier`、`DebugConsole` 等。Unity 对应为 Bootstrap 常驻对象。

主要场景映射：

| Godot | Unity 建议 | 用途 |
|---|---|---|
| `WelcomeScreen.tscn` | `WelcomeScene` | 欢迎页和窗口初始化后的首屏 |
| `LoadingScreen.tscn` | `LoadingScene` | 异步加载和进度反馈 |
| `MainMenuScreen.tscn` | `MainMenuScene` | 主菜单 |
| `NamingScreen.tscn` | `NamingScene` | 创建游戏、训练员命名 |
| `SaveSlotScreen.tscn` | `SaveSlotsScene` | 自动存档和存档槽 |
| `StorySelectorScreen.tscn` | `StorySelectorScene` | 内置/扩展剧情索引 |
| `StoryPlayerScreen.tscn` | `StoryPlayerScene` | 剧情运行时 |
| `SimulationMainScreen.tscn` | `SimulationScene` | 养成主界面 |
| `EditorScreen.tscn` | `StoryEditorScene` | 剧情项目编辑器 |
| `ExtensionEditorScreen.tscn` | `ExtensionEditorScene` | 扩展项目和行为编辑器 |
| `TrainingMenuUI.tscn` | `TrainingMenu.prefab` | 训练面板 |
| `InventoryUI.tscn` | `InventoryModal.prefab` | 背包面板 |
| `ScoutingUI.tscn` | `ScoutingModal.prefab` | 签约池面板 |
| `DebugConsole.tscn` | Bootstrap Overlay | 调试控制台 |
| `TestRunner.tscn` | Unity Test Framework | 自动化测试入口 |

典型流程：

```text
Welcome → MainMenu → Loading → Naming / SaveSlots / StorySelector
Naming → StorySelector → Simulation
StorySelector → StoryPlayer
Simulation → Training / Inventory / Scouting / StoryPlayer
```

剧情跳转上下文应使用显式对象，而不是静态字段：

```csharp
public sealed class StoryLaunchContext
{
    public string StoryPath { get; init; }
    public string StartNodeId { get; init; }
    public string ReturnSceneId { get; init; }
    public bool IsPreview { get; init; }
}
```

正式播放结束返回 `ReturnSceneId`。预览结束只发出完成事件并回到编辑器，不得误跳回主菜单。

## 4. UI、分辨率、窗口和资源路径

Godot 的剧情设计分辨率是 `1280x720`。Unity 应为剧情画布固定 Reference Resolution `1280x720`，运行时采用等比缩放并居中留边。不要根据每台设备分辨率重新计算剧情节点坐标，否则编辑器中的位置无法复现。

建议：

- uGUI `CanvasScaler` 使用 `Scale With Screen Size`。
- 剧情坐标放在固定 Design Canvas 中。
- 游戏 UI 使用 Anchor、Layout Group 和 Safe Area Root。
- `Screen.safeArea` 变化时只更新 Safe Area Root。
- 监听尺寸/方向变化并发送一次初始化通知。
- 不在每帧重建布局。
- 1280x720、1920x1080、21:9 和竖屏必须截图回归。

路径映射：

| Godot | Unity |
|---|---|
| `user://` | `Application.persistentDataPath` |
| `res://` | Addressables、StreamingAssets 或内置资源索引 |
| `FileAccess` / `DirAccess` | `FileSystemService` |
| `ResourceLoader` | Addressables、`UnityWebRequest` 或外部字节加载 |
| Autoload | Bootstrap 常驻服务 |
| signal | C# event 或 `EventBus` |
| `Timer` 防抖 | Coroutine、async 或取消令牌 |

扩展内容必须只使用包内相对路径，例如 `Assets/Sprites/body.png`。用户包中的绝对路径、`..`、`res://`、`user://` 和越出包根目录的符号链接路径都应拒绝。

## 5. 游戏状态与养成模块

对应 Godot 文件：

- [GameManager.cs](C:\Users\JuziD\godot\Eradream\scripts\Game\GameManager.cs)
- [GameState.cs](C:\Users\JuziD\godot\Eradream\scripts\Game\Models\GameState.cs)
- [PlayerStats.cs](C:\Users\JuziD\godot\Eradream\scripts\Game\Models\PlayerStats.cs)
- [UmaStats.cs](C:\Users\JuziD\godot\Eradream\scripts\Game\Models\UmaStats.cs)
- `scripts/Game/Modules/TrainingModule.cs`
- `RestModule.cs`
- `OutingModule.cs`
- `WorkModule.cs`
- `ShopModule.cs`
- `InventoryModule.cs`
- `EventModule.cs`

### 5.1 GameState

主要字段：

```text
CurrentTurn = 1
MaxTurns = 72
ScenarioPaths
CharacterPaths
ModPaths
ActiveUmaId
CurrentScoutPool
Player
Uma
Inventory
```

玩家数据：

```text
PlayerName
Money
Stamina / MaxStamina
Energy / MaxEnergy
```

角色数据：

```text
Mood
ActionStamina / MaxActionStamina
Energy / MaxEnergy
Speed
Stamina
Power
Guts
Intelligence
SkillPoints
Affection
Dictionary<string, int> CustomStats
```

约束必须集中在领域模型或领域服务中，不能只靠 UI 输入限制：

- 五维属性上限为 1200。
- Mood 范围为 0 到 150。
- 玩家资源范围为 0 到各自 Max。
- Money 不能因消费变成负数。
- SkillPoints 和 Affection 不低于 0。
- 自定义属性推荐使用 `namespace:stat_id`，避免扩展之间冲突。

建议 Unity 领域层保持纯 C#，不要在模型中引用 `MonoBehaviour`、`GameObject` 或 UI。UI 只提交命令并订阅状态变更。

### 5.2 训练流程

当前流程应保持如下顺序：

```text
TrainingMenuUI
  → SimulationMainScreen
  → TrainingModule
  → BehaviorRegistry.GetTrainingDefinition
  → 检查角色行动体力
  → 检查训练员精力
  → 计算失败率
  → 随机判定
  → 扣除资源
  → 发放属性奖励
  → 执行 OnTraining Hook
  → 执行 OnTraining_<id> Hook
  → 标记存档脏
  → 通知 UI 刷新
```

默认训练为 Speed、Stamina、Power、Guts、Intelligence。训练定义需要支持基础定义和扩展定义统一查询。

当前已知风险：`TrainingMenuUI` 可能先触发 `CustomTrainingSelected`，随后对可解析为默认训练的 ID 再触发 `TrainingSelected`，有同一训练执行两次的可能。迁移时必须先写回归测试确认实际行为，再决定修复或兼容，不能直接把它当作必然事实。

`GrowthBonus` 已经出现在角色配置模型中，但在当前训练/签约逻辑中没有确认到完整应用链路。Unity 应明确把它纳入训练奖励公式，或者在迁移规格中标成待决策字段，避免数据模型和运行逻辑再次脱节。

### 5.3 其他模块

建议每个模块提供独立、可测试的命令接口：

```text
TrainingService.Train(trainingId)
RestService.Rest()
OutingService.Go(outingId)
WorkService.Work(workId)
ShopService.Buy(itemId)
InventoryService.Use(itemId)
EventService.Resolve(eventId, optionId)
```

每个命令都应返回结构化结果，例如成功/失败、失败原因、资源变化、属性变化、是否消耗回合、触发的剧情和 Hook。不要让 UI 通过解析文本判断成功与否。

## 6. 回合结算和事件系统

回合结束顺序是核心行为契约，必须原样保留：

```text
OnTurnEnd
→ 更新物品持续效果
→ 恢复玩家资源
→ 恢复已签约角色，或刷新签约池
→ CurrentTurn++
→ 判断游戏结束
→ 自动存档
→ 检查回合开始剧情
→ OnTurnStart
```

`HandleTurnStart` 需要一个类似 `_hasTriggeredTurnStartThisTurn` 的保护，避免同一回合重复触发开始剧情和行为。

建议将回合系统写成显式状态机：

```text
Ready
→ ActionSelection
→ ActionResolved
→ TurnEnding
→ TurnStarted
→ GameOver
```

一个回合只能从 `ActionResolved` 进入 `TurnEnding` 一次。所有状态变更通过 `TurnResult` 和事件发出，存档在确定的结算点执行。

事件总线建议区分：

- 领域事件：`TrainingCompleted`、`TurnEnded`、`ItemEffectApplied`。
- 剧情事件：`StoryTriggered`、`StoryFinished`。
- UI 事件：`ShowToast`、`RefreshSimulationView`。

不要让行为 DSL 直接调用任意 Unity 方法。行为只能调用白名单动作。

## 7. 剧情数据模型和播放器

对应 Godot 文件：

- [StoryPlayerEngine.cs](C:\Users\JuziD\godot\Eradream\scripts\StoryPlayerEngine.cs)
- [StoryPlayerEngine.Presentation.cs](C:\Users\JuziD\godot\Eradream\scripts\StoryPlayerEngine.Presentation.cs)
- [StoryPlayerEngine.VisualEditing.cs](C:\Users\JuziD\godot\Eradream\scripts\StoryPlayerEngine.VisualEditing.cs)
- [StoryData.cs](C:\Users\JuziD\godot\Eradream\scripts\StoryEditor\StoryData.cs)
- `scripts/StoryEditor/Nodes/*.cs`

节点类型：

```text
Start, End, Dialogue, Narrative, Choice, Branch,
Background, Sprite, Sticker, Music, SoundEffect,
Transition, Value, ExtensionNode
```

播放器至少要支持：

- 对话和旁白。
- 打字机效果。
- 打字机完成后才开始自动推进计时。
- Choice 节点暂停。
- 背景 Cut、Fade、Slide。
- 黑幕、白闪、左右滑动转场。
- 立绘的位置、缩放、表情、遮罩和点击命中。
- BGM、Voice、SFX 分离。
- 阻塞和非阻塞 SFX。
- 预览和正式播放共用执行逻辑。
- 正式播放结束返回来源场景。
- 预览结束只通知编辑器。

Unity 映射：

| Godot | Unity |
|---|---|
| `Label` / `RichTextLabel` | TextMeshProUGUI |
| `TextureRect` | Image / RawImage |
| `AudioStreamPlayer` | 独立 BGM、Voice、SFX AudioSource |
| `Tween` | DOTween、Animator 或统一 Tween 服务 |
| `CanvasLayer` | Canvas sorting order |
| `ResourceLoader` | Addressables / ResourceProvider |

建议使用“数据节点 + 执行器”设计：

```text
StoryDocument
  → StoryValidator
  → StoryRuntimeContext
  → IStoryNodeExecutor
  → StoryPresentationAdapter
```

不要让节点数据直接创建 Unity UI。编辑器节点 View 也不应是运行时节点数据本身。

### 7.1 JSON 兼容

当前至少存在两种格式：

1. 旧版节点数组格式。
2. 新版包装格式，带 `SchemaVersion`、节点位置和多态节点数据。

Unity 应使用 Newtonsoft.Json 自定义多态 Converter。正式格式建议：

```json
{
  "schema_version": 2,
  "nodes": [
    {
      "node_type": "dialogue",
      "id": "node_001",
      "next": "node_002"
    }
  ]
}
```

`node_type` 应作为正式字段；`type`、`NodeType`、`Type` 只作为旧格式兼容。未知节点反序列化为 `UnknownNodeData` 并产生诊断，不应让整个故事项目无法打开。

加载后必须验证：缺失 Start、重复 ID、悬空引用、孤立节点、不可达节点、Choice 选项目标缺失和非法循环。

### 7.2 剧情画布和资源

剧情项目目录当前约定为：

```text
ProjectRoot/
  project.uma
  story.json
  characters.json
  stickers.json
  audio/
  backgrounds/
  sprites/
  fonts/
```

当前 `.era` 是 Godot PCK 风格容器，Unity 不能直接读取。迁移时应选择 ZIP/目录作为跨引擎标准，或另写 `.era` 转换器。建议不要把 Unity AssetBundle 当作跨引擎源格式。

## 8. StoryEditor 双编辑器之一：剧情编辑器

重要事实：Godot 的 StoryEditor 不是 Godot 原生 `EditorPlugin`，而是运行时场景 `EditorScreen.tscn` 中的作者工具。当前没有发现 `addons/*/plugin.cfg`、`EditorPlugin` 或 Godot 编辑器菜单入口。

对应文件：

- `scripts/StoryEditor/EditorScreen.cs`
- `StoryNodeManager.cs`
- `ProjectManager.cs`
- `StoryData.cs`
- `Nodes/*.cs`
- `AudioLibrary.cs`
- `BackgroundLibrary.cs`
- `SpriteLibrary.cs`
- `ResourceManagerUI.cs`
- `CharacterEditorUI.cs`
- `StickerEditorUI.cs`
- `StoryPreviewUI.cs`

当前功能：

- 节点分类创建。
- GraphEdit 拖动、缩放和吸附。
- 连线、断线和删除。
- Undo/Redo、Ctrl+Z、Ctrl+Y。
- 小地图。
- 自动保存。
- 搜索节点。
- 音频、背景、立绘资源库。
- 角色和贴纸编辑。
- 预览。
- Visual Editing。

Unity 必须拆开以下对象：

```text
StoryGraphDocument       // 可序列化的纯数据
StoryGraphController     // 选择、连接、删除、保存协调
StoryNodeViewFactory     // 数据到 UI View
StoryConnectionRenderer   // 连线绘制
StorySelectionService     // 当前选区
StoryEditorCommandHistory // 命令、Undo、Redo
StoryProjectPersistence   // JSON、自动保存、导入导出
StoryPreviewController    // 复用 StoryRuntime
```

运行时编辑器和 Unity 原生编辑器应明确分开：

```text
Runtime Editor
  = uGUI 或运行时 UI Toolkit，可随游戏发布

Unity EditorWindow
  = 内容制作团队使用，只依赖 UnityEditor API
```

Unity 对照工程已经存在：

- [RuntimeNodeEditorCanvas.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\StoryEditor\RuntimeNodeEditorCanvas.cs)
- [RuntimeNodeViewUI.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\StoryEditor\RuntimeNodeViewUI.cs)
- [ProjectManager.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\StoryEditor\ProjectManager.cs)
- [StoryEditorAutoSaveController.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\StoryEditor\StoryEditorAutoSaveController.cs)
- [StoryPreviewController.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\StoryEditor\StoryPreviewController.cs)

但当前 Unity 工程没有真正检出 `EditorWindow`、`CustomEditor`、`GraphView`、`OnInspectorGUI` 等原生编辑器实现。因此“编辑器已迁移”不能作为完成条件。

需要特别修正 Godot 原有的耦合问题：

- `BaseNodeData` 不应直接创建或同步 GraphNode。
- GraphEdit 连线和节点的 `next` 字段只能有一个权威来源，建议以 Document 为权威。
- UI 不应通过子节点索引获取关键控件，改用序列化引用或命名组件。
- 禁止自连接、重复连接和悬空连接。
- Choice 选项删除必须同步数据。
- 资源引用使用稳定 ID 或包内相对路径，不使用显示名。
- 自动保存必须有防抖和失败提示。

## 9. ExtensionEditor 双编辑器之二：扩展编辑器

对应 Godot 文件：

- `scripts/ExtensionEditor/ExtensionEditorScreen.cs`
- `ExtensionEditorScreen.Behavior.cs`
- `ExtensionEditorScreen.Race.cs`
- `scripts/ExtensionEditor/Models/ExtensionManifest.cs`

当前功能包括：

- 创建扩展项目。
- 编辑基础 manifest 字段。
- 编辑角色 JSON。
- 编辑行为 JSON。
- 编辑比赛/训练等扩展数据。
- 导入图片和音频。
- 刷新扩展项目文件树。
- 导出 `.umaext`。

建议 Unity ExtensionEditor 至少包含：

```text
ExtensionProjectDocument
ManifestInspector
CharacterDataEditor
BehaviorRuleEditor
ResourceBrowser
DependencyEditor
OverrideEditor
PackageValidator
PackagePreview
UmaextExporter
```

导出前必须执行：

1. manifest schema 校验。
2. ID、版本、依赖和覆盖规则校验。
3. 所有声明文件是否存在。
4. 所有路径是否为相对路径。
5. 行为条件和动作是否使用白名单字段。
6. 角色 ID、故事 ID、行为 ID 是否冲突。
7. 图片和音频格式、文件大小是否符合限制。
8. 输出文件清单、大小和 SHA-256。
9. ZIP 根目录是否直接包含 `manifest.json`。

## 10. 扩展包运行时

当前真实架构是：

```text
manifest.json
+ Logic/behavior.json
+ Data/*.json
+ Story/*.json
+ Assets/媒体
```

历史文档曾描述 `IUmaPlugin`、`SecurityScanner`、`ModLoader`、DLL 和安全扫描，但这些不是当前工作树的运行时能力。Unity 不应把历史 DLL 设计当作普通用户扩展实现。

### 10.1 建议包格式

保留 Godot 兼容的核心布局：

```text
ExtensionRoot/
  manifest.json
  Data/
    actor_config.json
    simulation.json
    Characters/
  Logic/
    behavior.json
  Story/
    *.json
  Assets/
    Sprites/
    Backgrounds/
    Audio/
  README.md
  checksums.json
```

用户 `.umaext` 是数据分发包，Unity UPM Package 是开发者代码模块，二者必须分离。

### 10.2 推荐加载管线

```text
发现目录或 .umaext
  → PackageReader 读取 manifest
  → ManifestValidator 校验 schema、ID、版本、能力声明
  → 检查重复 ID
  → DependencyResolver 构建依赖图
  → 校验游戏版本和依赖版本
  → staging 目录安全解压
  → 校验文件数、单文件大小、总大小、路径和哈希
  → 构造 ExtensionRuntimeContext
  → 加载 behavior.json
  → 注册行为、角色、剧情和资源索引
  → 提交 Loaded 状态
```

建议状态拆为 `Discovered`、`Validated`、`Installed`、`Enabled`、`Loaded`、`Failed`、`Disabled`，不要把“发现”“安装”“启用”“加载”混成一个布尔值。

解压限制至少保留：

- 单文件 50 MB。
- 总解压 200 MB。
- 最多 1000 个文件。
- 拒绝绝对路径和 `..`。
- 目标路径必须位于 staging 根目录。

同时建议增加：压缩比检查、SHA-256、可选签名、损坏包诊断和 staging 原子提交。

### 10.3 依赖、覆盖和卸载

激活应当是事务：

```text
解析全依赖图
  → 全量预验证
  → 准备并解压所有包
  → 注册表变更
  → 重建内容索引
  → 提交 Loaded 状态
```

任一步失败都必须清理 staging、回滚注册表变更，并保留此前已经正常加载的扩展。

覆盖对象建议使用来源栈：

```text
definition_id
  → BuiltInDefinition
  → ExtensionA
  → ExtensionBOverride
```

停用 ExtensionB 后恢复 ExtensionA，而不是简单删除最终对象。来源栈应应用于角色、菜单、训练、比赛、行为和资源替换。

停用顺序：

```text
停止新资源请求
  → 从行为来源栈移除
  → 恢复被覆盖定义
  → 移除角色和故事索引
  → 释放 Sprite、AudioClip、AssetBundle
  → 清理缓存
  → 更新状态
```

### 10.4 行为 DSL

当前 `behavior.json` 根结构包含 `rules`、`items`、`menus`、`races`、`trainings`。条件支持 AND 组合和：

```text
== != > < >= <=
```

可读取属性包括：

```text
Game.CurrentTurn
Player.Money
Player.Stamina
Player.Energy
Uma.Mood
Uma.ActionStamina
Uma.Energy
Uma.Affection
Uma.Speed
Uma.Stamina
Uma.Power
Uma.Guts
Uma.Intelligence
Uma.CustomStats:<id>
Variable:<id>
```

当前主要动作包括 `DetailedStory`、`BriefStory`、`ChangeStat`。Unity 应继续采用白名单 DSL，不允许 JSON 直接指定方法名、反射类型名、Unity 对象路径或任意脚本。

当前文档和实现存在字段漂移，例如文档使用 `path`，当前实现和测试数据可能使用 `target_property`、`value_change`。迁移时应以当前源码、测试样例和实际包为准，建立一份规范 JSON Schema，并为旧字段提供显式兼容转换。

## 11. 角色、剧情和媒体资源注册

当前 `CharacterManager` 会从激活扩展读取：

```text
Data/actor_config.json
Data/Characters/<character>/actor_config.json
Data/Characters/<actorId>/simulation.json
Data/simulation.json
```

它建立角色 ID 到扩展根目录的映射，用于后续加载立绘和音频。

Unity 建议将内容注册分为：

```text
CharacterDefinition
CharacterSource[]
StoryEntry
ResourceReference
```

同一角色 ID 或故事 ID 冲突时，不能依赖文件系统枚举顺序。应使用内置优先级、明确 load order、manifest override 和扩展版本共同决定最终定义，并输出冲突诊断。

资源分两种：

### 内置内容

可使用 Addressables、ScriptableObject 缓存或 StreamingAssets。

### 外部用户扩展

从 `Application.persistentDataPath/Extensions` 读取，解压后使用包根目录加相对路径加载图片和音频，不依赖 AssetDatabase。

现有 Unity [ResourceProxy.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\Services\ResourceProxy.cs) 允许任意绝对路径，也允许 HTTP/HTTPS。这对不可信用户包是不安全的，必须改为：绝对路径只允许内部调试 API；网络资源默认禁止，只有 manifest 声明能力且用户授权时才允许；所有路径统一经过包根目录校验。

## 12. 存档和版本迁移

Godot 当前路径：

```text
user://autosave.sav
user://autosave.globals.json
user://save_slot_N.sav
```

`.sav` 实际流程是：

```text
GameState JSON
  → UTF-8
  → Zstd 压缩
  → 临时文件
  → 回读校验
  → 原子替换正式文件
```

Unity 对应使用 `Application.persistentDataPath`，不要使用 PlayerPrefs 保存完整游戏状态。

建议存档格式：

```json
{
  "schema_version": 3,
  "game_version": "1.0.0",
  "content_version": "...",
  "session": {},
  "player": {},
  "uma": {},
  "inventory": {},
  "global_variables": {},
  "story_flags": {},
  "extension_snapshot": {}
}
```

实现 `SaveMigrator`，按 `v1 → v2 → v3` 顺序迁移，不要在加载时大量散落版本判断。保存流程必须支持临时文件、回读校验、原子替换、损坏备份和错误通知。

存档验收至少包括：新游戏默认值、保存/加载后所有数值一致、损坏文件不崩溃、临时文件不覆盖有效存档、快速连续变更只触发一次防抖保存、扩展快照和当前启用状态可解释。

## 13. 翻译、音频和 Shader

### 13.1 翻译

源文件是 `translations/game_text.csv`，结构为：

```csv
id,zh,en
```

Godot `.translation` 是导入产物。Unity 应以 CSV 或 Localization Package 为规范源，逻辑和剧情数据只保存 key。

必须覆盖：菜单、剧情文本、选项、错误提示、Toast、扩展内容和动态生成的 UI。TMP 需要中文字体、英文 fallback 和缺字检查。运行时切换语言后，当前可见 UI 也应刷新。

### 13.2 音频

当前仓库 `audio/` 没有真实音频文件，只有代码和编辑器约定，因此迁移不能假设已有可导入音频资产。

Unity 使用 BGM、Voice、SFX 独立 AudioSource，并通过 AudioMixerGroup 控制主音量、音乐、语音和音效。BGM 默认循环；SFX 要支持等待播放完成和非阻塞两种模式；扩展音频使用外部物理路径异步加载；测试应准备临时 WAV/OGG fixture。

### 13.3 Shader

Godot Shader 包括：

```text
blur_glass_shader.gdshader
blur_shader.gdshader
circle_crop.gdshader
gradient_center_shader.gdshader
gradient_shader.gdshader
radius.gdshader
Text.gdshader
```

Unity 对应建议：

| Godot Shader | Unity 方案 |
|---|---|
| 玻璃/屏幕模糊 | URP Opaque Texture、Renderer Feature 或后处理 |
| 圆形裁剪 | Mask 或 UI Shader |
| 中心渐变 | Shader Graph / HLSL |
| 线性渐变 | Shader Graph / HLSL |
| 圆角边框 | UI Shader、9-slice 或自定义材质 |
| 文本渐变 | TMP Vertex Gradient 或 TMP Shader |

不能直接复制 Godot 的 `SCREEN_TEXTURE`、`textureLod` 和 uniform 名称。每个 Shader 都应记录参数映射、默认值、使用 Prefab、目标平台限制和截图验收结果。

## 14. Unity 工程现状和缺口

当前 Unity 对照工程已有：

- `Assets/Scripts/Core/Models/`
- `Assets/Scripts/Core/Serialization/JsonSerialization.cs`
- `Assets/Scripts/Game/`
- `Assets/Scripts/Extensions/`
- `Assets/Scripts/StoryEditor/`
- `Assets/Scripts/RuntimeEngine/`
- `Assets/Scripts/Services/`
- `Assets/Editor/UnityMigrationAssetGenerator.cs`

关键现有文件：

- [GameManager.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\Game\GameManager.cs)
- [TrainingModule.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\Game\TrainingModule.cs)
- [GameSaveService.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\Game\GameSaveService.cs)
- [BehaviorRegistry.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\Extensions\BehaviorRegistry.cs)
- [ExtensionProjectManager.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\Extensions\ExtensionProjectManager.cs)
- [RuntimeNodeEditorCanvas.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\StoryEditor\RuntimeNodeEditorCanvas.cs)
- [StoryPlayerEngine.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\RuntimeEngine\StoryPlayerEngine.cs)
- [AppRouter.cs](C:\Users\JuziD\proj\eradream-unity\Assets\Scripts\Services\AppRouter.cs)

尚未确认完成或需要补齐：

- Unity 原生 `EditorWindow` 和 GraphView 编辑器。
- 完整 StoryEditor 数据绑定、连线校验、Undo/Redo 和保存。
- 完整 ExtensionEditor manifest/行为/依赖/覆盖编辑。
- 事务性扩展激活和卸载。
- 覆盖来源栈。
- 外部资源路径沙箱和扩展级释放。
- 完整剧情播放器节点执行、音频和转场。
- 养成所有模块及回合行为的等价回归。
- Localization、TMP 字体 fallback 和缺字扫描。
- Shader 视觉对照。
- Unity Test Framework 全套 EditMode/PlayMode/Batchmode 测试。

## 15. 推荐实施顺序

### 阶段一：规格和基础设施

先固定 `schema_version`、manifest、节点 JSON、行为 JSON、资源路径规则和版本解析器。建立 `BootstrapScene`、`SceneRouter`、`FileSystemService`、`SettingsService`、`LocalizationService` 和结构化诊断。

完成标准：Unity 可以启动、加载设置、切换语言、切换 Welcome/MainMenu/Loading，并能输出明确诊断。

### 阶段二：纯领域层

实现 GameState、PlayerStats、UmaStats、Inventory、训练、休息、外出、工作、商店、事件和命令历史。让这些代码不依赖 Unity 场景。

完成标准：EditMode 测试覆盖所有边界，训练和资源消费结果与 Godot 测试样例一致。

### 阶段三：存档和回合

实现 SaveService、压缩、临时文件、校验、存档槽、迁移和 TurnSystem。将回合结束顺序写成单独测试。

完成标准：保存/读取、损坏恢复、回合开始防重入和自动存档全部通过。

### 阶段四：扩展数据运行时

实现 PackageReader、ManifestValidator、DependencyResolver、JsonMerger、BehaviorRegistry 和 CharacterRegistry。优先支持纯 JSON DSL。

完成标准：包发现、依赖、ZIP 安全、行为 Hook、override、停用恢复和失败回滚都有测试。

### 阶段五：剧情运行时

实现多态 StoryDocument、Validator、StoryRuntime、节点执行器、固定 1280x720 画布、文本、Choice、立绘、背景、音乐、音效和转场。

完成标准：旧版数组和新版包装格式都能读取；正式播放返回来源场景；预览不离开编辑器。

### 阶段六：玩家流程 UI

迁移 Simulation、StorySelector、SaveSlots、Training、Inventory、Scouting、Naming 和剧情触发。

完成标准：Welcome 到 Simulation 的完整流程可操作，养成回合和剧情触发符合顺序。

### 阶段七：双编辑器

先完成运行时 StoryEditor 和 ExtensionEditor，再视内容团队需求实现 Unity 原生 `EditorWindow`、GraphView、Inspector、AssetPostprocessor 和批量验证器。两套编辑器必须共享 Document、Validator 和 Preview Runtime。

完成标准：编辑、保存、重新打开、预览、撤销重做、导出 `.umaext` 和错误定位全部可用。

### 阶段八：资源、Shader 和跨平台验收

加入 Addressables、外部图片/音频、资源缓存、扩展卸载、URP Shader、Safe Area、中文字体和多分辨率适配。

完成标准：Windows、Android、超宽屏、竖屏、刘海屏和无边框窗口完成手工验收。

## 16. 测试与验收矩阵

### EditMode

- GameState 默认值和属性边界。
- PlayerStats、UmaStats、Inventory。
- Training、Rest、Work、Shop、Event。
- CommandHistory Execute/Undo/Redo/Batch。
- Story JSON 多态读取和旧格式兼容。
- JSON Merge、数组按 ID 合并和 Replace/Merge/Append 策略。
- Manifest schema、版本和依赖解析。
- Behavior 条件、概率、Hook、动作和卸载。
- 存档压缩、原子写入、迁移和损坏恢复。
- ZIP Slip、文件数、单文件和总大小限制。
- 包内资源路径边界。

### PlayMode

- Bootstrap 初始化顺序。
- Welcome → MainMenu → Loading 路由。
- StoryPlayer 对话、打字机、Choice、自动推进。
- 背景、立绘、转场、BGM、Voice、SFX。
- Simulation 面板打开和关闭。
- 回合推进、剧情触发和自动存档。
- 预览结束返回编辑器。
- 屏幕尺寸变化、安全区和语言切换。

### 外部包和手工验收

- 空包、坏 JSON、缺 manifest、非法 ID。
- 重复扩展 ID、循环依赖、缺依赖和版本不满足。
- 恶意 ZIP 路径、超大包和 staging 残留。
- 图片、音频损坏和格式不允许。
- 行为重复注册、override、停用后恢复。
- 角色和故事索引在启用/停用后正确变化。
- 1280x720、1920x1080、21:9、竖屏、刘海屏。
- 中文、英文、长文本、缺失翻译和字体缺字。

## 17. 迁移时不要照搬的现有问题

以下内容应在 Unity 中明确修正或隔离：

1. 不要让 `GameManager` 继续承担所有系统职责。
2. 不要把 Graph UI 状态和剧情数据字段作为两个平行真相源。
3. 不要依赖子节点索引和场景路径字符串获取关键 UI。
4. 不要依赖文件系统枚举顺序解决扩展、角色或故事冲突。
5. 不要静默覆盖重复扩展 ID。
6. 不要把扩展激活做成不可回滚的半事务流程。
7. 不要在停用扩展时只删除行为而忘记角色、故事索引和资源缓存。
8. 不要让 override 丢失原始定义和来源优先级。
9. 不要允许用户包引用任意绝对文件路径或默认访问网络。
10. 不要在普通 `.umaext` 中加载不可信 DLL。
11. 不要把 `.era` Godot PCK 当作 Unity 原生格式。
12. 不要让用户扩展最终只能变成 Unity `.asset`。
13. 不要把 `GrowthBonus` 留在模型中却不进入奖励公式，除非规格明确决定废弃它。
14. 不要在未验证前修复训练 UI 可能重复触发的问题；应先建立复现测试。
15. 不要把历史 DLL 文档写成当前扩展实现。

## 18. 可直接交给其他 AI 的执行指令

可以把下面的任务模板直接交给后续 AI：

```text
你正在把 EraDream 从 Godot 迁移到 Unity。请先阅读
C:\Users\JuziD\godot\Eradream\UNITY_MIGRATION_ARCHITECTURE_REPORT.md，
再阅读与当前任务相关的 Godot 源码和 Unity 对照文件。

必须遵守：
1. 以当前源码和测试为准，不把历史 DLL 文档当作当前功能。
2. 用户扩展默认是纯 JSON + 媒体的 .umaext 数据包，不加载不可信 DLL。
3. JSON 是跨引擎规范源，ScriptableObject 只能作为 Unity 编辑器缓存。
4. 所有外部资源引用必须是包内相对路径，并经过根目录边界校验。
5. 领域层不能依赖 MonoBehaviour 或 UI。
6. 节点数据、编辑器 View 和剧情执行器必须分离。
7. 先写针对行为契约的测试，再实现代码。
8. 每个 C# 文件保持在 1000 行以内，并为复杂代码块添加简短中文注释。

本次任务输出：
- 变更文件清单；
- 行为和数据格式说明；
- 测试命令和结果；
- 未解决风险；
- 不要修改与任务无关的用户已有改动。
```

分派任务时建议一次只交给一个 AI 一个边界，例如“只实现 ZIP 安全和 ManifestValidator”“只实现 Story JSON Converter”“只实现 TurnSystem 和 EditMode 测试”，避免多个 AI 同时修改同一组核心文件。

## 19. 现有验证限制

本次勘察没有改动源码。尝试构建 Godot 项目时，当前 CLI 无法解析 `Godot.NET.Sdk/4.6.1`，并且网络/NuGet 不可用；这不能作为源码本身构建失败的结论。当前环境中 `godot` 也不在 `PATH`，因此无法运行 `TestRunner.tscn`。

正式迁移前，应在安装 Godot 4.6.1 .NET、可用 NuGet 源和完整导入缓存的环境中重新执行：

```powershell
dotnet build EraDream.csproj
godot --path . --headless --scene res://scenes/TestRunner.tscn
```

Unity 侧应补充 Unity Test Framework 的 EditMode、PlayMode 和 batchmode 包测试，并把当前 Godot 中未默认挂载的扩展测试纳入正式回归计划。

## 20. 最终结论

EraDream 当前最准确的定位是：

> 一个带依赖、覆盖、行为 DSL、剧情图和媒体资源的可热加载数据包系统，同时包含运行时剧情编辑器和扩展项目编辑器。

Unity 迁移的关键不是逐文件翻译，而是先固定跨引擎数据契约，再把纯领域层、存档、扩展运行时、剧情运行时、玩家 UI、两个编辑器和资源系统按阶段实现。最先应该完成的是 Bootstrap、领域模型、存档、包验证和行为注册；之后再做剧情播放器和两个编辑器。这样后续 AI 才有稳定的接口、测试和完成标准可依赖。
