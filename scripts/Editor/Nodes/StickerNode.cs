using Godot;
using EraDream.Editor.Nodes;
using System.Collections.Generic;

// 贴纸节点数据，复用 SpriteNode 的结构但绑定贴纸而非角色
public class StickerNodeData : BaseNodeData
{
	public int StickerId { get; set; } = 0;
	public string ActionType { get; set; } = "Show"; // Show, Hide
	
	// 可视化编辑属性
	public float OffsetX { get; set; } = 0;
	public float OffsetY { get; set; } = 0;
	public float Scale { get; set; } = 1.0f;
	public bool FlipH { get; set; } = false;

	private OptionButton _stickerSelector;
	private OptionButton _actionSelector;
	private Button _btnVisualEdit;

	public override GraphNode CreateGraphNode(GraphEdit host)
	{
		GraphNode node = new GraphNode { Title = "贴纸 (Sticker)", Name = Id };
		SetupBaseNodeUI(node);
		node.SetSlot(0, true, 0, new Color(1, 1, 1), true, 0, new Color(1, 1, 1));

		VBoxContainer container = new VBoxContainer();

		// 贴纸选择
		_stickerSelector = new OptionButton { CustomMinimumSize = new Vector2(180, 0) };
		foreach (var s in StickerManager.Stickers) {
			_stickerSelector.AddItem(s.Name, s.Id);
			if (s.Id == StickerId) _stickerSelector.Selected = _stickerSelector.GetItemCount() - 1;
		}
		container.AddChild(new Label { Text = "选择贴纸" });
		container.AddChild(_stickerSelector);
		
		_stickerSelector.ItemSelected += (idx) => {
			StickerId = _stickerSelector.GetSelectedId();
		};

		// 操作类型
		_actionSelector = new OptionButton();
		_actionSelector.AddItem("显示", 0);
		_actionSelector.AddItem("隐藏", 1);
		_actionSelector.Selected = ActionType == "Hide" ? 1 : 0;
		container.AddChild(new Label { Text = "操作类型" });
		container.AddChild(_actionSelector);

		// 可视化编辑按钮
		_btnVisualEdit = new Button { Text = "可视化编辑", CustomMinimumSize = new Vector2(0, 30) };
		_btnVisualEdit.Pressed += () => OnVisualEditRequested?.Invoke(Id);
		container.AddChild(_btnVisualEdit);

		// 隐藏时禁用编辑按钮
		_actionSelector.ItemSelected += (idx) => {
			_btnVisualEdit.Disabled = (idx == 1);
		};
		_btnVisualEdit.Disabled = ActionType == "Hide";

		node.AddChild(container);
		node.CustomMinimumSize = new Vector2(200, 220);
		node.Size = Vector2.Zero;
		return node;
	}

	public override void SyncFromView(GraphNode view)
	{
		PosX = view.PositionOffset.X;
		PosY = view.PositionOffset.Y;

		StickerId = _stickerSelector.GetSelectedId();
		ActionType = _actionSelector.Selected == 1 ? "Hide" : "Show";
	}
}
