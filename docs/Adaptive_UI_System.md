# EraDream 窗口自适应与响应式 UI 方案文档

本文档详细说明了项目中用于实现多平台（PC、移动端）窗口自适应、横竖屏切换以及安全区（刘海屏）适配的技术方案。

---

## 1. 核心组件 (Core Components)

### 1.1 ResponsiveManager (全局布局管理器)
*   **类名**：`EraDream.Core.ResponsiveManager`
*   **角色**：Autoload (单例)，UI 响应系统的“心脏”。
*   **核心逻辑**：
    *   监听 `GetTree().Root.SizeChanged` 信号。
    *   通过比较屏幕宽高比计算当前方向：`CurrentScreenSize.X > CurrentScreenSize.Y ? Landscape : Portrait`。
    *   当方向改变时，分发 `OnOrientationChanged(bool isLandscape)` 事件。
*   **用途**：驱动业务逻辑场景（如主菜单、养成界面）进行 UI 结构的重排。

### 1.2 SafeAreaAdapter (安全区适配器)
*   **类名**：`SafeAreaAdapter` (继承自 `MarginContainer`)
*   **角色**：通用 UI 容器，用于处理刘海屏、挖孔屏等安全区偏移。
*   **核心逻辑**：
    *   监听 `SettingsManager.Instance.OnSafeAreaPaddingChanged`。
    *   动态修改自身的 `margin_left/right/top/bottom` 常量覆盖。
*   **用法**：将场景中的根 UI 节点或主要内容容器设置为 `SafeAreaAdapter`，其子节点将自动避开屏幕边缘遮挡。

---

## 2. 响应式工作流程 (Adaptive Workflow)

### 2.1 容器优先原则
项目高度依赖 Godot 的 **Container** 系统实现基础自适应：
*   **BoxContainer (VBox/HBox)**：用于自动排列元素。
*   **GridContainer**：用于规整的属性面板（如马娘五维属性）。
*   **Size Flags**：通过设置 `Expand` 和 `Fill` 确保 UI 元素能随窗口缩放。

### 2.2 动态布局切换 (Dynamic Layout Switching)
对于无法单纯通过 Container 解决的横竖屏差异（例如：横屏左右分栏 vs 竖屏上下分栏），采用以下模式：

1.  **场景准备**：在 `.tscn` 中使用支持动态切换的节点（如 `BoxContainer`）或预留足够的层级。
2.  **代码介入**：
    ```csharp
    public override void _Ready() {
        if (ResponsiveManager.Instance != null) {
            ResponsiveManager.Instance.OnOrientationChanged += AdjustLayout;
            AdjustLayout(ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
        }
    }

    private void AdjustLayout(bool isLandscape) {
        if (isLandscape) {
            // 设置为横屏排版逻辑 (例如 BoxContainer.Vertical = false)
        } else {
            // 设置为竖屏排版逻辑 (例如 BoxContainer.Vertical = true)
        }
    }
    ```

---

## 3. 实现案例 (Implementation Examples)

### 3.1 养成主界面 (`SimulationMainScreen`)
*   **横屏**：使用 `HBoxContainer` 将“立绘”放在左侧，“属性面板”放在右侧。
*   **竖屏**：通过 `AdjustLayout` 将容器切换为垂直排列，变为“立绘在上，属性在下”的排版。

### 3.2 主菜单 (`MainMenuScreen`)
*   **横屏**：菜单按钮组靠左对齐。
*   **竖屏**：菜单按钮组居中对齐，以适应更窄的视觉重心。

---

## 4. 开发者准则
1.  **避免绝对坐标**：除非是特定的特效，否则严禁使用 `Position` 进行 UI 排版，必须使用 `Anchors` 和 `Containers`。
2.  **注册与销毁**：在 `_Ready` 中连接 `OnOrientationChanged`，务必在 `_ExitTree` 中进行解绑，防止内存泄漏。
3.  **安全区嵌套**：所有关键可交互 UI 必须包裹在 `SafeAreaAdapter` 或具备类似逻辑的容器内。
