using Godot;
using System;

public partial class SettingsOverlay : CanvasLayer
{
    public static SettingsOverlay Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        Layer = 90; // 低于 ErrorNotifier(100)，但高于普通UI
        
        // 调用分部类中定义的 UI 初始化方法
        InitUI();
    }

    private void OnDarkModeToggled(bool isToggled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.IsDarkMode = isToggled;
        }
    }

    private void OnEmbeddedWindowToggled(bool isToggled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.IsEmbeddedSubwindows = isToggled;
        }
    }

    public void ShowOverlay()
    {
        if (SettingsManager.Instance != null)
        {
            if (_darkModeToggle != null)
                _darkModeToggle.SetPressedNoSignal(SettingsManager.Instance.IsDarkMode);
            if (_embeddedWindowToggle != null)
                _embeddedWindowToggle.SetPressedNoSignal(SettingsManager.Instance.IsEmbeddedSubwindows);
        }
        _overlayRoot.Visible = true;
    }

    public void HideOverlay()
    {
        _overlayRoot.Visible = false;
    }
}
