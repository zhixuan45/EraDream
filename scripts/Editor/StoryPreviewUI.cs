using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UmaArchive.Editor.Nodes;

public partial class StoryPreviewUI : Node
{
    public static void Preview(Node parent, List<BaseNodeData> nodes)
    {
        if (nodes == null || nodes.Count == 0) return;

        var previewer = new StoryPreviewUI();
        parent.AddChild(previewer);
        previewer.CreatePreviewWindow(nodes);
    }

    private void CreatePreviewWindow(List<BaseNodeData> nodes)
    {
        Window window = new Window {
            Title = "剧情实时预览 (Preview)",
            Size = new Vector2I(1280, 720),
            Transient = true,
            Exclusive = true,
            InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen
        };

        // 核心：设置播放引擎的预览数据
        StoryPlayerEngine.PreviewNodes = nodes;

        // 实例化播放场景
        var playerScene = GD.Load<PackedScene>("res://scenes/StoryPlayerScreen.tscn");
        var playerInstance = playerScene.Instantiate();
        
        // 确保实例拉伸填充窗口
        if (playerInstance is Control control) {
            control.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        }

        window.AddChild(playerInstance);
        AddChild(window);

        // 监听播放结束信号，自动关闭窗口
        if (playerInstance is StoryPlayerEngine engine)
        {
            engine.StoryFinished += () => {
                window.QueueFree();
                this.QueueFree();
            };
        }

        window.CloseRequested += () => {
            window.QueueFree();
            this.QueueFree();
        };

        window.Popup();
    }
}
