using Godot;
using EraDream.StoryEditor.Nodes;
using System.Collections.Generic;

namespace EraDream.StoryEditor.Nodes
{
	public class SpriteNodeData : BaseNodeData
	{
		public string CharacterId { get; set; } = "";
		public string ActionType { get; set; } = "Show"; // Show, Change, Hide
		public string Expression { get; set; } = "Neutral";
		public string Position { get; set; } = "Center"; // Left, Center, Right
		public bool IsSilhouette { get; set; } = false;
		
		// Visual Edit Properties
		public float OffsetX { get; set; } = 0;
		public float OffsetY { get; set; } = 0;
		public float Scale { get; set; } = 1.0f;
		public bool FlipH { get; set; } = false;
		public float FadeInDuration { get; set; } = 0.25f;
		public float FadeOutDuration { get; set; } = 0.25f;

		private OptionButton _charSelector;
		private OptionButton _actionSelector;
		private OptionButton _exprSelector;
		private OptionButton _posSelector;
		private CheckBox _silhouetteCheck;
		private Button _btnVisualEdit;
		private LineEdit _fadeInInput;
		private LineEdit _fadeOutInput;
		private HSlider _fadeInSlider;
		private HSlider _fadeOutSlider;
		private bool _syncingFade;

		public override GraphNode CreateGraphNode(GraphEdit host)
		{
			GraphNode node = new GraphNode { Title = Tr("KEY_NODE_SPRITE"), Name = Id };
			SetupBaseNodeUI(node);
			node.SetSlot(0, true, 0, new Color(1, 1, 1), true, 0, new Color(1, 1, 1));

			VBoxContainer container = new VBoxContainer();

			// 角色选择
			_charSelector = new OptionButton { CustomMinimumSize = new Vector2(180, 0) };
			UpdateCharacterSelector();
			container.AddChild(new Label { Text = Tr("KEY_LABEL_CHAR_SELECT") });
			container.AddChild(_charSelector);
			
			_charSelector.ItemSelected += (idx) => {
				var actor = CharacterManager.GetActorByIndex((int)idx);
				CharacterId = actor?.ActorId ?? "";
				UpdateExpressionSelector();
			};

			// 操作类型
			_actionSelector = new OptionButton();
			_actionSelector.AddItem(Tr("KEY_ACTION_SHOW"), 0);
			_actionSelector.AddItem(Tr("KEY_ACTION_CHANGE"), 1);
			_actionSelector.AddItem(Tr("KEY_ACTION_HIDE"), 2);
			_actionSelector.Selected = ActionType switch { "Change" => 1, "Hide" => 2, _ => 0 };
			container.AddChild(new Label { Text = Tr("KEY_LABEL_ACTION_TYPE") });
			container.AddChild(_actionSelector);

			// 表情/差分选择
			_exprSelector = new OptionButton();
			UpdateExpressionSelector();
			container.AddChild(new Label { Text = Tr("KEY_LABEL_EXPRESSION") });
			container.AddChild(_exprSelector);

			// 位置选择
			_posSelector = new OptionButton();
			_posSelector.AddItem(Tr("KEY_POS_LEFT"), 0);
			_posSelector.AddItem(Tr("KEY_POS_CENTER"), 1);
			_posSelector.AddItem(Tr("KEY_POS_RIGHT"), 2);
			_posSelector.Selected = Position switch { "Left" => 0, "Right" => 2, _ => 1 };
			container.AddChild(new Label { Text = Tr("KEY_LABEL_POSITION") });
			container.AddChild(_posSelector);

			// 剪影开关
			_silhouetteCheck = new CheckBox { Text = Tr("KEY_LABEL_SILHOUETTE"), ButtonPressed = IsSilhouette };
			container.AddChild(_silhouetteCheck);

			container.AddChild(new Label { Text = "淡入时长（秒）" });
			(_fadeInSlider, _fadeInInput) = CreateFadeControl(FadeInDuration, value => FadeInDuration = value);
			container.AddChild(CreateFadeRow(_fadeInSlider, _fadeInInput, true));
			container.AddChild(new Label { Text = "淡出时长（秒）" });
			(_fadeOutSlider, _fadeOutInput) = CreateFadeControl(FadeOutDuration, value => FadeOutDuration = value);
			container.AddChild(CreateFadeRow(_fadeOutSlider, _fadeOutInput, false));

			// 可视化编辑按钮
			_btnVisualEdit = new Button { Text = "可视化编辑", CustomMinimumSize = new Vector2(0, 30) };
			_btnVisualEdit.Pressed += () => OnVisualEditRequested?.Invoke(Id);
			container.AddChild(_btnVisualEdit);

			// 交互逻辑
			_actionSelector.ItemSelected += (idx) => {
				bool isHide = (idx == 2);
				_exprSelector.Disabled = isHide;
				_posSelector.Disabled = isHide;
				_silhouetteCheck.Disabled = isHide;
				_btnVisualEdit.Disabled = isHide;
			};
			// 初始化禁用状态
			_btnVisualEdit.Disabled = ActionType == "Hide";

			node.AddChild(container);
			node.CustomMinimumSize = new Vector2(220, 390);
			node.Size = Vector2.Zero;
			return node;
		}

