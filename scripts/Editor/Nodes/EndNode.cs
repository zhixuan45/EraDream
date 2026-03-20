using Godot;
using UmaEraArchive.Editor.Nodes;

public class EndNodeData : BaseNodeData
{
	public string EndType { get; set; } = "Title";
	public string CustomScenePath { get; set; } = "";

	private OptionButton _typeSelector;
	private LineEdit _pathInput;

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_END"), Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, true, 0, new Color(1, 0, 0), false, 0, new Color(1, 0, 0));
		VBoxContainer container = new VBoxContainer();
		_typeSelector = new OptionButton();
		_typeSelector.AddItem(Tr("KEY_END_TITLE"), 0);
		_typeSelector.AddItem(Tr("KEY_END_NEXT"), 1);
		_typeSelector.AddItem(Tr("KEY_END_CUSTOM"), 2);
		_typeSelector.Selected = EndType switch {
			"Next" => 1,
			"Custom" => 2,
			_ => 0
		};
		container.AddChild(_typeSelector);
		_pathInput = new LineEdit { 
			PlaceholderText = Tr("KEY_PLACEHOLDER_SCENE"), 
			Text = CustomScenePath,
			Visible = (EndType == "Custom")
		};
		container.AddChild(_pathInput);

		_typeSelector.ItemSelected += (idx) => {
			_pathInput.Visible = (idx == 2);
		};

		node.AddChild(container);
		node.CustomMinimumSize = new Vector2(200, 120);
		node.Size = Vector2.Zero;
		return node;
	}

	public override void SyncFromView(GraphNode view)
	{
		var container = view.GetChild<VBoxContainer>(1);
		var selector = container.GetChild<OptionButton>(0);
		var input = container.GetChild<LineEdit>(1);

		EndType = selector.Selected switch {
			1 => "Next",
			2 => "Custom",
			_ => "Title"
		};
		CustomScenePath = input.Text;
	}
}
