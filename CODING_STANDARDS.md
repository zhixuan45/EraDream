# 项目编码规范 (Coding Standards)

为了确保 C# 代码在 Godot 4 环境下的健壮性与可维护性，所有开发人员必须遵守以下规范。

## 1. 命名空间冲突处理 (Namespace Ambiguity)

当同时使用 `using Godot;` 和 `using System.IO;` 时，某些类名会发生冲突。

- **FileAccess**: 
  - 禁止直接使用 `FileAccess`。
  - **必须** 使用 `Godot.FileAccess` 来调用 Godot 的文件系统 API（如 `FileExists`, `Open`）。
  - `System.IO.FileAccess` 仅作为枚举（Read/Write）使用时限定前缀。
- **Directory**:
  - 优先使用 `System.IO.Directory` 处理物理磁盘操作。
  - 使用 Godot 接口时使用 `Godot.DirAccess`。

## 2. 路径处理 (Path Handling)

- **绝对路径**: 在调用任何 `System.IO` 方法前，必须使用 `ProjectSettings.GlobalizePath()` 将 `res://` 或 `user://` 转换为绝对路径。
- **路径拼接**: 统一使用 `System.IO.Path.Combine()` 或 Godot 的 `PathJoin()`，禁止手动拼接 `/` 或 `\`。

## 3. 节点数据同步

- **SyncFromView**: 节点数据类必须实现此方法，将 GraphNode 的 UI 状态同步至 Data 对象。
- **坐标存储**: 节点位置 `PosX`, `PosY` 在序列化时必须使用 `Math.Round(val, 2)` 保留两位小数。

## 4. 预览模式逻辑

- **IsPreviewMode**: 播放引擎应检测此标记。在预览模式下，点击结束节点应通过 `StoryFinished` 信号通知 UI 关闭预览窗口，而非跳转至主菜单。
