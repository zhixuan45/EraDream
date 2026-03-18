using Godot;
using System.Collections.Generic;
using UmaArchive.Editor.Nodes;

public class ChoiceNodeData : BaseNodeData
{
	public List<ChoiceItem> Options { get; set; } = new List<ChoiceItem>();

	public class ChoiceItem
	{
		public string Text { get; set; } = "新选项";
		public string TargetNodeId { get; set; } = "";
	}

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = "选项分支", Name = Id };
		SetupBaseNodeUI(node);
		
		// Slot 0 (Header): 关闭所有端口，恢复整洁
		node.SetSlot(0, false, 0, new Color(1, 1, 1), false, 0, new Color(1, 1, 1));

		if (Options.Count == 0) Options.Add(new ChoiceItem());

		for (int i = 0; i < Options.Count; i++)
		{
			AddOptionSlot(node, i, Options[i]);
		}

		Button addOptionBtn = new Button { Text = "+ 添加新选项", Flat = true };
		addOptionBtn.Pressed += () => {
			var newItem = new ChoiceItem();
			Options.Add(newItem);
			AddOptionSlot(node, Options.Count - 1, newItem);
			ResetSize(node);
		};
		node.AddChild(addOptionBtn);

		ResetSize(node);
		return node;
	}

	private void AddOptionSlot(GraphNode node, int index, ChoiceItem item)
	{
		HBoxContainer box = new HBoxContainer();
		LineEdit input = new LineEdit { 
			Text = item.Text, 
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			PlaceholderText = "请输入选项文字..."
		};
		box.AddChild(input);

		Button delBtn = new Button { Text = "×", Flat = true };
		box.AddChild(delBtn);

		node.AddChild(box);
		node.MoveChild(box, node.GetChildCount() - 2);

		// 获取当前子节点索引作为槽位索引
		int slotIndex = node.GetChildCount() - 2;
		
		// 关键更改：仅为第一个选项（index == 0）开启左侧流入白色小点
		bool enableInput = (index == 0);
		node.SetSlot(slotIndex, enableInput, 0, new Color(1, 1, 1), true, 0, new Color(1, 0.6f, 0));

		delBtn.Pressed += () => {
			if (node.GetChildCount() > 3) {
				node.RemoveChild(box);
				box.QueueFree();
				ResetSize(node);
				// 提醒：如果删除了第一个选项，需要重新指定输入点（这里简单处理，建议通过刷新实现）
			}
		};
	}

	private void ResetSize(GraphNode node)
	{
		node.CustomMinimumSize = new Vector2(280, 0);
		node.Size = Vector2.Zero;
	}

	public override void SyncFromView(GraphNode view)
	{
		Options.Clear();
		int currentPort = 0; 
		
		for (int i = 1; i < view.GetChildCount(); i++)
		{
			if (view.GetChild(i) is HBoxContainer box)
			{
				var item = new ChoiceItem { Text = box.GetChild<LineEdit>(0).Text };
				
				GraphEdit graph = (GraphEdit)view.GetParent();
				foreach (var conn in graph.GetConnectionList())
				{
					if (conn["from_node"].AsString() == view.Name && conn["from_port"].AsInt32() == currentPort)
					{
						item.TargetNodeId = conn["to_node"].AsString();
					}
				}
				Options.Add(item);
				currentPort++; 
			}
		}
	}
}