		private void UpdateExpressionSelector()
		{
			if (_exprSelector == null) return;
			_exprSelector.Clear();
			_exprSelector.AddItem("默认 (Default)");
			var actor = CharacterManager.GetActor(CharacterId);
			if (actor != null)
			{
				foreach (var expr in actor.Visuals.Expressions.Keys)
				{
					_exprSelector.AddItem(expr);
					if (expr == Expression) _exprSelector.Selected = _exprSelector.GetItemCount() - 1;
				}
			}
		}

		private void UpdateCharacterSelector()
		{
			if (_charSelector == null) return;
			_charSelector.Clear();
			foreach (var actor in CharacterManager.Characters)
			{
				_charSelector.AddItem(actor.DisplayName);
				if (actor.ActorId == CharacterId)
					_charSelector.Selected = _charSelector.GetItemCount() - 1;
			}
		}

		public override void RefreshEditorView()
		{
			UpdateCharacterSelector();
			UpdateExpressionSelector();
		}

		public override void SyncFromView(GraphNode view)
		{
			PosX = view.PositionOffset.X;
			PosY = view.PositionOffset.Y;

			if (_charSelector != null)
			{
				var actor = CharacterManager.GetActorByIndex(_charSelector.Selected);
				CharacterId = actor?.ActorId ?? "";
			}

			if (_actionSelector != null) ActionType = _actionSelector.Selected switch { 1 => "Change", 2 => "Hide", _ => "Show" };
			if (_exprSelector != null) Expression = _exprSelector.Selected > 0 ? _exprSelector.GetItemText(_exprSelector.Selected) : "Neutral";
			if (_posSelector != null) Position = _posSelector.Selected switch { 0 => "Left", 2 => "Right", _ => "Center" };
			if (_silhouetteCheck != null) IsSilhouette = _silhouetteCheck.ButtonPressed;
			if (_fadeInInput != null && float.TryParse(_fadeInInput.Text, out float fadeIn) && float.IsFinite(fadeIn)) FadeInDuration = fadeIn;
			if (_fadeOutInput != null && float.TryParse(_fadeOutInput.Text, out float fadeOut) && float.IsFinite(fadeOut)) FadeOutDuration = fadeOut;
		}

		private (HSlider, LineEdit) CreateFadeControl(float value, System.Action<float> setter)
		{
			var slider = new HSlider { MinValue = 0, MaxValue = 3, Step = 0.05, Value = Mathf.Clamp(value, 0, 3) };
			var input = new LineEdit { Text = value.ToString("0.##"), CustomMinimumSize = new Vector2(80, 0), PlaceholderText = "请输入数字" };
			slider.ValueChanged += sliderValue =>
			{
				if (_syncingFade) return;
				setter((float)sliderValue);
				_syncingFade = true;
				input.Text = ((float)sliderValue).ToString("0.##");
				_syncingFade = false;
			};
			input.TextChanged += text =>
			{
				if (_syncingFade || !float.TryParse(text, out float parsed) || !float.IsFinite(parsed)) return;
				setter(parsed);
				_syncingFade = true;
				slider.Value = Mathf.Clamp(parsed, (float)slider.MinValue, (float)slider.MaxValue);
				_syncingFade = false;
			};
			return (slider, input);
		}

		private HBoxContainer CreateFadeRow(HSlider slider, LineEdit input, bool fadeIn)
		{
			var row = new HBoxContainer();
			row.AddChild(slider);
			row.AddChild(input);
			return row;
		}
	}
}
