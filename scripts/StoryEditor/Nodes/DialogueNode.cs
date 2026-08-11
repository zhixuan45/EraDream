using Godot;

namespace EraDream.StoryEditor.Nodes
{
    public class DialogueNodeData : BaseNodeData
    {
        public string CharacterId { get; set; } = "";
        public string Content { get; set; } = "";
        public string Emotion { get; set; } = "Neutral";
        public string VoiceFile { get; set; } = "";
        // 0 表示继承项目演出设置，保证旧 JSON 的行为兼容。
        public float TypewriterSpeed { get; set; } = 0f;
        public float AutoAdvanceDelay { get; set; } = 0f;
        public string FontFile { get; set; } = "";
        public string SoundEffectFile { get; set; } = "";
        public float SoundEffectVolume { get; set; } = 0.8f;

        private VBoxContainer _detailPanel;
        private OptionButton _characterSelector;
        private TextEdit _contentInput;
        private LineEdit _emotionInput;
        private OptionButton _voiceSelector;
        private OptionButton _fontSelector;
        private OptionButton _soundEffectSelector;
        private HSlider _typingSlider;
        private HSlider _autoAdvanceSlider;
        private HSlider _soundVolumeSlider;

        public override GraphNode CreateGraphNode(GraphEdit host)
        {
            var node = new GraphNode { Title = Tr("KEY_NODE_ACTOR"), Name = Id };
            SetupBaseNodeUI(node);
            node.SetSlot(0, true, 0, Colors.White, true, 0, Colors.White);

            _characterSelector = new OptionButton();
            foreach (var actor in CharacterManager.Characters)
            {
                _characterSelector.AddItem(Tr(actor.DisplayName));
                if (actor.ActorId == CharacterId)
                    _characterSelector.Selected = _characterSelector.GetItemCount() - 1;
            }
            if (_characterSelector.GetItemCount() == 0)
                _characterSelector.AddItem(Tr("KEY_CHAR_NARRATOR"));
            node.AddChild(_characterSelector);

            _contentInput = new TextEdit
            {
                PlaceholderText = Tr("KEY_PLACEHOLDER_DIALOGUE"),
                CustomMinimumSize = new Vector2(220, 60),
                Text = Content,
                // 对话文本按节点宽度自动折行，中文长句也不会溢出节点边界。
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ScrollFitContentHeight = true
            };
            node.AddChild(_contentInput);

            _detailPanel = new VBoxContainer { Visible = IsExpanded };
            _detailPanel.AddChild(new Label { Text = Tr("KEY_LABEL_EMOTION") });
            _emotionInput = new LineEdit { Text = Emotion };
            _detailPanel.AddChild(_emotionInput);
            AddPresentationControls(_detailPanel);
            node.AddChild(_detailPanel);

            UpdateNodeSize(node);
            node.TreeExiting += UnsubscribeResourceChanges;
            ResourceManagerUI.ResourcesChanged += OnResourcesChanged;
            return node;
        }

        private void AddPresentationControls(VBoxContainer panel)
        {
            panel.AddChild(new Label { Text = "语音" });
            _voiceSelector = new OptionButton();
            AudioLibrary.PopulateOptionButton(_voiceSelector, VoiceFile);
            panel.AddChild(_voiceSelector);
            panel.AddChild(new Label { Text = "字体覆盖" });
            _fontSelector = new OptionButton();
            FontLibrary.PopulateOptionButton(_fontSelector, FontFile);
            panel.AddChild(_fontSelector);
            panel.AddChild(new Label { Text = "打字速度（0=项目默认，字/秒）" });
            _typingSlider = new HSlider { MinValue = 0, MaxValue = 120, Step = 1, Value = TypewriterSpeed };
            panel.AddChild(_typingSlider);
            panel.AddChild(new Label { Text = "自动推进（0=等待点击，秒）" });
            _autoAdvanceSlider = new HSlider { MinValue = 0, MaxValue = 30, Step = 0.1, Value = AutoAdvanceDelay };
            panel.AddChild(_autoAdvanceSlider);
            panel.AddChild(new Label { Text = "同步音效" });
            _soundEffectSelector = new OptionButton();
            AudioLibrary.PopulateOptionButton(_soundEffectSelector, SoundEffectFile);
            panel.AddChild(_soundEffectSelector);
            panel.AddChild(new Label { Text = "音效音量" });
            _soundVolumeSlider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = SoundEffectVolume };
            panel.AddChild(_soundVolumeSlider);
        }

        protected override void OnDetailPressed(GraphNode node)
        {
            IsExpanded = !IsExpanded;
            if (_detailPanel != null) _detailPanel.Visible = IsExpanded;
            UpdateNodeSize(node);
        }

        private void UpdateNodeSize(GraphNode node)
        {
            node.CustomMinimumSize = new Vector2(260, IsExpanded ? 560 : 160);
            node.Size = Vector2.Zero;
        }

        private void OnResourcesChanged(ResourceManagerUI.ResourceType type)
        {
            if (type == ResourceManagerUI.ResourceType.Audio)
            {
                if (GodotObject.IsInstanceValid(_voiceSelector)) AudioLibrary.PopulateOptionButton(_voiceSelector, VoiceFile);
                if (GodotObject.IsInstanceValid(_soundEffectSelector)) AudioLibrary.PopulateOptionButton(_soundEffectSelector, SoundEffectFile);
            }
            else if (type == ResourceManagerUI.ResourceType.Font && GodotObject.IsInstanceValid(_fontSelector))
                FontLibrary.PopulateOptionButton(_fontSelector, FontFile);
        }

        private void UnsubscribeResourceChanges() => ResourceManagerUI.ResourcesChanged -= OnResourcesChanged;

        public override void SyncFromView(GraphNode view)
        {
            PosX = view.PositionOffset.X;
            PosY = view.PositionOffset.Y;
            IsExpanded = _detailPanel != null && _detailPanel.Visible;
            if (_characterSelector != null) CharacterId = CharacterManager.GetActorByIndex(_characterSelector.Selected)?.ActorId ?? "";
            if (_contentInput != null) Content = _contentInput.Text;
            if (_emotionInput != null) Emotion = _emotionInput.Text;
            VoiceFile = GetSelectedFile(_voiceSelector);
            FontFile = GetSelectedFile(_fontSelector);
            SoundEffectFile = GetSelectedFile(_soundEffectSelector);
            if (_typingSlider != null) TypewriterSpeed = (float)_typingSlider.Value;
            if (_autoAdvanceSlider != null) AutoAdvanceDelay = (float)_autoAdvanceSlider.Value;
            if (_soundVolumeSlider != null) SoundEffectVolume = (float)_soundVolumeSlider.Value;
        }

        private static string GetSelectedFile(OptionButton selector) => selector != null && selector.Selected > 0 ? selector.GetItemText(selector.Selected) : "";
        public override string GetSearchableText() => $"对话 {Content}";
    }
}
