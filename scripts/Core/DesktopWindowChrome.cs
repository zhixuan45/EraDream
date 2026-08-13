using Godot;

/// <summary>
/// 保留给现有 autoload 的兼容节点，窗口行为交由系统原生标题栏处理。
/// </summary>
public partial class DesktopWindowChrome : Node
{
    // 原生标题栏不占用游戏内容区域。
    public static float ContentTopInset => 0f;
}
