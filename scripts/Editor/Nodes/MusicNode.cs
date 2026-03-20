using Godot;
using UmaEraArchive.Editor.Nodes;

public class MusicNodeData : BaseNodeData
{
	public string AudioFile { get; set; } = "";
	public float Volume { get; set; } = 0.8f;

	private OptionButton _musicSelector;

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_AUDIO"), Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, true, 0, new Color(0.4f, 0.7f, 1.0f), true, 0, new Color(0.4f, 0.7f, 1.0f));
		_musicSelector = new OptionButton();
		AudioLibrary.PopulateOptionButton(_musicSelector, AudioFile);
		node.AddChild(_musicSelector);
		Button playBtn = new Button { Text = Tr("KEY_BTN_PLAY_PREVIEW"), Flat = false };
		playBtn.Pressed += () => PlayPreview(_musicSelector);
		node.AddChild(playBtn);
		HBoxContainer volBox = new HBoxContainer();
		Label volLabel = new Label { Text = Tr("KEY_LABEL_VOLUME") };
		volLabel.AddThemeFontSizeOverride("font_size", 12);
		volBox.AddChild(volLabel);
		HSlider volumeSlider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05f, Value = Volume, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		volBox.AddChild(volumeSlider);
		node.AddChild(volBox);
		
		node.Size = new Vector2(240, 180);
		return node;
	}

	private void PlayPreview(OptionButton selector)
	{
		if (selector.Selected <= 0) return;
		
		string fileName = selector.GetItemText(selector.Selected);
		string fullPath = $"res://audio/{fileName}";
		
		// 使用引擎的预览器播放
		AudioStream stream = GD.Load<AudioStream>(fullPath);
		if (stream != null)
		{
			AudioStreamPlayer player = new AudioStreamPlayer();
			((SceneTree)Engine.GetMainLoop()).Root.AddChild(player);
			player.Stream = stream;
			player.VolumeDb = Mathf.LinearToDb(Volume);
			player.Play();
			player.Finished += () => player.QueueFree();
		}
	}

	public override void SyncFromView(GraphNode view)
	{
		if (_musicSelector.Selected > 0)
			AudioFile = _musicSelector.GetItemText(_musicSelector.Selected);
		else
			AudioFile = "";
			
		Volume = (float)view.GetChild<HBoxContainer>(3).GetChild<HSlider>(1).Value;
	}
}
