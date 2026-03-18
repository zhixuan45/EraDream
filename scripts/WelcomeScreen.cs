using Godot;
using System;

public partial class WelcomeScreen : Control
{
    // C#需要注释，最多两行
    // 监听全局输入，实现点击跳转
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        }
        else if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
        {
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        }
    }
}
