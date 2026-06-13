# 02 - 编辑器节点系统 (Editor/Nodes)

> 目录: `scripts/Editor/Nodes/` + `scripts/Editor/StoryNodeManager.cs` + `scripts/Editor/EditorScreen.cs` | 问题: 21个 | CRITICAL: 0 | HIGH: 3

---

## [HIGH] #1 - ChoiceNode 删除按钮不移除 Options 列表项

**文件**: `scripts/Editor/Nodes/ChoiceNode.cs` | **行号**: 87-93

**问题**: 选项行的 "x" 删除按钮点击时只移除了视觉上的 `HBoxContainer`，但**未从 `Options` 列表中移除对应 `ChoiceItem`**。如果在删除选项后、下次 `SyncFromView` 前保存，被删除的选项会被持久化。

```csharp
// 当前代码（简化）:
var deleteBtn = new Button { Text = "x" };
deleteBtn.Pressed += () => {
    optionRow.QueueFree();
    // BUG: Options.Remove(choiceItem) 缺失!
};
```

**修复建议**:
```csharp
var capturedItem = choiceItem;  // 捕获变量
deleteBtn.Pressed += () => {
    Options.Remove(capturedItem);
    optionRow.QueueFree();
    node.Size = Vector2.Zero;  // 触发重新布局
};
```

---

## [HIGH] #2 - StoryNodeManager.LoadProject 静默 catch-all

**文件**: `scripts/Editor/StoryNodeManager.cs` | **行号**: 63

**问题**:
```csharp
try {
    // JSON 反序列化
} catch {
    return new List<BaseNodeData>();  // 空列表，无日志，无错误提示
}
```
文件损坏、schema不兼容、重命名的类等所有错误被静默吞掉，用户看到空白画布，无任何提示。

**修复建议**:
```csharp
catch (Exception ex) {
    GD.PushError($"[StoryNodeManager] Failed to load project: {ex.Message}");
    ErrorNotifier.Instance?.ShowErrorDialog("加载失败", $"剧情文件加载失败:\n{ex.Message}");
    return new List<BaseNodeData>();
}
```

---

## [HIGH] #3 - 未验证的 GraphEdit 强制转换

**文件**: `scripts/Editor/Nodes/BranchNode.cs:53` + `scripts/Editor/Nodes/ChoiceNode.cs:120`

**问题**:
```csharp
GraphEdit graph = (GraphEdit)view.GetParent();  // 如果父节点不是 GraphEdit，直接 InvalidCastException
```

**修复建议**:
```csharp
var graph = view.GetParent() as GraphEdit;
if (graph == null) {
    GD.PushError($"[{nameof(BranchNode)}] Parent is not a GraphEdit");
    return;
}
```

---

## [MEDIUM] #4 - 文件写入失败无错误反馈

**文件**: `scripts/Editor/StoryNodeManager.cs` | **行号**: 18-22

**问题**:
```csharp
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
if (file != null) { file.StoreString(json); }
// file 为 null 时（权限不足/磁盘满）静默无输出
```

**修复建议**:
```csharp
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
if (file == null) {
    GD.PushError($"[StoryNodeManager] Failed to open file for writing: {path}");
    return false;
}
file.StoreString(json);
return true;
```

---

## [MEDIUM] #5 - 无 Schema 版本管理

**文件**: `scripts/Editor/StoryNodeManager.cs` | **行号**: 54-66

**问题**: 序列化的 JSON 不含版本号。未来任何数据结构变更（加字段、改类型、重命名）都会导致所有已有存档无法加载且无法迁移。

**修复建议**: 在序列化数据最外层加入 `"schema_version": 1`，加载时检查并执行逐版本迁移逻辑。

---

## [MEDIUM] #6 - BranchNode.ComparisonValue 未验证

**文件**: `scripts/Editor/Nodes/BranchNode.cs` | **行号**: 8

**问题**: `ComparisonValue` 是裸 string，默认 `"10"`。用户可以输入 `"abc"` 并保存，运行时 `int.Parse`/`float.Parse` 会抛异常。

**修复建议**: 在 `SyncFromView` 中验证:
```csharp
if (!float.TryParse(_comparisonInput.Text, out _)) {
    _comparisonInput.Text = "10";  // 回退为默认值
    ErrorNotifier.Instance?.ShowToast("条件值必须是数字");
}
```

---

## [MEDIUM] #7 - 资源引用使用显示名称而非持久ID

**文件**: `scripts/Editor/Nodes/BackgroundNode.cs:50-53` + `scripts/Editor/Nodes/MusicNode.cs:115-118`

