using Godot;
using UmaArchive.Editor.Nodes;

public class BackgroundNodeData : BaseNodeData
{
	public string BackgroundFile { get; set; } = "";
	public string TransitionType { get; set; } = "Fade";

	private OptionButton _bgSelector;
	private OptionButton _transSelector;

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_BACKGROUND"), Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, true, 0, new Color(1, 1, 1), true, 0, new Color(1, 1, 1));
		VBoxContainer container = new VBoxContainer();
		container.AddChild(new Label { Text = Tr("KEY_LABEL_BG_SELECT") });
		_bgSelector = new OptionButton { CustomMinimumSize = new Vector2(180, 0) };
		BackgroundLibrary.PopulateOptionButton(_bgSelector, BackgroundFile);
		container.AddChild(_bgSelector);
		container.AddChild(new Control { CustomMinimumSize = new Vector2(0, 5) });
		container.AddChild(new Label { Text = Tr("KEY_LABEL_TRANSITION") });
		_transSelector = new OptionButton();
		_transSelector.AddItem(Tr("KEY_TRANS_FADE"), 0);
		_transSelector.AddItem(Tr("KEY_TRANS_CUT"), 1);
		_transSelector.AddItem(Tr("KEY_TRANS_SLIDE"), 2);
		
		// 恢复选项
		_transSelector.Selected = TransitionType switch {
			"Cut" => 1,
			"Slide" => 2,
			_ => 0
		};
		container.AddChild(_transSelector);
		
		node.AddChild(container);
		
		node.CustomMinimumSize = new Vector2(200, 160);
		node.Size = Vector2.Zero;
		return node;
	}

	public override void SyncFromView(GraphNode view)
	{
		// 同步背景文件
		if (_bgSelector.Selected > 0)
			BackgroundFile = _bgSelector.GetItemText(_bgSelector.Selected);
		else
			BackgroundFile = "";
			
		// 同步过渡类型
		TransitionType = _transSelector.Selected switch {
			1 => "Cut",
			2 => "Slide",
			_ => "Fade"
		};
	}
}
