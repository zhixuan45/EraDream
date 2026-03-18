# 响应式 UI 系统与窗口大小自适应指南 (Responsive UI Guide)

本文档总结了本项目（基于 Godot 4.x 与 C#）中处理跨平台、多分辨率、多屏幕比例（横屏/竖屏）的界面自适应技术方案。通过这套机制，游戏可以在 PC 端（任意拖拽窗口）与移动端之间实现 UI 布局的智能切换，而**无需修改 Godot 引擎源代码**。

## 1. 核心原理：为什么不用绝对坐标？
传统的游戏开发喜欢使用固定的像素坐标（例如 `x: 500, y: 300`）。但这种做法在面对不同比例的屏幕时会带来毁灭性的灾难（元素出框或大量黑边）。

本系统采用了**“引擎拉伸 + 逻辑重排”**双轨并行的策略：
1. **引擎拉伸 (Engine Scaling)**：处理整体渲染像素的自适应。
2. **逻辑重排 (Logical Responsive)**：处理UI排版的物理逻辑变化（如长条形变方形、左侧对齐变居中）。

---

## 2. 引擎级配置 (project.godot)
我们在项目的核心设置中，将显示拉伸模式设置为 Godot 原生的 2D 友好模式：

```ini
[display]
window/stretch/mode="canvas_items"
window/stretch/aspect="expand"
```

- **`canvas_items`**：引擎会自动缩放所有基于 CanvasItem 的节点（所有的 2D 节点和 Control 节点），保证它们在不同分辨率下不会模糊。
- **`expand`**：当屏幕的宽高比与设计宽高比不一致时，**不产生黑边**，而是扩展摄像机的视野（Viewport）。这就要求我们的 UI 必须使用“锚点（Anchors）”来对齐屏幕边缘，而不是硬编码。

---

## 3. 全局响应式布局管理器 (ResponsiveManager.cs)
为了处理“宽屏变窄屏”时的**排版切换**问题，我们开发了 `ResponsiveManager.cs` 单例。

### 工作流：
1. **自动加载 (Autoload)**：在引擎启动时，该脚本作为全局单例运行。
2. **信号监听**：它监听了 `GetTree().Root.SizeChanged`，只要玩家拖动窗口边缘或手机屏幕旋转，就会触发回调。
3. **比例计算**：实时计算当前视口的宽高比：
   ```csharp
   ScreenOrientation newOrientation = CurrentScreenSize.X > CurrentScreenSize.Y 
       ? ScreenOrientation.Landscape 
       : ScreenOrientation.Portrait;
   ```
4. **事件分发**：当横竖状态发生改变时，触发全局事件 `OnOrientationChanged(bool isLandscape)`。

### 如何在 UI 中使用它？
任何需要响应式变化的界面（如主菜单、对话框、选项按钮）都可以订阅此事件。

**以 `MainMenuScreen.cs` 为例：**
```csharp
public override void _Ready()
{
    // 注册响应式布局回调
    ResponsiveManager.Instance.OnOrientationChanged += AdjustLayout;
    
    // 初始化时手动调用一次，确保UI立刻匹配当前屏幕状态
    AdjustLayout(ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
}

private void AdjustLayout(bool isLandscape)
{
    if (isLandscape) {
        // 横屏模式：按钮靠左排列，适合PC宽屏
        _vboxContainer.SetAnchorsPreset(LayoutPreset.CenterLeft);
        _vboxContainer.Position = new Vector2(100, _vboxContainer.Position.Y);
    } else {
        // 竖屏模式：按钮居中排列，适合手机竖屏
        _vboxContainer.SetAnchorsPreset(LayoutPreset.Center);
    }
}
```

---

## 4. 最佳实践规范
为了确保此系统完美运作，后续开发必须遵守以下规范：

1. **绝对禁用硬编码大小**：不要手动拖拽 Control 节点调整 Size，而是依靠容器（Container）自动推算大小。
2. **全面拥抱容器类**：
   - 使用 `VBoxContainer`（垂直排列）和 `HBoxContainer`（水平排列）来组合按钮。
   - 使用 `MarginContainer` 来控制页面边距（Padding）。
3. **锚点优先级**：非容器类的 UI 必须使用 Godot 的 **Anchors Preset**（锚点预设）。例如，右上角的金币数量框，其锚点必须设为 `Top Right`，这样无论屏幕多宽，它都会死死咬住右上角。

通过这套基于 C# 事件驱动的管理器和 Godot 原生容器机制的结合，我们的引擎已经具备了现代网页开发中常说的“响应式设计（Responsive Design）”能力。
