using Godot;

/// <summary>
/// Provides desktop window controls when the native title bar is disabled.
/// </summary>
public partial class DesktopWindowChrome : Node
{
    private const float TitleBarHeight = 38f;
    private Window _rootWindow;

    public static float ContentTopInset => OS.HasFeature("mobile") ? 0f : TitleBarHeight;

    public override void _Ready()
    {
        if (OS.HasFeature("mobile")) return;

        _rootWindow = GetTree().Root;
        _rootWindow.Borderless = true;
        CallDeferred(MethodName.MaximizeWindow);
        CreateChrome();
    }

    private void MaximizeWindow()
    {
        _rootWindow.Mode = Window.ModeEnum.Maximized;
    }

    private void CreateChrome()
    {
        var layer = new CanvasLayer { Layer = 128 };
        AddChild(layer);

        var bar = new Panel
        {
            Name = "DesktopTitleBar",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        bar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        // 锚点预设会清空尺寸偏移，必须在其后明确设置标题栏高度。
        bar.OffsetBottom = TitleBarHeight;
        bar.AddThemeStyleboxOverride("panel", CreateBarStyle());
        layer.AddChild(bar);

        var title = new Label
        {
            Text = "EraDream",
            Position = new Vector2(16, 0),
            Size = new Vector2(260, TitleBarHeight),
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        bar.AddChild(title);

        var controls = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        controls.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        // 使用右侧偏移固定窗口控制区，避免锚点重置后落到左上角或失去高度。
        controls.OffsetLeft = -132;
        controls.OffsetBottom = TitleBarHeight;
        controls.AddThemeConstantOverride("separation", 0);
        bar.AddChild(controls);

        controls.AddChild(CreateWindowButton("-", "最小化窗口", () => _rootWindow.Mode = Window.ModeEnum.Minimized));
        controls.AddChild(CreateWindowButton("[]", "最大化或还原窗口", ToggleMaximize));
        controls.AddChild(CreateWindowButton("X", "退出", () => GetTree().Quit(), true));

        bar.GuiInput += HandleTitleBarInput;
    }

    private Button CreateWindowButton(string text, string tooltip, System.Action action, bool closeButton = false)
    {
        var button = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(44, TitleBarHeight),
            FocusMode = Control.FocusModeEnum.None
        };
        if (closeButton) button.AddThemeColorOverride("font_hover_color", new Color("ffffff"));
        button.Pressed += action;
        return button;
    }

    private void HandleTitleBarInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouse || mouse.ButtonIndex != MouseButton.Left || !mouse.Pressed) return;

        if (mouse.DoubleClick)
        {
            ToggleMaximize();
            return;
        }

        if (_rootWindow.Mode == Window.ModeEnum.Windowed) _rootWindow.StartDrag();
    }

    private void ToggleMaximize()
    {
        _rootWindow.Mode = _rootWindow.Mode == Window.ModeEnum.Maximized
            ? Window.ModeEnum.Windowed
            : Window.ModeEnum.Maximized;
    }

    private static StyleBoxFlat CreateBarStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color("171923"),
            BorderColor = new Color("3d4254"),
            BorderWidthBottom = 1
        };
    }
}
