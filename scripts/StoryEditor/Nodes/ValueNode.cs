using System;
using Godot;
using System.Text.Json.Serialization;

namespace EraDream.StoryEditor.Nodes
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
				Title = Tr("KEY_NODE_VALUE_CHANGE"), 
				PositionOffset = new Vector2(PosX, PosY),
				CustomMinimumSize = new Vector2(200, 160)
			};
			SetupBaseNodeUI(node);

			// 属性选择
			_attrPicker = new OptionButton {
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			_attrPicker.AddItem(Tr("KEY_ATTR_MONEY"), 0);
			_attrPicker.AddItem(Tr("KEY_ATTR_VITALITY"), 1);
			_attrPicker.AddItem(Tr("KEY_ATTR_ENERGY"), 2);
			_attrPicker.AddItem(Tr("KEY_ATTR_SPEED"), 3);
			_attrPicker.AddItem(Tr("KEY_ATTR_STAMINA"), 4);
			_attrPicker.AddItem(Tr("KEY_ATTR_POWER"), 5);
			_attrPicker.AddItem(Tr("KEY_ATTR_GUTS"), 6);
			_attrPicker.AddItem(Tr("KEY_ATTR_INTELLIGENCE"), 7);
			_attrPicker.AddItem(Tr("KEY_ATTR_SKILLPOINTS"), 8);
			_attrPicker.AddItem(Tr("KEY_ATTR_AFFECTION"), 10);
			_attrPicker.AddItem(Tr("KEY_ATTR_CUSTOM"), 9);

			_attrPicker.Select(GetAttrIndex(TargetAttribute));
			node.AddChild(_attrPicker);

			// 自定义 ID 输入
			_customIdInput = new LineEdit {
				PlaceholderText = Tr("KEY_PLACEHOLDER_CUSTOM_VAR"),
				Text = CustomId,
				Visible = TargetAttribute == "Custom",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			_attrPicker.ItemSelected += (idx) => {
				if (_customIdInput != null) _customIdInput.Visible = (idx == 9);
			};
			node.AddChild(_customIdInput);

			// 数值输入
			_valueBox = new SpinBox {
				MinValue = -9999,
				MaxValue = 9999,
				Value = ChangeValue,
				Prefix = Tr("KEY_PREFIX_CHANGE_VAL"),
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
			
			if (_attrPicker != null) TargetAttribute = GetAttrKey(_attrPicker.Selected);
			if (_customIdInput != null) CustomId = TargetAttribute == "Custom" ? _customIdInput.Text.Trim() : "";
			if (_valueBox != null) ChangeValue = (int)_valueBox.Value;
		}

		private int GetAttrIndex(string key) => key switch {
			"Money" => 0, "Vitality" => 1, "Energy" => 2,
			"Speed" => 3, "Stamina" => 4, "Power" => 5,
			"Guts" => 6, "Intelligence" => 7, "SkillPoints" => 8,
			"Affection" => 10,
			"Custom" => 9,
			_ => 0
		};

		private string GetAttrKey(int index) => index switch {
			0 => "Money", 1 => "Vitality", 2 => "Energy",
			3 => "Speed", 4 => "Stamina", 5 => "Power",
			6 => "Guts", 7 => "Intelligence", 8 => "SkillPoints",
			10 => "Affection",
			9 => "Custom",
			_ => "Money"
		};
	}
}
