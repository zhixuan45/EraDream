using Godot;
using System;

public partial class SettingsOverlay
{
    private Control _overlayRoot;
    private CheckButton _darkModeToggle;
    private CheckButton _embeddedWindowToggle;

    private void InitUI()
    {
        Theme mainTheme = null;
        if (ResourceLoader.Exists("res://resources/theme_main.tres"))
        {
            mainTheme = GD.Load<Theme>("res://resources/theme_main.tres");
        }

        // 创建根节点
        _overlayRoot = new Control();
        _overlayRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlayRoot.Visible = false;
        AddChild(_overlayRoot);

        // 背景遮罩
        var bgRect = new ColorRect();
        bgRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bgRect.Color = new Color(0, 0, 0, 0.6f);
        _overlayRoot.AddChild(bgRect);

        // 居中容器
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlayRoot.AddChild(centerContainer);

        // 面板
        var panel = new PanelContainer();
        centerContainer.AddChild(panel);

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        styleBox.CornerRadiusTopLeft = 12;
        styleBox.CornerRadiusTopRight = 12;
        styleBox.CornerRadiusBottomLeft = 12;
        styleBox.CornerRadiusBottomRight = 12;
        styleBox.ContentMarginLeft = 40;
        styleBox.ContentMarginRight = 40;
        styleBox.ContentMarginTop = 30;
        styleBox.ContentMarginBottom = 30;
        panel.AddThemeStyleboxOverride("panel", styleBox);

        // 垂直容器
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        panel.AddChild(vbox);

        // 标题
        var titleLabel = new Label();
        titleLabel.Text = "设置"; 
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        vbox.AddChild(titleLabel);

        // 深色模式切换
        var hboxDarkMode = CreateSettingRow("深色模式", out _darkModeToggle, mainTheme);
        vbox.AddChild(hboxDarkMode);

        // 等待 SettingsManager 准备好后同步状态
        Callable.From(() => {
            _darkModeToggle.ButtonPressed = SettingsManager.Instance.IsDarkMode;
            _darkModeToggle.Toggled += OnDarkModeToggled;
        }).CallDeferred();

        // 嵌入窗口切换
        var hboxEmbedded = CreateSettingRow("嵌入式窗口", out _embeddedWindowToggle, mainTheme);
        vbox.AddChild(hboxEmbedded);

        Callable.From(() => {
            _embeddedWindowToggle.ButtonPressed = SettingsManager.Instance.IsEmbeddedSubwindows;
            _embeddedWindowToggle.Toggled += OnEmbeddedWindowToggled;
        }).CallDeferred();

        // 关闭按钮
        var closeButton = new Button();
        closeButton.Text = "关闭";
        closeButton.CustomMinimumSize = new Vector2(150, 40);
        if (mainTheme != null) closeButton.Theme = mainTheme;
        closeButton.Pressed += HideOverlay;
        vbox.AddChild(closeButton);
    }

    private HBoxContainer CreateSettingRow(string labelText, out CheckButton toggle, Theme theme = null)
    {
        var hbox = new HBoxContainer();
        var label = new Label();
        label.Text = labelText;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        hbox.AddChild(label);

        toggle = new CheckButton();
        if (theme != null) toggle.Theme = theme;
        hbox.AddChild(toggle);
        return hbox;
    }
}
