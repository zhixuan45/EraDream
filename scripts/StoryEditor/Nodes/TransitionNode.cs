using Godot;

namespace EraDream.StoryEditor.Nodes
{
    /// <summary>独立场景过场，可在不切换背景时播放遮罩演出。</summary>
    public class TransitionNodeData : BaseNodeData
    {
        public string TransitionType { get; set; } = "FadeBlack";
        public float Duration { get; set; } = 0.5f;

        private OptionButton _typeSelector;
        private HSlider _durationSlider;
        private LineEdit _durationInput;
        private bool _syncingDuration;

        public override GraphNode CreateGraphNode(GraphEdit host)
        {
            var node = new GraphNode { Title = "场景过场", Name = Id };
            SetupBaseNodeUI(node);
            node.SetSlot(0, true, 0, Colors.White, true, 0, Colors.White);
            var container = new VBoxContainer();
            container.AddChild(new Label { Text = "过场方式" });
            _typeSelector = new OptionButton();
            _typeSelector.AddItem("黑幕淡入淡出");
            _typeSelector.AddItem("白闪");
            _typeSelector.AddItem("左滑");
            _typeSelector.AddItem("右滑");
            _typeSelector.Selected = TransitionType == "FlashWhite" ? 1 : TransitionType == "SlideLeft" ? 2 : TransitionType == "SlideRight" ? 3 : 0;
            container.AddChild(_typeSelector);
            container.AddChild(new Label { Text = "过场时长（秒）" });
            _durationSlider = new HSlider { MinValue = 0.05, MaxValue = 5, Step = 0.05, Value = Duration };
            _durationInput = new LineEdit
            {
                Text = Duration.ToString("0.##"),
                CustomMinimumSize = new Vector2(90, 0),
                PlaceholderText = "请输入数字"
            };
            // 滑块拖动时立即回写输入框，输入框修改时同步滑块位置。
            _durationSlider.ValueChanged += value => SetDurationFromSlider((float)value);
            _durationInput.TextChanged += value => SetDurationFromInput(value);
            var durationRow = new HBoxContainer();
            durationRow.AddChild(_durationSlider);
            durationRow.AddChild(_durationInput);
            container.AddChild(durationRow);
            node.AddChild(container);
            node.CustomMinimumSize = new Vector2(220, 180);
            return node;
        }

        public override void SyncFromView(GraphNode view)
        {
            PosX = view.PositionOffset.X;
            PosY = view.PositionOffset.Y;
            if (_typeSelector != null)
                TransitionType = _typeSelector.Selected == 1 ? "FlashWhite" : _typeSelector.Selected == 2 ? "SlideLeft" : _typeSelector.Selected == 3 ? "SlideRight" : "FadeBlack";
            // Duration 已由输入框或滑块事件实时维护，不能用滑块值覆盖超出滑块范围的合法输入。
        }

        private void SetDurationFromSlider(float value)
        {
            if (_syncingDuration) return;
            Duration = value;
            _syncingDuration = true;
            _durationInput.Text = Duration.ToString("0.##");
            _syncingDuration = false;
        }

        private void SetDurationFromInput(string text)
        {
            if (_syncingDuration || !float.TryParse(text, out float value) || !float.IsFinite(value)) return;
            Duration = value;
            _syncingDuration = true;
            _durationSlider.Value = Mathf.Clamp(value, (float)_durationSlider.MinValue, (float)_durationSlider.MaxValue);
            _syncingDuration = false;
        }
    }
}
