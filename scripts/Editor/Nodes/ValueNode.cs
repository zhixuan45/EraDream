using System;
using Godot;
using System.Text.Json.Serialization;

namespace UmaEraArchive.Editor.Nodes
{
	public class ValueNodeData : BaseNodeData
	{
		[JsonPropertyName("target_attribute")]
		public string TargetAttribute { get; set; } = "Money";

		[JsonPropertyName("custom_id")]
		public string CustomId { get; set; } = "";

		[JsonPropertyName("change_value")]
		public int ChangeValue { get; set; } = 0;

		private OptionButton _attrPicker;
		private LineEdit _customIdInput;
		private SpinBox _valueBox;

		public override GraphNode CreateGraphNode(GraphEdit host)
		{
			var node = new GraphNode { 
				Name = Id,
				Title = Tr("数值变更"), 
				PositionOffset = new Vector2(PosX, PosY),
				CustomMinimumSize = new Vector2(200, 160)
			};
			SetupBaseNodeUI(node);

			// 属性选择
			_attrPicker = new OptionButton {
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			_attrPicker.AddItem(Tr("金钱"), 0);
			_attrPicker.AddItem(Tr("体力"), 1);
			_attrPicker.AddItem(Tr("精力"), 2);
			_attrPicker.AddItem(Tr("速度"), 3);
			_attrPicker.AddItem(Tr("耐力"), 4);
			_attrPicker.AddItem(Tr("力量"), 5);
			_attrPicker.AddItem(Tr("根性"), 6);
			_attrPicker.AddItem(Tr("智力"), 7);
			_attrPicker.AddItem(Tr("技能点"), 8);
			_attrPicker.AddItem(Tr("自定义 (MOD/变量)"), 9);

			_attrPicker.Select(GetAttrIndex(TargetAttribute));
			node.AddChild(_attrPicker);

			// 自定义 ID 输入
			_customIdInput = new LineEdit {
				PlaceholderText = "变量 ID (如 favor_points)",
				Text = CustomId,
				Visible = TargetAttribute == "Custom",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			_attrPicker.ItemSelected += (idx) => {
				_customIdInput.Visible = (idx == 9);
			};
			node.AddChild(_customIdInput);

			// 数值输入
			_valueBox = new SpinBox {
				MinValue = -9999,
				MaxValue = 9999,
				Value = ChangeValue,
				Prefix = Tr("变化量: "),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			node.AddChild(_valueBox);

			// 槽位设置 (左入右出)
			node.SetSlot(0, true, 0, new Color(1, 1, 1), true, 0, new Color(1, 1, 1));

			return node;
		}

		public override void SyncFromView(GraphNode view)
		{
			PosX = view.PositionOffset.X;
			PosY = view.PositionOffset.Y;
			
			TargetAttribute = GetAttrKey(_attrPicker.Selected);
			CustomId = _customIdInput.Text.Trim();
			ChangeValue = (int)_valueBox.Value;
		}

		private int GetAttrIndex(string key) => key switch {
			"Money" => 0, "Stamina" => 1, "Energy" => 2,
			"Speed" => 3, "Endurance" => 4, "Power" => 5,
			"Guts" => 6, "Intelligence" => 7, "SkillPoint" => 8,
			"Custom" => 9,
			_ => 0
		};

		private string GetAttrKey(int index) => index switch {
			0 => "Money", 1 => "Stamina", 2 => "Energy",
			3 => "Speed", 4 => "Endurance", 5 => "Power",
			6 => "Guts", 7 => "Intelligence", 8 => "SkillPoint",
			9 => "Custom",
			_ => "Money"
		};
	}
}
