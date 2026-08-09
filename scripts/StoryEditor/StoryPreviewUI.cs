using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using EraDream.StoryEditor.Nodes;

public partial class StoryPreviewUI : Node
{
    // Visual editing commits when the preview closes, using the editor's debounce save path.
    private EditorScreen _editorOwner;

    public static void Preview(Node parent, List<BaseNodeData> nodes, string startNodeId = null, bool isEditMode = false)
    {
        if (nodes == null || nodes.Count == 0) return;

        var previewer = new StoryPreviewUI();
        previewer._editorOwner = parent as EditorScreen;
        parent.AddChild(previewer);
        previewer.CreatePreviewWindow(nodes, startNodeId, isEditMode);
    }

    private void CreatePreviewWindow(List<BaseNodeData> nodes, string startNodeId, bool isEditMode)
    {
        Window window = new Window {
            Title = isEditMode ? "可视化立绘编辑 (Visual Edit)" : "剧情实时预览 (Preview)",
            // 初始尺寸是设计画布，用户仍可调整窗口来验证各种屏幕比例。
            Size = new Vector2I(1280, 720),
            MinSize = new Vector2I(320, 180),
            Unresizable = false,
            Transient = false,   // 独立原生窗口，不锁定在主窗口之上
            Exclusive = false,   // 不阻塞主编辑器
            InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen
        };

        // 核心：设置播放引擎的预览数据
        StoryPlayerEngine.PreviewNodes = nodes;
        StoryPlayerEngine.StartNodeId = startNodeId;
        StoryPlayerEngine.EnableVisualEditing = isEditMode;

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
				if (isEditMode) _editorOwner?.NotifyVisualEditCommitted();
                window.QueueFree();
                this.QueueFree();
            };
        }

        window.CloseRequested += () => {
			if (isEditMode) _editorOwner?.NotifyVisualEditCommitted();
            window.QueueFree();
            this.QueueFree();
        };

        window.Popup();
    }
}
