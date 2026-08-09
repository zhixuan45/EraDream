using Godot;

namespace EraDream.StoryEditor.Nodes
{
    /// <summary>独立音效节点；非阻塞音效可与后续文本并行播放。</summary>
    public class SoundEffectNodeData : BaseNodeData
    {
        public string AudioFile { get; set; } = "";
        public float Volume { get; set; } = 0.8f;
        public bool WaitForCompletion { get; set; } = false;

        private OptionButton _audioSelector;
        private HSlider _volumeSlider;
        private CheckBox _waitCheck;

        public override GraphNode CreateGraphNode(GraphEdit host)
        {
            var node = new GraphNode { Title = "音效", Name = Id };
            SetupBaseNodeUI(node);
            node.SetSlot(0, true, 0, new Color(0.4f, 0.85f, 0.65f), true, 0, new Color(0.4f, 0.85f, 0.65f));
            var container = new VBoxContainer();
            container.AddChild(new Label { Text = "音效文件" });
            _audioSelector = new OptionButton();
            AudioLibrary.PopulateOptionButton(_audioSelector, AudioFile);
            container.AddChild(_audioSelector);
            container.AddChild(new Label { Text = "音量" });
            _volumeSlider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = Volume };
            container.AddChild(_volumeSlider);
            _waitCheck = new CheckBox { Text = "播放完毕后继续剧情", ButtonPressed = WaitForCompletion };
            container.AddChild(_waitCheck);
            node.AddChild(container);
            node.CustomMinimumSize = new Vector2(220, 190);
            node.TreeExiting += UnsubscribeResourceChanges;
            ResourceManagerUI.ResourcesChanged += OnResourcesChanged;
            return node;
        }

        private void OnResourcesChanged(ResourceManagerUI.ResourceType type)
        {
            if (type == ResourceManagerUI.ResourceType.Audio && GodotObject.IsInstanceValid(_audioSelector))
                AudioLibrary.PopulateOptionButton(_audioSelector, AudioFile);
        }

        private void UnsubscribeResourceChanges() => ResourceManagerUI.ResourcesChanged -= OnResourcesChanged;

        public override void SyncFromView(GraphNode view)
        {
            PosX = view.PositionOffset.X;
            PosY = view.PositionOffset.Y;
            AudioFile = _audioSelector != null && _audioSelector.Selected > 0 ? _audioSelector.GetItemText(_audioSelector.Selected) : "";
            if (_volumeSlider != null) Volume = (float)_volumeSlider.Value;
            if (_waitCheck != null) WaitForCompletion = _waitCheck.ButtonPressed;
        }
    }
}
