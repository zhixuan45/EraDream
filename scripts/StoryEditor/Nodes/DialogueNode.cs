using Godot;
using System.Collections.Generic;
using EraDream.StoryEditor.Nodes;

namespace EraDream.StoryEditor.Nodes
{
	public class DialogueNodeData : BaseNodeData
	{
		public string CharacterId { get; set; } = "";
		public string Content { get; set; } = "";
		public string Emotion { get; set; } = "Neutral";
		public string VoiceFile { get; set; } = "";

		private VBoxContainer _detailPanel;
		private OptionButton _voiceSelector;
		
		// 缓存输入和选择器控件，避开 child index 耦合
		private OptionButton _charSelector;
		private TextEdit _contentInput;
		private LineEdit _emotionInput;

		public override GraphNode CreateGraphNode(GraphEdit host)
		{
			GraphNode node = new GraphNode { Title = Tr("KEY_NODE_ACTOR"), Name = Id };
			SetupBaseNodeUI(node);
			node.SetSlot(0, true, 0, new Color(1,1,1), true, 0, new Color(1,1,1));
			
			_charSelector = new OptionButton();
			var allActors = CharacterManager.Characters;
			
			if (allActors.Count == 0)
			{
				_charSelector.AddItem(Tr("KEY_CHAR_NARRATOR"));
			}
			else
			{
				foreach (var c in allActors)
				{
					_charSelector.AddItem(Tr(c.DisplayName));
					if (c.ActorId == CharacterId)
						_charSelector.Selected = _charSelector.GetItemCount() - 1;
				}
			}
			node.AddChild(_charSelector);
			
			_contentInput = new TextEdit { 
				PlaceholderText = Tr("KEY_PLACEHOLDER_DIALOGUE"), 
				CustomMinimumSize = new Vector2(220, 60), 
				Text = Content 
			};
			node.AddChild(_contentInput);

			_detailPanel = new VBoxContainer { Visible = IsExpanded };
			
			Label emotionLabel = new Label { Text = Tr("KEY_LABEL_EMOTION") };
			emotionLabel.AddThemeFontSizeOverride("font_size", 12);
			_detailPanel.AddChild(emotionLabel);
			_emotionInput = new LineEdit { Text = Emotion };
			_detailPanel.AddChild(_emotionInput);

			_detailPanel.AddChild(new HSeparator());
			Label voiceLabel = new Label { Text = Tr("KEY_LABEL_VOICE_SYNC") };
			voiceLabel.AddThemeFontSizeOverride("font_size", 12);
			_detailPanel.AddChild(voiceLabel);
			
			_voiceSelector = new OptionButton { CustomMinimumSize = new Vector2(150, 0) };
			AudioLibrary.PopulateOptionButton(_voiceSelector, VoiceFile);
			_detailPanel.AddChild(_voiceSelector);

			node.AddChild(_detailPanel);
			
			ResetNodeSize(node);
			return node;
		}

		protected override void OnDetailPressed(GraphNode node)
		{
			IsExpanded = !IsExpanded;
			if (_detailPanel != null) _detailPanel.Visible = IsExpanded;
			ResetNodeSize(node);
		}

		private void ResetNodeSize(GraphNode node)
		{
			float targetY = IsExpanded ? 320f : 160f;
			node.CustomMinimumSize = new Vector2(250, targetY);
			node.Size = Vector2.Zero; // 强制收缩重算
		}

		public override void SyncFromView(GraphNode view)
		{
			PosX = view.PositionOffset.X;
			PosY = view.PositionOffset.Y;
			IsExpanded = _detailPanel != null && _detailPanel.Visible;

			if (_charSelector != null)
			{
				int selectedIdx = _charSelector.Selected;
				var actor = CharacterManager.GetActorByIndex(selectedIdx);
				CharacterId = actor?.ActorId ?? "";
			}

			if (_contentInput != null) Content = _contentInput.Text;
			if (_emotionInput != null) Emotion = _emotionInput.Text;
			
			if (_voiceSelector != null)
			{
				if (_voiceSelector.Selected > 0 && !_voiceSelector.GetItemText(_voiceSelector.Selected).StartsWith("⚠️"))
					VoiceFile = _voiceSelector.GetItemText(_voiceSelector.Selected);
				else
					VoiceFile = "";
			}
		}

		public override string GetSearchableText() => $"对话 {Content}";
	}
}
