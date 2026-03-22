using Godot;
using System;

public partial class SettingsOverlay
{
    private Control _overlayRoot;
    private CheckButton _darkModeToggle;
    private CheckButton _embeddedWindowToggle;
    private CheckButton _mouseCursorToggle;
    private HSlider _safeAreaSlider;
    private ColorRect _safeAreaPreview;

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

        // --- 安全区预览层 ---
        _safeAreaPreview = new ColorRect();
        _safeAreaPreview.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _safeAreaPreview.Color = new Color(1, 0, 0, 0.2f); // 半透明红色效果
        _safeAreaPreview.MouseFilter = Control.MouseFilterEnum.Ignore;
        _safeAreaPreview.Visible = false;
        _overlayRoot.AddChild(_safeAreaPreview);

        // 居中容器
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlayRoot.AddChild(centerContainer);

        // 面板
        var panel = new PanelContainer();
        panel.MouseFilter = Control.MouseFilterEnum.Stop;
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

        // 鼠标光标切换
        var hboxMouse = CreateSettingRow("显示鼠标光标", out _mouseCursorToggle, mainTheme);
        vbox.AddChild(hboxMouse);

        Callable.From(() => {
            _mouseCursorToggle.ButtonPressed = SettingsManager.Instance.ShowMouseCursor;
            _mouseCursorToggle.Toggled += OnMouseCursorToggled;
        }).CallDeferred();

        // 安全区调整
        var safeAreaVBox = new VBoxContainer();
        var safeAreaLabel = new Label { Text = "安全区适配 (圆角/刘海)" };
        safeAreaLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        safeAreaVBox.AddChild(safeAreaLabel);

        _safeAreaSlider = new HSlider
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 1,
            CustomMinimumSize = new Vector2(200, 30)
        };
        safeAreaVBox.AddChild(_safeAreaSlider);
        vbox.AddChild(safeAreaVBox);

        Callable.From(() => {
            _safeAreaSlider.Value = SettingsManager.Instance.SafeAreaPadding;
            _safeAreaSlider.ValueChanged += OnSafeAreaSliderChanged;
            _safeAreaSlider.DragStarted += () => { _safeAreaPreview.Visible = true; UpdatePreview((float)_safeAreaSlider.Value); };
            _safeAreaSlider.DragEnded += (changed) => _safeAreaPreview.Visible = false;
        }).CallDeferred();

        // 关闭按钮
        var closeButton = new Button();
        closeButton.Text = "关闭";
        closeButton.CustomMinimumSize = new Vector2(150, 40);
        if (mainTheme != null) closeButton.Theme = mainTheme;
        closeButton.Pressed += HideOverlay;
        vbox.AddChild(closeButton);
    }

    private void OnMouseCursorToggled(bool isToggled)
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ShowMouseCursor = isToggled;
        }
    }

    private void OnSafeAreaSliderChanged(double value)
    {
        if (SettingsManager.Instance != null)
        {
            float padding = (float)value;
            SettingsManager.Instance.SafeAreaPadding = padding;
            UpdatePreview(padding);
        }
    }

    private void UpdatePreview(float padding)
    {
        if (_safeAreaPreview != null)
        {
            // 使用 Offset 来调整 Margin
            _safeAreaPreview.OffsetLeft = padding;
            _safeAreaPreview.OffsetTop = padding;
            _safeAreaPreview.OffsetRight = -padding;
            _safeAreaPreview.OffsetBottom = -padding;
        }
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
