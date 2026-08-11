using Godot;
using System.Collections.Generic;
using EraDream.StoryEditor.Nodes;

namespace EraDream.StoryEditor.Nodes
{
	public class ChoiceNodeData : BaseNodeData
	{
		public List<ChoiceItem> Options { get; set; } = new List<ChoiceItem>();
		public float BlurValue { get; set; } = 0.0f;
		public float Darkness { get; set; } = 0.0f;

		private VBoxContainer _detailPanel;
		private HSlider _blurSlider;
		private HSlider _darkSlider;
		
		// 缓存选项行输入框的列表，用于 O(1) 准确同步
		private List<TextEdit> _optionInputs = new List<TextEdit>();

		public class ChoiceItem
		{
			public string Text { get; set; } = "新选项";
			public string TargetNodeId { get; set; } = "";
		}

		public override GraphNode CreateGraphNode(GraphEdit host)
		{
			GraphNode node = new GraphNode { Title = Tr("KEY_NODE_CHOICE"), Name = Id };
			SetupBaseNodeUI(node);
			
			// 开启顶部的左侧唯一输入端口 (slot 0)
			node.SetSlot(0, true, 0, new Color(1, 1, 1), false, 0, new Color(1, 1, 1));

			// 详细面板 (滤镜等)
			_detailPanel = new VBoxContainer { Visible = IsExpanded };
			_detailPanel.AddChild(new Label { Text = "背景虚化 (Blur)", ThemeTypeVariation = "HeaderSmall" });
			_blurSlider = new HSlider { MinValue = 0, MaxValue = 5, Step = 0.1, Value = BlurValue };
			_detailPanel.AddChild(_blurSlider);

			_detailPanel.AddChild(new Label { Text = "背景暗度 (Darkness)", ThemeTypeVariation = "HeaderSmall" });
			_darkSlider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = Darkness };
			_detailPanel.AddChild(_darkSlider);
			node.AddChild(_detailPanel);

			_optionInputs.Clear();
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
			if (_detailPanel != null) _detailPanel.Visible = IsExpanded;
			ResetSize(node);
		}

		private void AddOptionSlot(GraphNode node, int index, ChoiceItem item)
		{
			HBoxContainer box = new HBoxContainer();
			// 选项文本允许输入较长内容，并在节点内部自动换行。
			TextEdit input = new TextEdit {
				Text = item.Text,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(0, 48),
				PlaceholderText = Tr("KEY_PLACEHOLDER_CHOICE"),
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				ScrollFitContentHeight = true
			};
			box.AddChild(input);
			_optionInputs.Add(input); // 缓存输入框引用

			Button delBtn = new Button { Text = "×", Flat = true };
			box.AddChild(delBtn);

			node.AddChild(box);
			node.MoveChild(box, node.GetChildCount() - 2);

			int slotIndex = node.GetChildCount() - 2;
			
			// 所有的选项行左侧都不开启输入端口，只在右侧开启输出端口 ( slotIndex )
			node.SetSlot(slotIndex, false, 0, new Color(1, 1, 1), true, 0, new Color(1, 0.6f, 0));

			var capturedItem = item;
			delBtn.Pressed += () => {
				if (node.GetChildCount() > 4) { // 考虑 detailPanel 和 addBtn
					// 删除行会让 GraphNode 的端口重新编号，先断开旧连线，避免目标跟随旧端口错位。
					GraphEdit graph = node.GetParent() as GraphEdit;
					if (graph != null)
					{
						var oldConnections = new List<Godot.Collections.Dictionary>();
						foreach (Godot.Collections.Dictionary connection in graph.GetConnectionList())
							if (connection["from_node"].AsString() == node.Name) oldConnections.Add(connection);
						foreach (Godot.Collections.Dictionary connection in oldConnections)
						{
							graph.DisconnectNode(node.Name, connection["from_port"].AsInt32(), connection["to_node"].AsString(), connection["to_port"].AsInt32());
						}
					}
					_optionInputs.Remove(input);
					Options.Remove(capturedItem); // 移除对应的数据项，解决残留问题
					node.RemoveChild(box);
					box.QueueFree();
					if (graph != null)
					{
						// 按删除后的选项顺序恢复目标，保证保存和预览使用同一分支映射。
						for (int i = 0; i < Options.Count; i++)
							if (!string.IsNullOrEmpty(Options[i].TargetNodeId))
								graph.ConnectNode(node.Name, i, Options[i].TargetNodeId, 0);
					}
					ResetSize(node);
				}
			};
		}

		private void ResetSize(GraphNode node)
		{
			node.CustomMinimumSize = new Vector2(280, 80); // 最小高度限制为 80 像素，防高度缩死
			node.Size = Vector2.Zero;
		}

		public override void SyncFromView(GraphNode view)
		{
			PosX = view.PositionOffset.X;
			PosY = view.PositionOffset.Y;
			IsExpanded = _detailPanel != null && _detailPanel.Visible;

			if (_blurSlider != null) BlurValue = (float)_blurSlider.Value;
			if (_darkSlider != null) Darkness = (float)_darkSlider.Value;

			// 依靠缓存的 inputs 保证数据一致性
			var newOptions = new List<ChoiceItem>();
			GraphEdit graph = view.GetParent() as GraphEdit;
			if (graph == null) return;

			int currentPort = 0;
			for (int i = 0; i < _optionInputs.Count; i++)
			{
				var item = new ChoiceItem { Text = _optionInputs[i].Text };
				
				foreach (var conn in graph.GetConnectionList())
				{
					if (conn["from_node"].AsString() == view.Name && conn["from_port"].AsInt32() == currentPort)
					{
						item.TargetNodeId = conn["to_node"].AsString();
					}
				}
				newOptions.Add(item);
				currentPort++;
			}
			Options = newOptions;
		}
	}
}
