using Godot;
using System;
using UmaEraArchive.Core;

namespace umaEraArchive.Game;

public partial class SimulationMainScreen : Control
{
    // UI 节点绑定
    private BoxContainer _topSection;
    private Label _turnInfo;
    private Label _playerInfo;
    
    // 马娘五维属性 Label
    private Label _speedLabel;
    private Label _staminaLabel;
    private Label _powerLabel;
    private Label _gutsLabel;
    private Label _intLabel;
    private Label _affectionLabel;

    // 按钮
    private Button _btnTrain;
    private Button _btnRest;
    private Button _btnSystem;

    public override void _Ready()
    {
        // 绑定节点
        _topSection = GetNode<BoxContainer>("SafeArea/MainVBox/TopSection");
        _turnInfo = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/TurnInfo");
        _playerInfo = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/PlayerInfo");

        _speedLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/SpeedLabel");
        _staminaLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/StaminaLabel");
        _powerLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/PowerLabel");
        _gutsLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/GutsLabel");
        _intLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/IntLabel");
        _affectionLabel = GetNode<Label>("SafeArea/MainVBox/TopSection/StatsVBox/UmaStatsGrid/AffectionLabel");

        _btnTrain = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnTrain");
        _btnRest = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnRest");
        _btnSystem = GetNode<Button>("SafeArea/MainVBox/BottomActions/BtnSystem");

        // 绑定事件
        _btnTrain.Pressed += OnTrainPressed;
        _btnRest.Pressed += OnRestPressed;
        _btnSystem.Pressed += OnSystemPressed;

        // 响应式布局注册
        if (ResponsiveManager.Instance != null)
        {
            ResponsiveManager.Instance.OnOrientationChanged += AdjustLayout;
            AdjustLayout(ResponsiveManager.Instance.CurrentOrientation == ScreenOrientation.Landscape);
        }

        UpdateUI();
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
        // 横屏时水平排列 (HBox)，竖屏时垂直排列 (VBox)
        _topSection.Vertical = !isLandscape;
        
        // 竖屏时调整间距
        _topSection.AddThemeConstantOverride("separation", isLandscape ? 30 : 10);
    }

    private void UpdateUI()
    {
        if (GameManager.Instance?.CurrentState != null)
        {
            var state = GameManager.Instance.CurrentState;
            
            // 更新回合与玩家信息
            _turnInfo.Text = $"当前回合: {state.CurrentTurn} / {state.MaxTurns}";
            _playerInfo.Text = $"{state.Player.PlayerName} 体力: {state.Player.Stamina} / {state.Player.MaxStamina} | 金钱: {state.Player.Money}";

            // 更新马娘五维
            var uma = state.Uma;
            _speedLabel.Text = $"速度: {uma.Speed}";
            _staminaLabel.Text = $"耐力: {uma.Stamina}";
            _powerLabel.Text = $"力量: {uma.Power}";
            _gutsLabel.Text = $"根性: {uma.Guts}";
            _intLabel.Text = $"智力: {uma.Intelligence}";
            _affectionLabel.Text = $"好感度: {uma.Affection}";
        }
    }

    private void OnTrainPressed()
    {
        // 目前简单接入训练模块逻辑并推进回合
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            GameManager.Instance.Training.ExecuteTraining(GameManager.Instance.CurrentState, TrainingType.Speed); // 默认练速
            GameManager.Instance.AdvanceTurn();
            UpdateUI();
        }
    }

    private void OnRestPressed()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            GameManager.Instance.Rest.ExecuteRest(GameManager.Instance.CurrentState);
            GameManager.Instance.AdvanceTurn();
            UpdateUI();
        }
    }

    private void OnSystemPressed()
    {
        // 返回主界面前自动存档
        GameManager.Instance?.AutoSave();
        LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }
}
