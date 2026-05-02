using Godot;
using System;
using UmaEraArchive.Core;
using umaEraArchive.Game;

public partial class SaveSlotScreen : Control
{
    private Button _btnNewGame;
    private Button _btnLoadGame;
    private Button _btnBack;

    public override void _Ready()
    {
        _btnNewGame = GetNode<Button>("VBoxContainer/BtnNewGame");
        _btnLoadGame = GetNode<Button>("VBoxContainer/BtnLoadGame");
        _btnBack = GetNode<Button>("VBoxContainer/BtnBack");

        _btnNewGame.Pressed += OnNewGamePressed;
        _btnLoadGame.Pressed += OnLoadGamePressed;
        _btnBack.Pressed += OnBackPressed;

        // 响应式布局
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged += AdjustLayout;
            AdjustLayout(ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
        }
    }

    private void AdjustLayout(bool isLandscape)
    {
        var container = GetNode<VBoxContainer>("VBoxContainer");
        if (isLandscape)
        {
            container.SetAnchorsPreset(LayoutPreset.Center);
        }
        else
        {
            container.SetAnchorsPreset(LayoutPreset.Center);
        }
    }

    private void OnNewGamePressed()
    {
        if (GameManager.Instance != null)
        {
            // 初始化新游戏状态（跳过剧本选择，由后续签约流程决定）
            GameManager.Instance.StartNewGame(null);
            
            LoadingScreen.TargetScene = "res://scenes/NamingScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        }
    }

    private void OnLoadGamePressed()
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
                GetNode<ErrorNotifier>("/root/ErrorNotifier")?.ShowErrorDialog("未找到存档", "目前没有可用的存档文件。");
            }
        }
    }

    private void OnBackPressed()
    {
        LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }

    public override void _ExitTree()
    {
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged -= AdjustLayout;
        }
    }
}
