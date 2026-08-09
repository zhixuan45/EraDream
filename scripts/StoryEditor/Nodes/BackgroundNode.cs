using Godot;

namespace EraDream.StoryEditor.Nodes
{
    public class BackgroundNodeData : BaseNodeData
    {
        public string BackgroundFile { get; set; } = "";
        public string TransitionType { get; set; } = "Fade";
        // 旧 JSON 缺失这些字段时会自动使用默认值。
        public float OffsetX { get; set; } = 0f;
        public float OffsetY { get; set; } = 0f;
        public float Scale { get; set; } = 1f;
        public float TransitionDuration { get; set; } = 0.35f;

        private OptionButton _backgroundSelector;
        private OptionButton _transitionSelector;
        private HSlider _durationSlider;
        private Label _durationLabel;

        public override GraphNode CreateGraphNode(GraphEdit host)
        {
            var node = new GraphNode { Title = Tr("KEY_NODE_BACKGROUND"), Name = Id };
            SetupBaseNodeUI(node);
            node.SetSlot(0, true, 0, Colors.White, true, 0, Colors.White);

            var container = new VBoxContainer();
            container.AddChild(new Label { Text = Tr("KEY_LABEL_BG_SELECT") });
            _backgroundSelector = new OptionButton { CustomMinimumSize = new Vector2(180, 0) };
            BackgroundLibrary.PopulateOptionButton(_backgroundSelector, BackgroundFile);
            container.AddChild(_backgroundSelector);

            container.AddChild(new Label { Text = Tr("KEY_LABEL_TRANSITION") });
            _transitionSelector = new OptionButton();
            _transitionSelector.AddItem(Tr("KEY_TRANS_FADE"));
            _transitionSelector.AddItem(Tr("KEY_TRANS_CUT"));
            _transitionSelector.AddItem(Tr("KEY_TRANS_SLIDE"));
            _transitionSelector.Selected = TransitionType == "Cut" ? 1 : TransitionType == "Slide" ? 2 : 0;
            container.AddChild(_transitionSelector);

            _durationLabel = new Label();
            container.AddChild(_durationLabel);
            _durationSlider = new HSlider { MinValue = 0.05, MaxValue = 3.0, Step = 0.05, Value = TransitionDuration };
            _durationSlider.ValueChanged += value => UpdateDurationLabel((float)value);
            UpdateDurationLabel(TransitionDuration);
            container.AddChild(_durationSlider);

            var visualEditButton = new Button { Text = "可视化编辑背景" };
            visualEditButton.Pressed += () => OnVisualEditRequested?.Invoke(Id);
            container.AddChild(visualEditButton);
            container.AddChild(new Label { Text = "操作提示：拖动定位，滚轮缩放，右键恢复" });

            node.AddChild(container);
            node.CustomMinimumSize = new Vector2(230, 255);
            node.Size = Vector2.Zero;
            node.TreeExiting += UnsubscribeResourceChanges;
            ResourceManagerUI.ResourcesChanged += OnResourcesChanged;
            return node;
        }

        private void UpdateDurationLabel(float duration)
        {
            if (_durationLabel != null)
                _durationLabel.Text = $"过场时长：{duration:0.00} 秒";
        }

        private void OnResourcesChanged(ResourceManagerUI.ResourceType type)
        {
            if (type == ResourceManagerUI.ResourceType.Background && GodotObject.IsInstanceValid(_backgroundSelector))
                BackgroundLibrary.PopulateOptionButton(_backgroundSelector, BackgroundFile);
        }

        private void UnsubscribeResourceChanges()
        {
            ResourceManagerUI.ResourcesChanged -= OnResourcesChanged;
        }

        public override void SyncFromView(GraphNode view)
        {
            PosX = view.PositionOffset.X;
            PosY = view.PositionOffset.Y;
            if (_backgroundSelector != null && _backgroundSelector.Selected > 0)
                BackgroundFile = _backgroundSelector.GetItemText(_backgroundSelector.Selected);
            else
                BackgroundFile = "";

            if (_transitionSelector != null)
                TransitionType = _transitionSelector.Selected == 1 ? "Cut" : _transitionSelector.Selected == 2 ? "Slide" : "Fade";
            if (_durationSlider != null)
                TransitionDuration = (float)_durationSlider.Value;
        }
    }
}
