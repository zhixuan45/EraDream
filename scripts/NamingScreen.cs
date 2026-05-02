using Godot;
using System;
using umaEraArchive.Game;

public partial class NamingScreen : Control
{
    private LineEdit _nameEdit;
    private Button _btnConfirm;

    public override void _Ready()
    {
        _nameEdit = GetNode<LineEdit>("VBoxContainer/NameEdit");
        _btnConfirm = GetNode<Button>("VBoxContainer/BtnConfirm");

        _btnConfirm.Pressed += OnConfirmPressed;
        
        // 默认焦点
        _nameEdit.GrabFocus();
    }

    private void OnConfirmPressed()
    {
        string newName = _nameEdit.Text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            newName = "训练员"; // 默认名字
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != null)
        {
            // 设置玩家姓名
            GameManager.Instance.CurrentState.Player.PlayerName = newName;
            
            // 立即保存一次
            GameManager.Instance.AutoSave();
            
            // 跳转至养成主界面
            LoadingScreen.TargetScene = "res://scenes/SimulationMainScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        }
        else
        {
            GD.PrintErr("[NamingScreen] GameManager or CurrentState is null!");
        }
    }
}
