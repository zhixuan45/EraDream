using Godot;
using System;
using UmaEraArchive.Core;
using umaEraArchive.Game;

public partial class MainMenuScreen : Control
{
    private VBoxContainer _vboxContainer;

    // 初始化按钮并绑定事件
    // 主界面的核心交互逻辑
    public override void _Ready()
    {
        _vboxContainer = GetNode<VBoxContainer>("VBoxContainer");

        GetNode<Button>("VBoxContainer/StartButton").Pressed += OnStartPressed;
        GetNode<Button>("VBoxContainer/LoadButton").Pressed += OnLoadPressed;
        GetNode<Button>("VBoxContainer/SettingsButton").Pressed += OnSettingsPressed;
        GetNode<Button>("VBoxContainer/StoryButton").Pressed += OnStoryPressed;
        GetNode<Button>("VBoxContainer/EditorButton").Pressed += OnEditorPressed;
        GetNode<Button>("VBoxContainer/ExitButton").Pressed += OnExitPressed;

        // 注册响应式布局回调
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSafeAreaPaddingChanged += ApplySafeArea;
            ApplySafeArea(SettingsManager.Instance.SafeAreaPadding);
        }
        
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged += AdjustLayout;
            AdjustLayout(ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
        }
    }

    private void ApplySafeArea(float padding)
    {
        // 寻找根包装容器，通常主界面根节点就是控制节点
        // 直接设置全局 Offset 或内边距
        if (this is Control root)
        {
            // 通过调整宿主节点的 Margin 来避开安全区
            // 如果根节点是 Control，我们可以包装一层或手动设置 Offset
            // 这里使用更稳定的方式：设置 Position 和 Size 缩放，或者寻找 MarginContainer
            var marginContainer = GetNodeOrNull<MarginContainer>("MarginContainer");
            if (marginContainer != null)
            {
                marginContainer.AddThemeConstantOverride("margin_left", (int)padding);
                marginContainer.AddThemeConstantOverride("margin_right", (int)padding);
                marginContainer.AddThemeConstantOverride("margin_top", (int)padding);
                marginContainer.AddThemeConstantOverride("margin_bottom", (int)padding);
            }
        }
    }

    public override void _ExitTree()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSafeAreaPaddingChanged -= ApplySafeArea;
        }
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged -= AdjustLayout;
        }
    }

    private void AdjustLayout(bool isLandscape)
    {
        if (isLandscape)
        {
            // 横屏模式：靠左对齐，垂直居中
            _vboxContainer.SetAnchorsPreset(LayoutPreset.CenterLeft);
            _vboxContainer.Position = new Vector2(100, _vboxContainer.Position.Y);
        }
        else
        {
            // 竖屏模式：水平垂直居中
            _vboxContainer.SetAnchorsPreset(LayoutPreset.Center);
        }
    }

    private void OnStoryPressed()
    {
        LoadingScreen.TargetScene = "res://scenes/StorySelectorScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    private void OnEditorPressed()
    {
        LoadingScreen.TargetScene = "res://scenes/EditorScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    private void OnStartPressed()
    {
        // 标记为开启养成模式选择，并跳转剧本选择界面
        StorySelectorScreen.IsForSimulation = true;
        LoadingScreen.TargetScene = "res://scenes/StorySelectorScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    private void OnLoadPressed()
    {
        if (GameManager.Instance != null)
        {
            if (FileAccess.FileExists(GameManager.AutoSavePath))
            {
                GameManager.Instance.LoadGame(GameManager.AutoSavePath);
                LoadingScreen.TargetScene = "res://scenes/SimulationMainScreen.tscn";
                GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
            }
            else
            {
                GD.Print("No autosave found!");
                GetNode<ErrorNotifier>("/root/ErrorNotifier")?.ShowErrorDialog("未找到自动存档", "无法加载游戏进度");
            }
        }
    }

    private void OnSettingsPressed()
    {
        if (SettingsOverlay.Instance != null)
        {
            SettingsOverlay.Instance.ShowOverlay();
        }
        else
        {
            GD.PrintErr("SettingsOverlay instance is null!");
        }
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }
}
