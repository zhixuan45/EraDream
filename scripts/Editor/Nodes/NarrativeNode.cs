using Godot;
using UmaArchive.Editor.Nodes;

public class NarrativeNodeData : BaseNodeData
{
    public string Content { get; set; } = "";
    public string BackgroundTag { get; set; } = "";

    private VBoxContainer _detailPanel;

    public override GraphNode CreateGraphNode(GraphEdit host)
    {
        GraphNode node = new GraphNode { Title = "KEY_NODE_NARRATIVE", Name = Id };
        SetupBaseNodeUI(node);
        node.SetSlot(0, true, 0, new Color(1,1,1), true, 0, new Color(1,1,1));
        
        TextEdit contentInput = new TextEdit { 
            PlaceholderText = "KEY_PLACEHOLDER_NARRATIVE", 
            CustomMinimumSize = new Vector2(220, 100), 
            Text = Content 
        };
        node.AddChild(contentInput);

        // 详细面板 (背景切换等)
        _detailPanel = new VBoxContainer { Visible = IsExpanded };
        Label bgLabel = new Label { Text = "KEY_LABEL_BG_ID" };
        bgLabel.AddThemeFontSizeOverride("font_size", 12);
        _detailPanel.AddChild(bgLabel);
        _detailPanel.AddChild(new LineEdit { Text = BackgroundTag, PlaceholderText = "e.g. bg_classroom_day" });
        node.AddChild(_detailPanel);

        UpdateNodeSize(node);
        return node;
    }

    protected override void OnDetailPressed(GraphNode node)
    {
        IsExpanded = !IsExpanded;
        _detailPanel.Visible = IsExpanded;
        UpdateNodeSize(node);
    }

    private void UpdateNodeSize(GraphNode node)
    {
        float targetY = IsExpanded ? 240f : 160f;
        node.CustomMinimumSize = new Vector2(240, targetY);
        node.Size = new Vector2(240, targetY);
    }

    public override void SyncFromView(GraphNode view)
    {
        Content = view.GetChild<TextEdit>(1).Text;
        BackgroundTag = _detailPanel.GetChild<LineEdit>(1).Text;
    }
}
