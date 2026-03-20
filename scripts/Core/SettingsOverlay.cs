using Godot;
using System;

public partial class SettingsOverlay : CanvasLayer
{
    public static SettingsOverlay Instance { get; private set; }

    private Control _overlayRoot;
    private CheckButton _darkModeToggle;

    public override void _Ready()
    {
        Instance = this;
        Layer = 90; // 低于 ErrorNotifier(100)，但高于普通UI

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

        // VBox
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        panel.AddChild(vbox);

        // 标题
        var titleLabel = new Label();
        titleLabel.Text = Tr("设置"); // 暂时硬编码中文，或使用翻译键
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        vbox.AddChild(titleLabel);

        // 深色模式切换
        var hboxDarkMode = new HBoxContainer();
        vbox.AddChild(hboxDarkMode);

        var darkModeLabel = new Label();
        darkModeLabel.Text = "深色模式";
        darkModeLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        darkModeLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        hboxDarkMode.AddChild(darkModeLabel);

        _darkModeToggle = new CheckButton();
        if (mainTheme != null) _darkModeToggle.Theme = mainTheme;
        // 等待 SettingsManager 准备好后同步状态
        Callable.From(() => {
            _darkModeToggle.ButtonPressed = SettingsManager.Instance.IsDarkMode;
            _darkModeToggle.Toggled += OnDarkModeToggled;
        }).CallDeferred();
        hboxDarkMode.AddChild(_darkModeToggle);

        // 关闭按钮
        var closeButton = new Button();
        closeButton.Text = "关闭";
        closeButton.CustomMinimumSize = new Vector2(150, 40);
        if (mainTheme != null) closeButton.Theme = mainTheme;
        closeButton.Pressed += HideOverlay;
        vbox.AddChild(closeButton);
    }

    private void OnDarkModeToggled(bool isToggled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.IsDarkMode = isToggled;
        }
    }

    public void ShowOverlay()
    {
        if (_darkModeToggle != null && SettingsManager.Instance != null)
        {
            _darkModeToggle.SetPressedNoSignal(SettingsManager.Instance.IsDarkMode);
        }
        _overlayRoot.Visible = true;
    }

    public void HideOverlay()
    {
        _overlayRoot.Visible = false;
    }
}
