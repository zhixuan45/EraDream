using Godot;
using System.Collections.Generic;
using UmaArchive.Editor.Nodes;

public class DialogueNodeData : BaseNodeData
{
	public int CharacterId { get; set; } = 0;
	public string Content { get; set; } = "";
	public string Emotion { get; set; } = "Neutral";
	public string VoiceFile { get; set; } = "";

	private VBoxContainer _detailPanel;
	private OptionButton _voiceSelector;

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = "KEY_NODE_ACTOR", Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, true, 0, new Color(1,1,1), true, 0, new Color(1,1,1));
		
		OptionButton charSelector = new OptionButton();
		charSelector.AddItem("KEY_CHAR_PROTAGONIST");
		charSelector.AddItem("KEY_CHAR_SIDEKICK");
		charSelector.AddItem("KEY_CHAR_NARRATOR");
		charSelector.Selected = CharacterId;
		node.AddChild(charSelector);
		
		TextEdit contentInput = new TextEdit { 
			PlaceholderText = "KEY_PLACEHOLDER_DIALOGUE", 
			CustomMinimumSize = new Vector2(220, 60), 
			Text = Content 
		};
		node.AddChild(contentInput);

		_detailPanel = new VBoxContainer { Visible = IsExpanded };
		
		Label emotionLabel = new Label { Text = "KEY_LABEL_EMOTION" };
		emotionLabel.AddThemeFontSizeOverride("font_size", 12);
		_detailPanel.AddChild(emotionLabel);
		_detailPanel.AddChild(new LineEdit { Text = Emotion });

		_detailPanel.AddChild(new HSeparator());
		Label voiceLabel = new Label { Text = "KEY_LABEL_VOICE_SYNC" };
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
		_detailPanel.Visible = IsExpanded;
		ResetNodeSize(node);
	}

	private void ResetNodeSize(GraphNode node)
	{
		// 关键逻辑：除了设置最小尺寸，还强制将 Size 归零，触发自动布局重算
		float targetY = IsExpanded ? 320f : 160f;
		node.CustomMinimumSize = new Vector2(250, targetY);
		node.Size = Vector2.Zero; // 强制收缩
	}

	public override void SyncFromView(GraphNode view)
	{
		CharacterId = view.GetChild<OptionButton>(1).Selected;
		Content = view.GetChild<TextEdit>(2).Text;
		Emotion = _detailPanel.GetChild<LineEdit>(1).Text;
		
		if (_voiceSelector.Selected > 0)
			VoiceFile = _voiceSelector.GetItemText(_voiceSelector.Selected);
		else
			VoiceFile = "";
	}
}
