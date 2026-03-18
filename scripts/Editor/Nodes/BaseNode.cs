using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

namespace UmaArchive.Editor.Nodes
{
	[JsonDerivedType(typeof(DialogueNodeData), typeDiscriminator: "dialogue")]
	[JsonDerivedType(typeof(NarrativeNodeData), typeDiscriminator: "narrative")]
	[JsonDerivedType(typeof(MusicNodeData), typeDiscriminator: "music")]
	[JsonDerivedType(typeof(ChoiceNodeData), typeDiscriminator: "choice")]
	[JsonDerivedType(typeof(BranchNodeData), typeDiscriminator: "branch")]
	public abstract class BaseNodeData
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string NextNodeId { get; set; } = "";
		public bool IsExpanded { get; set; } = false;

		// 核心回调：用于通知管理器删除此节点
		[JsonIgnore]
		public Action OnDeleteRequested;

		public abstract GraphNode CreateGraphNode(GraphEdit host);
		public abstract void SyncFromView(GraphNode view);

		protected void SetupBaseNodeUI(GraphNode node)
		{
			// 创建功能头，设置固定高度确保可见
			HBoxContainer header = new HBoxContainer {
				CustomMinimumSize = new Vector2(0, 32),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			
			// 详细信息按钮 (左侧)
			Button detailBtn = new Button { 
				Text = " ≡ ", 
				Flat = true,
				TooltipText = "KEY_NODE_SETTINGS"
			};
			header.AddChild(detailBtn);
			
			Control spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			header.AddChild(spacer);

			// 删除按钮 (右侧)
			Button closeBtn = new Button {
				Text = " × ",
				Flat = true,
				TooltipText = "KEY_NODE_DELETE"
			};
			header.AddChild(closeBtn);
			
			node.AddChild(header);
			
			detailBtn.Pressed += () => OnDetailPressed(node);
			// 直接执行回调，绕过信号系统
			closeBtn.Pressed += () => OnDeleteRequested?.Invoke();
		}

		protected virtual void OnDetailPressed(GraphNode node) { }
	}
}
