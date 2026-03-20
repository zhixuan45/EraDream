using Godot;
using UmaEraArchive.Editor.Nodes;
using System.Collections.Generic;

public class SpriteNodeData : BaseNodeData
{
	public int CharacterId { get; set; } = 0;
	public string ActionType { get; set; } = "Show"; // Show, Change, Hide
	public string Expression { get; set; } = "Neutral";
	public string Position { get; set; } = "Center"; // Left, Center, Right
	public bool IsSilhouette { get; set; } = false;
	
	// Visual Edit Properties
	public float OffsetX { get; set; } = 0;
	public float OffsetY { get; set; } = 0;
	public float Scale { get; set; } = 1.0f;
	public bool FlipH { get; set; } = false;

	private OptionButton _charSelector;
	private OptionButton _actionSelector;
	private OptionButton _exprSelector;
	private OptionButton _posSelector;
	private CheckBox _silhouetteCheck;
	private Button _btnVisualEdit;

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = Tr("KEY_NODE_SPRITE"), Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, true, 0, new Color(1, 1, 1), true, 0, new Color(1, 1, 1));

		VBoxContainer container = new VBoxContainer();

		// 角色选择
		_charSelector = new OptionButton { CustomMinimumSize = new Vector2(180, 0) };
		foreach (var c in CharacterManager.Characters) {
			_charSelector.AddItem(c.Name, c.Id);
			if (c.Id == CharacterId) _charSelector.Selected = _charSelector.GetItemCount() - 1;
		}
		container.AddChild(new Label { Text = Tr("KEY_LABEL_CHAR_SELECT") });
		container.AddChild(_charSelector);
		
		_charSelector.ItemSelected += (idx) => {
			CharacterId = _charSelector.GetSelectedId();
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
		node.CustomMinimumSize = new Vector2(200, 280);
		node.Size = Vector2.Zero;
		return node;
	}

	private void UpdateExpressionSelector()
	{
		_exprSelector.Clear();
		_exprSelector.AddItem("默认 (Default)");
		var charData = CharacterManager.Characters.Find(c => c.Id == CharacterId);
		if (charData != null)
		{
			foreach (var expr in charData.Expressions.Keys)
			{
				_exprSelector.AddItem(expr);
				if (expr == Expression) _exprSelector.Selected = _exprSelector.GetItemCount() - 1;
			}
		}
	}

	public override void SyncFromView(GraphNode view)
	{
		CharacterId = _charSelector.GetSelectedId();
		ActionType = _actionSelector.Selected switch { 1 => "Change", 2 => "Hide", _ => "Show" };
		Expression = _exprSelector.Selected > 0 ? _exprSelector.GetItemText(_exprSelector.Selected) : "Neutral";
		Position = _posSelector.Selected switch { 0 => "Left", 2 => "Right", _ => "Center" };
		IsSilhouette = _silhouetteCheck.ButtonPressed;
	}
}
