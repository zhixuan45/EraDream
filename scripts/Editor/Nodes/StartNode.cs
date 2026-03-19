using Godot;
using UmaArchive.Editor.Nodes;

public class StartNodeData : BaseNodeData
{
	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_START"), Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, false, 0, new Color(1, 1, 1), true, 0, new Color(1, 1, 1));
		Label infoLabel = new Label { 
			Text = Tr("KEY_START_INFO"), 
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(120, 40)
		};
		node.AddChild(infoLabel);
		
		node.CustomMinimumSize = new Vector2(150, 100);
		node.Size = Vector2.Zero;
		return node;
	}

	public override void SyncFromView(GraphNode view)
	{
		// 无需同步特定数据，仅作为标记
	}
}
