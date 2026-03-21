using System;
using Godot;
using System.Text.Json.Serialization;

namespace UmaEraArchive.Editor.Nodes
{
	public class ValueNodeData : BaseNodeData
	{
		public string TargetAttribute { get; set; } = "Money";
		public int ChangeValue { get; set; } = 0;

		public override GraphNode CreateGraphNode(GraphEdit host)
		{
			var node = new GraphNode { 
				Name = Id,
				Title = Tr("数值变更"), 
				PositionOffset = new Vector2(PosX, PosY),
				CustomMinimumSize = new Vector2(200, 120)
			};
			SetupBaseNodeUI(node);

			// 属性选择 (Index 1)
			OptionButton attrPicker = new OptionButton {
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			attrPicker.AddItem(Tr("金钱"), 0);
			attrPicker.AddItem(Tr("体力"), 1);
			attrPicker.AddItem(Tr("精力"), 2);
			attrPicker.AddItem(Tr("速度"), 3);
			attrPicker.AddItem(Tr("耐力"), 4);
			attrPicker.AddItem(Tr("力量"), 5);
			attrPicker.AddItem(Tr("根性"), 6);
			attrPicker.AddItem(Tr("智力"), 7);
			attrPicker.AddItem(Tr("技能点"), 8);

			attrPicker.Select(GetAttrIndex(TargetAttribute));
			node.AddChild(attrPicker);

			// 数值输入 (Index 2)
			SpinBox valueBox = new SpinBox {
				MinValue = -9999,
				MaxValue = 9999,
				Value = ChangeValue,
				Prefix = Tr("变化量: "),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			node.AddChild(valueBox);

			// 槽位设置 (左入右出)
			node.SetSlot(0, true, 0, new Color(1, 1, 1), true, 0, new Color(1, 1, 1));

			return node;
		}

		public override void SyncFromView(GraphNode view)
		{
			PosX = view.PositionOffset.X;
			PosY = view.PositionOffset.Y;
			
			var attrPicker = view.GetChild<OptionButton>(1);
			var valueBox = view.GetChild<SpinBox>(2);

			TargetAttribute = GetAttrKey(attrPicker.Selected);
			ChangeValue = (int)valueBox.Value;
		}

		private int GetAttrIndex(string key) => key switch {
			"Money" => 0, "Stamina" => 1, "Energy" => 2,
			"Speed" => 3, "Endurance" => 4, "Power" => 5,
			"Guts" => 6, "Intelligence" => 7, "SkillPoint" => 8,
			_ => 0
		};

		private string GetAttrKey(int index) => index switch {
			0 => "Money", 1 => "Stamina", 2 => "Energy",
			3 => "Speed", 4 => "Endurance", 5 => "Power",
			6 => "Guts", 7 => "Intelligence", 8 => "SkillPoint",
			_ => "Money"
		};
	}
}
