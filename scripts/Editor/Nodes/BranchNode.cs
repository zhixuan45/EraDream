using Godot;
using EraDream.Editor.Nodes;

public class BranchNodeData : BaseNodeData
{
	public string VariableId { get; set; } = "player_score";
	public string ComparisonValue { get; set; } = "10";
	public string SuccessNodeId { get; set; } = "";
	public string FailNodeId { get; set; } = "";

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_BRANCH"), Name = Id };
		SetupBaseNodeUI(node);
		
		// Slot 0: Header (关闭端口，恢复整洁)
		node.SetSlot(0, false, 0, new Color(1, 1, 1), false, 0, new Color(1, 1, 1));

		// Slot 1: 变量 ID (开启左侧流入白色小点)
		LineEdit varInput = new LineEdit { Text = VariableId, PlaceholderText = Tr("KEY_PLACEHOLDER_VAR_ID") };
		node.AddChild(varInput);
		node.SetSlot(1, true, 0, new Color(1, 1, 1), false, 0, new Color(1, 1, 1));

		// Slot 2: 对比值
		LineEdit valInput = new LineEdit { Text = ComparisonValue, PlaceholderText = Tr("KEY_PLACEHOLDER_COMP_VALUE") };
		node.AddChild(valInput);

		// Slot 3: 成功输出 (右侧绿色 Port 0)
		Label successLabel = new Label { Text = Tr("KEY_LABEL_TRUE"), HorizontalAlignment = HorizontalAlignment.Right };
		node.AddChild(successLabel);
		node.SetSlot(3, false, 0, new Color(1, 1, 1), true, 0, new Color(0, 1, 0));

		// Slot 4: 失败输出 (右侧红色 Port 1)
		Label failLabel = new Label { Text = Tr("KEY_LABEL_FALSE"), HorizontalAlignment = HorizontalAlignment.Right };
		node.AddChild(failLabel);
		node.SetSlot(4, false, 0, new Color(1, 1, 1), true, 0, new Color(1, 0, 0));

		node.Size = new Vector2(260, 200);
		return node;
	}

	public override void SyncFromView(GraphNode view)
	{
		PosX = view.PositionOffset.X;
		PosY = view.PositionOffset.Y;

		VariableId = view.GetChild<LineEdit>(1).Text;
		ComparisonValue = view.GetChild<LineEdit>(2).Text;

		SuccessNodeId = "";
		FailNodeId = "";
		
		GraphEdit graph = (GraphEdit)view.GetParent();
		foreach (var conn in graph.GetConnectionList())
		{
			if (conn["from_node"].AsString() == view.Name)
			{
				int port = conn["from_port"].AsInt32();
				if (port == 0) SuccessNodeId = conn["to_node"].AsString();
				if (port == 1) FailNodeId = conn["to_node"].AsString();
			}
		}
	}
}
