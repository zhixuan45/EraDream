using Godot;
using System;

public partial class WelcomeScreen : Control
{
    // C#需要注释，最多两行
    // 在进入欢迎界面时请求必要的权限
    public override void _Ready()
    {
        if (OS.GetName() == "Android")
        {
            OS.RequestPermissions();
        }
    }

    // 监听全局输入，实现点击跳转
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            LoadingScreen.TargetScene = "res://scenes/UI/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
        }
        else if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
        {
            LoadingScreen.TargetScene = "res://scenes/UI/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
        }
    }
}
