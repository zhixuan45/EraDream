using Godot;
using System;

/// <summary>
/// 用于适配安全区的包装容器 (MarginContainer)
/// 会自动根据 SettingsManager 中的安全区设置调整内边距
/// </summary>
public partial class SafeAreaAdapter : MarginContainer
{
    public override void _Ready()
    {
        // 监听安全区改变事件
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSafeAreaPaddingChanged += ApplySafeArea;
            // 初始化时应用一次
            ApplySafeArea(SettingsManager.Instance.SafeAreaPadding);
        }
    }

    public override void _ExitTree()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSafeAreaPaddingChanged -= ApplySafeArea;
        }
    }

    private void ApplySafeArea(float padding)
    {
        // 防止 NaN/Infinity 导致 (int)padding 返回 int.MinValue 破坏布局
        if (float.IsNaN(padding) || float.IsInfinity(padding)) padding = 0f;
        int p = Mathf.Clamp((int)padding, 0, 100);
        AddThemeConstantOverride("margin_left", p);
        AddThemeConstantOverride("margin_right", p);
        AddThemeConstantOverride("margin_top", p);
        AddThemeConstantOverride("margin_bottom", p);
    }
}
