using Godot;
using EraDream.Editor.Nodes;

using System.Text.Json.Serialization;

public enum TriggerTiming
{
	TurnStart,
	TurnEnd
}

public class StartNodeData : BaseNodeData
{
	[JsonPropertyName("trigger_condition")]
	public string TriggerCondition { get; set; } = "";

	[JsonPropertyName("trigger_timing")]
	public TriggerTiming Timing { get; set; } = TriggerTiming.TurnStart;

	private LineEdit _conditionInput;
	private OptionButton _timingPicker;

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

		_timingPicker = new OptionButton {
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_timingPicker.AddItem("回合开始触发 (TurnStart)", 0);
		_timingPicker.AddItem("回合结束触发 (TurnEnd)", 1);
		_timingPicker.Select((int)Timing);
		node.AddChild(_timingPicker);

		_conditionInput = new LineEdit {
			PlaceholderText = "触发条件 (例: Affection>=50)",
			Text = TriggerCondition,
			CustomMinimumSize = new Vector2(120, 30)
		};
		node.AddChild(_conditionInput);
		
		node.CustomMinimumSize = new Vector2(160, 150);
		node.Size = Vector2.Zero;
		return node;
	}

	public override void SyncFromView(GraphNode view)
	{
		PosX = view.PositionOffset.X;
		PosY = view.PositionOffset.Y;

		if (_conditionInput != null)
		{
			TriggerCondition = _conditionInput.Text.Trim();
		}
		if (_timingPicker != null)
		{
			Timing = (TriggerTiming)_timingPicker.Selected;
		}
	}
}
