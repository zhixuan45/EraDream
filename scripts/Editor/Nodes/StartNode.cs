using Godot;
using UmaEraArchive.Editor.Nodes;

using System.Text.Json.Serialization;

public class StartNodeData : BaseNodeData
{
	[JsonPropertyName("trigger_condition")]
	public string TriggerCondition { get; set; } = "";

	private LineEdit _conditionInput;

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_START"), Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, false, 0, new Color(1, 1, 1), true, 0, new Color(1, 1, 1));
		Label infoLabel = new Label { 
			Text = Tr("KEY_START_INFO"), 
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(120, 20)
		};
		node.AddChild(infoLabel);

		_conditionInput = new LineEdit {
			PlaceholderText = "触发条件 (例: Affection>=50)",
			Text = TriggerCondition,
			CustomMinimumSize = new Vector2(120, 30)
		};
		node.AddChild(_conditionInput);
		
		node.CustomMinimumSize = new Vector2(160, 120);
		node.Size = Vector2.Zero;
		return node;
	}

	public override void SyncFromView(GraphNode view)
	{
		if (_conditionInput != null)
		{
			TriggerCondition = _conditionInput.Text.Trim();
		}
	}
}
