using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

namespace UmaEraArchive.Editor.Nodes
{
	[JsonDerivedType(typeof(DialogueNodeData), typeDiscriminator: "dialogue")]
	[JsonDerivedType(typeof(NarrativeNodeData), typeDiscriminator: "narrative")]
	[JsonDerivedType(typeof(MusicNodeData), typeDiscriminator: "music")]
	[JsonDerivedType(typeof(ChoiceNodeData), typeDiscriminator: "choice")]
	[JsonDerivedType(typeof(BranchNodeData), typeDiscriminator: "branch")]
	[JsonDerivedType(typeof(StartNodeData), typeDiscriminator: "start")]
	[JsonDerivedType(typeof(EndNodeData), typeDiscriminator: "end")]
	[JsonDerivedType(typeof(BackgroundNodeData), typeDiscriminator: "background")]
	[JsonDerivedType(typeof(SpriteNodeData), typeDiscriminator: "sprite")]
	public abstract class BaseNodeData
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string NextNodeId { get; set; } = "";
		public bool IsExpanded { get; set; } = false;
		public float PosX { get; set; } = 0;
		public float PosY { get; set; } = 0;

		[JsonIgnore]
		public Action OnDeleteRequested { get; set; }

		[JsonIgnore]
		public Action<string> OnVisualEditRequested { get; set; }

		public abstract GraphNode CreateGraphNode(GraphEdit host);
		public abstract void SyncFromView(GraphNode view);

		// 便捷翻译方法，解决普通类无法调用 Tr 的问题
		protected string Tr(string key) => TranslationServer.Translate(key);

		protected void SetupBaseNodeUI(GraphNode node)
		{
			node.Resizable = true;
			node.CustomMinimumSize = new Vector2(200, 100);
			
			HBoxContainer header = new HBoxContainer();
			Button detailBtn = new Button { Text = "≡", Flat = true };
			Button closeBtn = new Button { Text = "×", Flat = true };
			
			header.AddChild(detailBtn);
			header.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
			header.AddChild(closeBtn);
			node.AddChild(header);
			
			detailBtn.Pressed += () => OnDetailPressed(node);
			closeBtn.Pressed += () => OnDeleteRequested?.Invoke();
		}

		protected virtual void OnDetailPressed(GraphNode node) { }
	}
}
