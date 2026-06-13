using Godot;
using EraDream.StoryEditor.Nodes;

namespace EraDream.StoryEditor.Nodes
{
    public class NarrativeNodeData : BaseNodeData
    {
        public string Content { get; set; } = "";
        public string BackgroundTag { get; set; } = "";
        public float BlurValue { get; set; } = 0.0f;
        public float Darkness { get; set; } = 0.0f;

        private VBoxContainer _detailPanel;
        private HSlider _blurSlider;
        private HSlider _darkSlider;
        
        // 缓存文本输入框与背景标签输入框
        private TextEdit _contentInput;
        private LineEdit _bgTagInput;

        public override GraphNode CreateGraphNode(GraphEdit host)
        {
            GraphNode node = new GraphNode { Title = Tr("KEY_NODE_NARRATIVE"), Name = Id };
            SetupBaseNodeUI(node);
            node.SetSlot(0, true, 0, new Color(1,1,1), true, 0, new Color(1,1,1));
            
            _contentInput = new TextEdit { 
                PlaceholderText = Tr("KEY_PLACEHOLDER_NARRATIVE"), 
                CustomMinimumSize = new Vector2(220, 100), 
                Text = Content 
            };
            node.AddChild(_contentInput);

            // 详细面板 (背景切换、滤镜等)
            _detailPanel = new VBoxContainer { Visible = IsExpanded };
            
            Label bgLabel = new Label { Text = Tr("KEY_LABEL_BG_ID") };
            bgLabel.AddThemeFontSizeOverride("font_size", 12);
            _detailPanel.AddChild(bgLabel);
            _bgTagInput = new LineEdit { Text = BackgroundTag, PlaceholderText = "e.g. bg_classroom_day" };
            _detailPanel.AddChild(_bgTagInput);

            // 虚化控制
            _detailPanel.AddChild(new Label { Text = "背景虚化 (Blur)", ThemeTypeVariation = "HeaderSmall" });
            _blurSlider = new HSlider { MinValue = 0, MaxValue = 5, Step = 0.1, Value = BlurValue };
            _detailPanel.AddChild(_blurSlider);

            // 亮度控制 (暗度)
            _detailPanel.AddChild(new Label { Text = "背景暗度 (Darkness)", ThemeTypeVariation = "HeaderSmall" });
            _darkSlider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = Darkness };
            _detailPanel.AddChild(_darkSlider);

            node.AddChild(_detailPanel);

            UpdateNodeSize(node);
            return node;
        }

        protected override void OnDetailPressed(GraphNode node)
        {
            IsExpanded = !IsExpanded;
            if (_detailPanel != null) _detailPanel.Visible = IsExpanded;
            UpdateNodeSize(node);
        }

        private void UpdateNodeSize(GraphNode node)
        {
            float targetY = IsExpanded ? 320f : 160f;
            node.CustomMinimumSize = new Vector2(240, targetY);
            node.Size = Vector2.Zero; // 强制收缩重算，防大小异常
        }

        public override void SyncFromView(GraphNode view)
        {
            PosX = view.PositionOffset.X;
            PosY = view.PositionOffset.Y;
            IsExpanded = _detailPanel != null && _detailPanel.Visible;

            if (_contentInput != null) Content = _contentInput.Text;
            if (_bgTagInput != null) BackgroundTag = _bgTagInput.Text;
            if (_blurSlider != null) BlurValue = (float)_blurSlider.Value;
            if (_darkSlider != null) Darkness = (float)_darkSlider.Value;
        }
    }
}
