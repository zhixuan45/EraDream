# 剧情编辑器节点开发规范 (Story Editor Node Specification)

为了确保剧情编辑器（Story Editor）的插件化扩展性及跨平台一致性，所有新节点类型的开发必须严格遵守以下规范。

## 1. 核心继承架构 (Core Inheritance)
所有节点数据类必须继承自 `BaseNodeData`，并实现以下两个核心方法：
- `CreateGraphNode(GraphEdit host)`：定义节点在编辑器中的 UI 表现。
- `SyncFromView(GraphNode view)`：从 UI 界面同步数据回 C# 对象。

## 2. 数据模型与序列化 (Data Model & Serialization)
- **多态注册**：在 `BaseNodeData.cs` 的顶部，必须通过 `[JsonDerivedType]` 注册新节点类，以确保 JSON 读写正常。
- **ID 唯一性**：节点 ID 必须由 `BaseNodeData` 的构造函数生成（默认 `Guid`），严禁手动硬编码。
- **属性命名**：属性应清晰反映其在剧情中的作用（如 `CharacterName`, `AudioFile`）。

## 3. UI 交互规范 (UI Interaction)
- **功能头 (Header)**：必须在 `CreateGraphNode` 中首先调用 `SetupBaseNodeUI(node)`，以确保“≡ (详细设置)”和“× (删除)”按钮的统一。
- **槽位设置 (Slots)**：
  - 输入槽（左侧）：通常设置在 `Slot 0`，代表剧情流的进入。
  - 输出槽（右侧）：根据节点逻辑设置（如 `Dialogue` 只有 1 个，`Choice` 有多个，`Branch` 有 2 个）。
- **尺寸控制**：
  - 必须通过 `ResetNodeSize` 逻辑来处理折叠/展开。
  - 展开状态应至少包含 `CustomMinimumSize` 的设置，并调用 `node.Size = Vector2.Zero` 以强制布局刷新。

## 4. 资源选取 (Resource Management)
- **音频/图像资源**：严禁使用 `LineEdit` 让用户手动输入文件路径。
- **推荐方案**：使用 `AudioLibrary.PopulateOptionButton` 将 `OptionButton` 填充为项目中的资源列表，并自动处理“资源为空”的警告提醒。

## 5. 扩展性原则 (Extensibility)
- **详细面板 (Detail Panel)**：复杂的、非实时的配置（如情感、立绘偏移、音效路径）应放在 `_detailPanel` 中，由“≡”按钮控制展开。
- **原子化设计**：一个节点只负责一类逻辑（如：声音节点只管播音乐，对话节点只管文字）。不要试图在一个节点里塞入所有功能。
