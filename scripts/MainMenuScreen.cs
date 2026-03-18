using Godot;
using System;
using UmaArchive.Core;

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
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged += AdjustLayout;
            // 初始化调用一次
            AdjustLayout(ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
        }
    }

    public override void _ExitTree()
    {
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
        GetTree().ChangeSceneToFile("res://scenes/StorySelectorScreen.tscn");
    }

    private void OnEditorPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/EditorScreen.tscn");
    }

    private void OnStartPressed()
    {
        GD.Print(Tr("KEY_START_GAME"));
    }

    private void OnLoadPressed()
    {
        GD.Print(Tr("KEY_LOAD_GAME"));
    }

    private void OnSettingsPressed()
    {
        GD.Print(Tr("KEY_SETTINGS"));
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }
}
