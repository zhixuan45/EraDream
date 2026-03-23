using Godot;
using System;

namespace umaEraArchive.Game;

public partial class SimulationMainScreen : Control
{
    private Label _infoLabel;
    private Button _nextTurnBtn;
    private Button _backBtn;

    public override void _Ready()
    {
        _infoLabel = GetNode<Label>("VBoxContainer/InfoLabel");
        _nextTurnBtn = GetNode<Button>("VBoxContainer/NextTurnBtn");
        _backBtn = GetNode<Button>("VBoxContainer/BackBtn");

        _nextTurnBtn.Pressed += OnNextTurnPressed;
        _backBtn.Pressed += OnBackPressed;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (GameManager.Instance?.CurrentState != null)
        {
            var state = GameManager.Instance.CurrentState;
            _infoLabel.Text = $"当前回合: {state.CurrentTurn}\n" +
                              $"体力: {state.Player.Stamina} / {state.Player.MaxStamina}\n" +
                              $"金钱: {state.Player.Money}";
        }
        else
        {
            _infoLabel.Text = "游戏状态未初始化";
        }
    }

    private void OnNextTurnPressed()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CurrentState.IsGameOver)
        {
            // 作为测试，点击推进时顺带睡一觉并触发回合更替
            GameManager.Instance.Rest.ExecuteRest(GameManager.Instance.CurrentState);
            GameManager.Instance.AdvanceTurn();
            UpdateUI();
        }
    }

    private void OnBackPressed()
    {
        // 返回主界面
        LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }
}
