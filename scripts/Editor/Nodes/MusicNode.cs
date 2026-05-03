using Godot;
using UmaEraArchive.Editor.Nodes;

public class MusicNodeData : BaseNodeData
{
	public string AudioFile { get; set; } = "";
	public float Volume { get; set; } = 0.8f;

	private OptionButton _musicSelector;
	private AudioStreamPlayer _previewPlayer;
	private Button _playBtn;

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_AUDIO"), Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, true, 0, new Color(0.4f, 0.7f, 1.0f), true, 0, new Color(0.4f, 0.7f, 1.0f));
		
		_musicSelector = new OptionButton();
		AudioLibrary.PopulateOptionButton(_musicSelector, AudioFile);
		_musicSelector.ItemSelected += (idx) => StopPreview(); // 切换音乐时自动停止
		node.AddChild(_musicSelector);
		
		// 播放器控制面板
		HBoxContainer ctrlBox = new HBoxContainer();
		_playBtn = new Button { Text = "▶ 播放", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		Button stopBtn = new Button { Text = "⏹ 停止", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_playBtn.Pressed += () => TogglePlayPreview(_musicSelector);
		stopBtn.Pressed += StopPreview;
		ctrlBox.AddChild(_playBtn);
		ctrlBox.AddChild(stopBtn);
		node.AddChild(ctrlBox);

		// 音量控制
		HBoxContainer volBox = new HBoxContainer();
		Label volLabel = new Label { Text = Tr("KEY_LABEL_VOLUME") };
		volLabel.AddThemeFontSizeOverride("font_size", 12);
		volBox.AddChild(volLabel);
		HSlider volumeSlider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05f, Value = Volume, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		volumeSlider.ValueChanged += (v) => {
			Volume = (float)v;
			if (_previewPlayer != null) _previewPlayer.VolumeDb = Mathf.LinearToDb(Volume);
		};
		volBox.AddChild(volumeSlider);
		node.AddChild(volBox);
		
		// 保证在节点被删除/关闭时清理播放器
		node.TreeExiting += StopPreview;

		node.Size = new Vector2(240, 200);
		return node;
	}

	private void TogglePlayPreview(OptionButton selector)
	{
		if (selector.Selected <= 0) return;
		
		if (_previewPlayer != null && _previewPlayer.Playing)
		{
			// 如果正在播放，则暂停
			_previewPlayer.StreamPaused = true;
			_playBtn.Text = "▶ 继续";
			return;
		}

		if (_previewPlayer != null && _previewPlayer.StreamPaused)
		{
			// 如果已暂停，则继续
			_previewPlayer.StreamPaused = false;
			_playBtn.Text = "⏸ 暂停";
			return;
		}

		// 全新播放（或停止后重新播放）
		string fileName = selector.GetItemText(selector.Selected);
		AudioStream stream = UmaEraArchive.Core.ResourceProxy.LoadAudioFromProject(fileName);
		if (stream != null)
		{
			if (_previewPlayer == null)
			{
				_previewPlayer = new AudioStreamPlayer();
				((SceneTree)Engine.GetMainLoop()).Root.AddChild(_previewPlayer);
				_previewPlayer.Finished += () => {
					if (_playBtn != null) _playBtn.Text = "▶ 播放";
				};
			}
			
			_previewPlayer.Stream = stream;
			_previewPlayer.VolumeDb = Mathf.LinearToDb(Volume);
			_previewPlayer.Play();
			_previewPlayer.StreamPaused = false;
			_playBtn.Text = "⏸ 暂停";
		}
	}

	private void StopPreview()
	{
		if (_previewPlayer != null)
		{
			_previewPlayer.Stop();
			_previewPlayer.QueueFree();
			_previewPlayer = null;
		}
		if (_playBtn != null)
		{
			_playBtn.Text = "▶ 播放";
		}
	}

	public override void SyncFromView(GraphNode view)
	{
		PosX = view.PositionOffset.X;
		PosY = view.PositionOffset.Y;

		if (_musicSelector.Selected > 0)
			AudioFile = _musicSelector.GetItemText(_musicSelector.Selected);
		else
			AudioFile = "";
			
		// 恢复为正确的索引 3：因为 ctrlBox 只是替换了原来的 playBtn 的位置，总排序没变
		Volume = (float)view.GetChild<HBoxContainer>(3).GetChild<HSlider>(1).Value;
	}
}
