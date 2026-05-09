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
        GD.Print("[SaveSlotScreen] New Game button pressed.");
        if (GameManager.Instance != null)
        {
            // 初始化新游戏状态（跳过剧本选择，由后续签约流程决定）
            GameManager.Instance.StartNewGame(null);
            
            LoadingScreen.TargetScene = "res://scenes/UI/NamingScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
        }
        else
        {
            GD.PrintErr("[SaveSlotScreen] Cannot start new game: GameManager.Instance is NULL!");
            GetNode<ErrorNotifier>("/root/ErrorNotifier")?.ShowErrorDialog("系统错误", "游戏核心管理器 (GameManager) 未启动，请检查项目配置。");
        }
    }

    private void OnLoadGamePressed()
    {
        if (GameManager.Instance != null)
        {
            // 优先从 SettingsManager 获取最近存档路径，否则回退到自动存档
            string lastPath = SettingsManager.Instance?.LastSavePath;
            if (string.IsNullOrEmpty(lastPath) || !FileAccess.FileExists(lastPath))
            {
                lastPath = GameManager.AutoSavePath;
            }

            if (FileAccess.FileExists(lastPath))
            {
                GameManager.Instance.LoadGame(lastPath);
                LoadingScreen.TargetScene = "res://scenes/Game/SimulationMainScreen.tscn";
                GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
            }
            else
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier")?.ShowErrorDialog("未找到存档", "目前没有可用的存档文件。");
            }
        }
    }

    private void OnBackPressed()
    {
        LoadingScreen.TargetScene = "res://scenes/UI/MainMenuScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
    }

    public override void _ExitTree()
    {
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged -= AdjustLayout;
        }
    }
}
