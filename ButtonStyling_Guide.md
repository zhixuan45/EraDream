# 按钮样式管理指南

在本项目中，我们通过 Godot 的 **Theme（主题）** 系统统一管理界面样式。这意味着你只需要修改一个文件，所有引用该主题的按钮都会同步更新。

## 1. 快速修改圆角与颜色
所有样式逻辑都存储在 `res://resources/theme_main.tres` 中。

### 如何通过 Godot 编辑器修改：
1. 在 **FileSystem** 面板双击 `theme_main.tres`。
2. 在 **Inspector** 面板中，展开 `Button` -> `Styles`。
3. 你会看到 `Normal`、`Hover`、`Pressed` 等状态的 `StyleBoxFlat`：
   - **圆角**：展开 `Corner Radius` 修改 `Top Left` 等像素值。
   - **颜色**：展开 `Bg Color` 更改背景色。
   - **描边**：修改 `Border Width` 增加边框。

## 2. 后期更换整套样式
如果你想彻底更换 UI 风格，有以下几种方案：

### 方案 A：直接替换主题文件
你可以创建一个全新的主题 `.tres` 文件，然后修改 `MainMenuScreen.tscn` 根节点的 `Theme` 属性，将其指向新的文件。

### 方案 B：使用不同的 StyleBox 类型
当前使用的是 `StyleBoxFlat`（纯色+圆角）。
- 如果你想用**图片素材**作为按钮背景，可以将主题中的 `StyleBoxFlat` 替换为 `StyleBoxTexture`。
- 将你的按钮切图拖入 `Texture` 槽位，并设置好 `Nine Patch Stretch`（九宫格拉伸）的页边距。

## 3. 为特定按钮设置例外
如果你想让某个按钮拥有独特的样式（不跟随全局主题）：
1. 在场景中选中该按钮。
2. 在 **Inspector** 的 `Theme Overrides` 下，找到 `Styles`。
3. 勾选你想覆盖的状态（如 `Normal`），并分配一个新的 `StyleBox`。

## 4. 最佳实践建议
- **不要在每个按钮上单独设置样式**：尽可能通过根节点的 `Theme` 统一控制。
- **状态区分**：确保 `Hover`（鼠标悬停）和 `Pressed`（点击）状态有明显的视觉反馈（如变亮或变暗），以增强交互感。
- **响应式字体**：主题也可以统一设置 `Font Size`。目前我们在按钮节点上使用了 `theme_override_font_sizes`，后期也可以迁移到主题中统一管理。
