# EraDream Unity Engine Architecture & Framework

`EraDream Unity` 是从 Godot 4 (C#) 架构解耦移植的视觉小说 (Galgame/AVG) & 养成游戏数据驱动引擎与运行时节点编辑器框架。

## 目录与模块架构 (Directory Architecture)

```
unityproj/eradream-unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                           # 纯 C# 领域数据模型与核心算法 (零引擎依赖)
│   │   │   ├── Models/
│   │   │   │   ├── Nodes/                  # 基础节点 BaseNodeData 及全类型派生定义
│   │   │   │   ├── CharacterData.cs        # 角色及其差分表情定义
│   │   │   │   └── GameModels.cs           # 养成系统 (UmaStats, PlayerStats, Inventory)
│   │   │   ├── AppSettings.cs              # 应用偏好数据模型
│   │   │   ├── GlobalGameState.cs          # 运行时全局剧情变量
│   │   │   └── CommandHistory.cs           # Undo/Redo 撤销重做栈
│   │   ├── Services/                       # Unity 平台基础设施与资源服务
│   │   │   ├── SettingsManager.cs          # persistentDataPath/settings.json 存取
│   │   │   ├── ResourceProxy.cs            # 动态加载本地磁盘/包内图片与音频 (Sprite/AudioClip)
│   │   │   └── SafeAreaAdapter.cs          # uGUI Safe Area 移动端异形屏适配
│   │   ├── RuntimeEngine/                  # 运行时剧情解释播放器 (uGUI + TextMeshPro)
│   │   │   ├── StoryPlayerEngine.cs        # 节点链式遍历、打字机、音乐、分支选项播放器
│   │   │   └── CharacterSpriteUI.cs        # 立绘呈现、差分切换与拖拽定位
│   │   └── StoryEditor/                    # 选项 A：运行时 uGUI 可视化节点图编辑器
│   │       ├── RuntimeNodeEditorCanvas.cs  # 节点图画布、平移连线与 JSON 导入导出
│   │       └── RuntimeNodeViewUI.cs        # 节点卡片 UI 视图与拖拽
├── Packages/
│   └── manifest.json                       # Unity Package 依赖 (TextMeshPro, uGUI, 2D)
```

## 技术特性

1. **三层完全解耦设计**：
   - **Data Layer**: 所有 `BaseNodeData` 均采用纯 C# + `System.Text.Json` 标注，可在 Godot / Unity / 命令行工具中通用序列化。
   - **Service Layer**: 抽象 `ResourceProxy`，完美支持外置 `.era` / `.json` 剧本包与其对应的音视频资源动态装载。
   - **Presentation Layer**: 采用 Unity `uGUI` + `TextMeshPro` 搭建，符合【选项 A】运行时内置 Node 编辑器的全部功能需求。
