# EraDream Bug 分析报告

> 扫描日期: 2026-06-13 | 范围: 全代码库 ~70个源文件 | 发现问题: 107个

## 项目概要

- **引擎**: Godot 4.6 + C# (.NET 8, Mono)
- **类型**: 视觉小说(GalGame)引擎 + 养成游戏框架
- **子系统**: 剧情播放引擎 | 节点式编辑器 | 养成模拟 | 核心基础设施 | 扩展/Mod系统

---

## 严重度汇总

| 等级 | 数量 | 说明 |
|------|------|------|
| **CRITICAL** | 5 | 导致崩溃/数据损毁/功能完全失效 |
| **HIGH** | 15 | 导致功能严重异常 |
| **MEDIUM** | 35 | 特定条件下行为异常 |
| **LOW** | 52 | 代码质量问题 |

---

## 子系统详细报告

| 文档 | 子系统 | 问题数 | CRITICAL |
|------|--------|--------|----------|
| [01_story_player_engine.md](./01_story_player_engine.md) | 剧情播放引擎 | 19 | 1 |
| [02_editor_nodes.md](./02_editor_nodes.md) | 编辑器节点系统 | 21 | 0 |
| [03_game_system.md](./03_game_system.md) | 养成游戏系统 | 43 | 3 |
| [04_core_infrastructure.md](./04_core_infrastructure.md) | 核心基础设施 | 18 | 0 |
| [05_extensions_mods.md](./05_extensions_mods.md) | 扩展/Mod系统 | 19 | 1 |

---

## 系统架构与Bug热力图

```
EraDream/                        Bug密度
├── scripts/
│   ├── StoryPlayerEngine.cs          🔴🔴 (19个)
│   ├── Editor/
│   │   ├── EditorScreen.cs           🔴   (集成点，耦合多)
│   │   ├── StoryNodeManager.cs       🔴   (静默失败)
│   │   └── Nodes/                    🟡   (21个，分散)
│   ├── Game/                         🔴🔴🔴 (43个，最密集)
│   │   ├── GameManager.cs            🔴🔴🔴 (状态机核心)
│   │   ├── SimulationMainScreen.cs   🔴🔴 (UI+逻辑耦合)
│   │   ├── Modules/TrainingModule.cs 🔴   (数值溢出)
│   │   ├── Modules/EventModule.cs    🔴   (跨模块副作用)
│   │   └── Modules/InventoryModule.cs🔴   (集合修改异常)
│   ├── Core/                         🔴   (18个)
│   │   ├── FileIOManager.cs          🔴🔴 (静默写入失败)
│   │   ├── ResponsiveManager.cs      🔴   (事件泄漏)
│   │   ├── SettingsOverlay.UI.cs     🔴   (信号泄漏 x6)
│   │   └── SettingsManager.cs        🔴   (静默数据丢失)
│   └── Core/Extensions/              🔴🔴 (19个)
│       ├── ExtensionManager.cs       🔴🔴 (加载流程核心)
│       ├── SecurityScanner.cs        🔴   (扫描可绕过)
│       └── ExtensionJsonMerger.cs    🟡   (死代码)
```

---

## 5个全局性系统缺陷

### 1. 信号/事件生命周期管理
**模式**: ` += lambda` 从不调用 `-=`
**影响文件**: ResponsiveManager, SettingsOverlay.UI, TrainingMenuUI, ErrorNotifier, DebugConsole, SimulationMainScreen, StoryPlayerEngine
**后果**: 节点释放后悬挂回调 → 访问已释放对象 → 崩溃

### 2. 静默失败模式
**模式**: catch空块 | null检查后无日志 | 值解析失败默认0/空
**影响文件**: StoryNodeManager, FileIOManager, SettingsManager, BehaviorRegistry, ExtensionManager
**后果**: 用户数据丢失无感知，bug难以排查

### 3. 单例初始化时序
**模式**: `Instance = this` 在 `_Ready()` 中设置
**影响文件**: GameManager, SettingsManager, GlobalGameState
**后果**: 其他 Autoload 在 `_Ready` 中访问 Instance 时可能为 null

### 4. 文件IO无原子性
**模式**: 直接 `FileAccess.Open + StoreString` 覆盖写入
**影响文件**: FileIOManager, StoryNodeManager, SettingsManager
**后果**: 写入过程中崩溃 → 文件损坏且无备份恢复

### 5. Godot API线程安全
**模式**: public方法直接调用 Godot API，未使用 CallDeferred
**影响文件**: ErrorNotifier, FileIOManager, ResourceProxy, UIUtils, SettingsManager
**后果**: 非主线程调用 → Godot运行时异常

---

## 给修复AI的阅读建议

1. **先读** README.md（本文）了解全局
2. **按子系统依次修复**，建议顺序: Core → Game → StoryPlayer → Editor → Extensions
3. 每个子系统的文档包含: 文件路径、行号、问题描述、修复建议、严重度
4. 修复 CRITICAL 问题后立即运行测试验证
5. 注意跨文件关联问题（标记了 [关联: 文件名] 的问题需同步修改）