**问题**: 背景文件和音频文件通过 `OptionButton.GetItemText()` 获取**显示名称**来引用。文件重命名或跨机器迁移后，引用变为死链。

**修复建议**: 使用资源文件路径或 GUID 作为标识符，显示名称仅用于UI展示。

---

## [MEDIUM] #8 - 允许节点自连接

**文件**: `scripts/Editor/EditorScreen.cs` | **行号**: 527

**问题**: `ConnectNodesUndoable` 中 `f == t` 时直接连接节点到自身，形成无限循环且无任何警告。

**修复建议**:
```csharp
if (fromNode == toNode) {
    ErrorNotifier.Instance?.ShowToast("不能将节点连接到自身");
    return;
}
```

---

## [MEDIUM] #9 - 允许重复连接

**文件**: `scripts/Editor/EditorScreen.cs` | **行号**: 527

**问题**: 同一输出端口到同一输入端口的连接可以被创建多次，导致连接列表中出现重复。

**修复建议**: 连接前检查是否已存在相同连接:
```csharp
if (graph.IsNodeConnected(fromNode, fromPort, toNode, toPort)) {
    return;  // 已存在，跳过
}
```

---

## [MEDIUM] #10 - ChoiceNode 第一个选项端口的无用输入端口

**文件**: `scripts/Editor/Nodes/ChoiceNode.cs` | **行号**: 84-85

**问题**:
```csharp
bool enableInput = (index == 0);  // 仅第一个选项有输入端口
```
这个输入端口没有对应的数据读取逻辑（`SyncFromView` 不处理选项的输入连接），但UI上显示为可连接，会误导用户。

**修复建议**: 移除所有选项的左端口，或为输入连接定义明确的行为。

---

## [MEDIUM] #11 - Child Index 硬编码假设（所有节点）

**文件**: 所有 `SyncFromView` 方法

**问题**: 每个节点的 `SyncFromView` 通过子节点索引访问控件（如 `Child 1 is OptionButton`）。如果 `CreateGraphNode` 中的子节点顺序被修改，数据同步会悄悄读取到错误的值。

**影响文件及关键索引**:
| 文件 | 索引假设 | 风险控件 |
|------|---------|---------|
| `EndNode.cs:50` | Child[1].Child[0] | OptionButton |
| `EndNode.cs:52` | Child[1].Child[1] | LineEdit |
| `DialogueNode.cs:90` | Child[1] | OptionButton (角色选择) |
| `DialogueNode.cs:94` | Child[2] | TextEdit (内容) |
| `NarrativeNode.cs:72` | Child[1] | TextEdit (内容) |
| `MusicNode.cs:121` | Child[3].Child[1] | HSlider (音量) |
| `ChoiceNode.cs:114-131` | Child[1].Children | 选项行迭代 |

**修复建议**: 将控件引用缓存为成员字段（如 `_characterSelector`），在 `CreateGraphNode` 中赋值，`SyncFromView` 中直接使用。

---

## [LOW] #12~21 - 其他低优先级问题

| # | 文件 | 行号 | 问题 | 修复 |
|---|------|------|------|------|
| 12 | ChoiceNode.cs | 98 | `CustomMinimumSize.Y = 0` 允许节点缩到零高度 | 设最小高度 ≥ 30 |
| 13 | DialogueNode.cs | 81 | 重置 `Size = Vector2.Zero` | 统一节点大小策略 |
| 14 | NarrativeNode.cs | 63 | 设置显式 `Size` | 统一节点大小策略 |
| 15 | StickerNode.cs | 23 | 硬编码中文标题 `贴纸 (Sticker)` | 使用 `Tr()` 本地化 |
| 16 | ValueNode.cs | 27 | 硬编码中文标题 `数值变更` | 使用 `Tr()` 本地化 |
| 17 | EndNode.cs | 7 | `CustomScenePath` 不验证路径存在 | 加 FileAccess.FileExists 检查 |
| 18 | ValueNode.cs | 12 | `CustomId` 在非 Custom 模式时仍被序列化 | 序列化前判断 TargetAttribute |
| 19 | MusicNode.cs | 81-86 | `_previewPlayer` 可能未释放 | 在 `_ExitTree` 中移除并释放 |
| 20 | BaseNode.cs | 27-31 | 无 Clone/Copy 支持 | 如需复制功能时实现 ICloneable |
| 21 | ChoiceNode.cs | 15-19 | `ChoiceItem` 是可变引用类型 | 如需复制则实现深拷贝 |
