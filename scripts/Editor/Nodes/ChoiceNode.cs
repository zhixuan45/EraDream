using Godot;
using System.Collections.Generic;
using UmaEraArchive.Editor.Nodes;

public class ChoiceNodeData : BaseNodeData
{
	public List<ChoiceItem> Options { get; set; } = new List<ChoiceItem>();
	public float BlurValue { get; set; } = 0.0f;
	public float Darkness { get; set; } = 0.0f;

	private VBoxContainer _detailPanel;
	private HSlider _blurSlider;
	private HSlider _darkSlider;

	public class ChoiceItem
	{
		public string Text { get; set; } = "新选项";
		public string TargetNodeId { get; set; } = "";
	}

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_CHOICE"), Name = Id };
		SetupBaseNodeUI(node);
		
		node.SetSlot(0, false, 0, new Color(1, 1, 1), false, 0, new Color(1, 1, 1));

		// 详细面板 (滤镜等)
		_detailPanel = new VBoxContainer { Visible = IsExpanded };
		_detailPanel.AddChild(new Label { Text = "背景虚化 (Blur)", ThemeTypeVariation = "HeaderSmall" });
		_blurSlider = new HSlider { MinValue = 0, MaxValue = 5, Step = 0.1, Value = BlurValue };
		_detailPanel.AddChild(_blurSlider);

		_detailPanel.AddChild(new Label { Text = "背景暗度 (Darkness)", ThemeTypeVariation = "HeaderSmall" });
		_darkSlider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = Darkness };
		_detailPanel.AddChild(_darkSlider);
		node.AddChild(_detailPanel);

		if (Options.Count == 0) Options.Add(new ChoiceItem());

		for (int i = 0; i < Options.Count; i++)
		{
			AddOptionSlot(node, i, Options[i]);
		}

		Button addOptionBtn = new Button { Text = Tr("KEY_LABEL_ADD_OPTION"), Flat = true };
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

	protected override void OnDetailPressed(GraphNode node)
	{
		IsExpanded = !IsExpanded;
		_detailPanel.Visible = IsExpanded;
		ResetSize(node);
	}

	private void AddOptionSlot(GraphNode node, int index, ChoiceItem item)
	{
		HBoxContainer box = new HBoxContainer();
		LineEdit input = new LineEdit { 
			Text = item.Text, 
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			PlaceholderText = Tr("KEY_PLACEHOLDER_CHOICE")
		};
		box.AddChild(input);

		Button delBtn = new Button { Text = "×", Flat = true };
		box.AddChild(delBtn);

		node.AddChild(box);
		node.MoveChild(box, node.GetChildCount() - 2);

		int slotIndex = node.GetChildCount() - 2;
		
		bool enableInput = (index == 0);
		node.SetSlot(slotIndex, enableInput, 0, new Color(1, 1, 1), true, 0, new Color(1, 0.6f, 0));

		delBtn.Pressed += () => {
			if (node.GetChildCount() > 4) { // 考虑 detailPanel 和 addBtn
				node.RemoveChild(box);
				box.QueueFree();
				ResetSize(node);
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
		PosX = view.PositionOffset.X;
		PosY = view.PositionOffset.Y;
		IsExpanded = _detailPanel.Visible;

		BlurValue = (float)_blurSlider.Value;
		Darkness = (float)_darkSlider.Value;

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
