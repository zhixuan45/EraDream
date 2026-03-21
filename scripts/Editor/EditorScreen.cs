using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UmaEraArchive.Editor.Nodes;
using UmaEraArchive.Core;

public partial class EditorScreen : Control
{
	private GraphEdit _graphEdit;
	private Dictionary<string, BaseNodeData> _nodeDataMap = new Dictionary<string, BaseNodeData>();
	private MenuBar _menuBar;
	private UndoRedo _undoRedo = new UndoRedo();

	private Control _loadingOverlay;
	private ProgressBar _loadingProgress;

	// 侧边栏布局组件
	private VBoxContainer _sideScrollVBox;
	private Button _btnReturn;

	public override void _Ready()
	{
		SetupLayout();
		_graphEdit = GetNode<GraphEdit>("VBoxContainer/HSplitContainer/GraphEdit");
		_graphEdit.AddThemeColorOverride("activity_color", new Color(1, 0.2f, 0.2f)); // 设置激活连线颜色为警告红
		
		SetupLoadingOverlay();
		SetupSidePanelStructure();

		// 1. 剧情内容分类
		CreateCollapsibleCategory("剧情内容", out var contentCategory);
		AddCategoryButton(contentCategory, "添加对话节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new DialogueNodeData());
		});
		AddCategoryButton(contentCategory, "添加叙述节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new NarrativeNodeData());
		});

		// 2. 分支逻辑分类
		CreateCollapsibleCategory("分支判定", out var logicCategory);
		AddCategoryButton(logicCategory, "添加选项节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new ChoiceNodeData());
		});
		AddCategoryButton(logicCategory, "添加条件分支", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new BranchNodeData());
		});
		AddCategoryButton(logicCategory, "添加数值节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new ValueNodeData());
		});

		// 3. 视听控制分类
		CreateCollapsibleCategory("视听效果", out var assetCategory);
		AddCategoryButton(assetCategory, "添加背景节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new BackgroundNodeData());
		});
		AddCategoryButton(assetCategory, "添加立绘节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new SpriteNodeData());
		});
		AddCategoryButton(assetCategory, "添加音乐节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new MusicNodeData());
		});

		// 4. 流程工具分类
		CreateCollapsibleCategory("流程与预览", out var flowCategory);
		AddCategoryButton(flowCategory, "添加开始节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new StartNodeData());
		});
		AddCategoryButton(flowCategory, "添加结束节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new EndNodeData());
		});
		AddCategoryButton(flowCategory, "自动纠正节点顺序", () => {
			if (EnsureProjectOpen()) AutoFixNodeOrder();
		});
		AddCategoryButton(flowCategory, " 预览剧情 ", () => {
			if (EnsureProjectOpen()) LaunchPreview(null, false);
		});

		// 基础图表信号
		_graphEdit.ConnectionRequest += (f, fp, t, tp) => {
			_undoRedo.CreateAction("Connect Nodes");
			_undoRedo.AddDoMethod(Callable.From(() => ConnectNodesUndoable(f, fp, t, tp)));
			_undoRedo.AddUndoMethod(Callable.From(() => DisconnectNodesUndoable(f, fp, t, tp)));
			_undoRedo.CommitAction();
		};
		_graphEdit.DisconnectionRequest += (f, fp, t, tp) => {
			if (!EnsureProjectOpen()) return;
			_undoRedo.CreateAction("Disconnect Nodes");
			_undoRedo.AddDoMethod(Callable.From(() => DisconnectNodesUndoable(f, fp, t, tp)));
			_undoRedo.AddUndoMethod(Callable.From(() => ConnectNodesUndoable(f, fp, t, tp)));
			_undoRedo.CommitAction();
		};
		_graphEdit.DeleteNodesRequest += (nodes) => { 
			if (!EnsureProjectOpen()) return;
			_undoRedo.CreateAction("Delete Nodes");
			foreach (string n in nodes)
			{
				if (_nodeDataMap.TryGetValue(n, out var data))
				{
					Vector2 pos = _graphEdit.GetNode<GraphNode>(n).PositionOffset;
					_undoRedo.AddDoMethod(Callable.From(() => DeleteNode(n)));
					_undoRedo.AddUndoMethod(Callable.From(() => SpawnNodeUndoable(data, pos)));
				}
			}
			_undoRedo.CommitAction();
		};
	}

	private void SetupSidePanelStructure()
	{
		var sidePanel = GetNode<PanelContainer>("VBoxContainer/HSplitContainer/SidePanel");
		foreach (Node child in sidePanel.GetChildren()) child.QueueFree();

		var rootVBox = new VBoxContainer { Name = "RootVBox" };
		rootVBox.AddThemeConstantOverride("separation", 10);
		sidePanel.AddChild(rootVBox);

		Label titleLabel = new Label { Text = Tr("KEY_EDITOR_TITLE"), HorizontalAlignment = HorizontalAlignment.Center };
		titleLabel.AddThemeFontSizeOverride("font_size", 24);
		rootVBox.AddChild(titleLabel);
		rootVBox.AddChild(new HSeparator());

		ScrollContainer scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		rootVBox.AddChild(scroll);

		_sideScrollVBox = new VBoxContainer { Name = "ScrollVBox", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_sideScrollVBox.AddThemeConstantOverride("separation", 12);
		scroll.AddChild(_sideScrollVBox);

		rootVBox.AddChild(new HSeparator());
		_btnReturn = new Button { Text = Tr("KEY_RETURN_TO_MENU") };
		_btnReturn.Pressed += () => {
			LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
			GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
		};
		rootVBox.AddChild(_btnReturn);
	}

	private void CreateCollapsibleCategory(string title, out VBoxContainer contentContainer)
	{
		var categoryRoot = new VBoxContainer { Name = title + "Category" };
		_sideScrollVBox.AddChild(categoryRoot);

		Button headerBtn = new Button { Text = "▼ " + title, Alignment = HorizontalAlignment.Left, ThemeTypeVariation = "HeaderSmall", Flat = true };
		categoryRoot.AddChild(headerBtn);

		var contentVBox = new VBoxContainer { Name = "Content", Visible = true };
		contentVBox.AddThemeConstantOverride("margin_left", 15);
		categoryRoot.AddChild(contentVBox);
		contentContainer = contentVBox;

		headerBtn.Pressed += () => {
			contentVBox.Visible = !contentVBox.Visible;
			headerBtn.Text = (contentVBox.Visible ? "▼ " : "▶ ") + title;
		};
	}

	private void AddCategoryButton(VBoxContainer category, string text, Action onPressed)
	{
		Button btn = new Button { Text = text, Alignment = HorizontalAlignment.Left };
		btn.Pressed += onPressed;
		category.AddChild(btn);
	}

	private void ConnectNodesUndoable(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		_graphEdit.ConnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
		if (_nodeDataMap.TryGetValue(fromNode, out var fromData) && _nodeDataMap.TryGetValue(toNode, out var toData))
		{
			if (fromData is ChoiceNodeData c) { if (fromPort < c.Options.Count) c.Options[(int)fromPort].TargetNodeId = toData.Id; }
			else if (fromData is BranchNodeData b) { if (fromPort == 0) b.SuccessNodeId = toData.Id; else if (fromPort == 1) b.FailNodeId = toData.Id; }
			else fromData.NextNodeId = toData.Id;
			UpdateNodeWarnings();
		}
	}

	private void DisconnectNodesUndoable(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		_graphEdit.DisconnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
		if (_nodeDataMap.TryGetValue(fromNode, out var fromData))
		{
			if (fromData is ChoiceNodeData c) { if (fromPort < c.Options.Count) c.Options[(int)fromPort].TargetNodeId = null; }
			else if (fromData is BranchNodeData b) { if (fromPort == 0) b.SuccessNodeId = null; else if (fromPort == 1) b.FailNodeId = null; }
			else fromData.NextNodeId = null;
			UpdateNodeWarnings();
		}
	}

	private void SpawnNodeWithUndo(BaseNodeData data)
	{
		_undoRedo.CreateAction("Add Node");
		Vector2 pos = StoryNodeManager.GetViewCenter(_graphEdit) - new Vector2(100, 50);
		_undoRedo.AddDoMethod(Callable.From(() => SpawnNodeUndoable(data, pos)));
		_undoRedo.AddUndoMethod(Callable.From(() => DeleteNode(data.Id)));
		_undoRedo.CommitAction();
	}

	private void SpawnNodeUndoable(BaseNodeData data, Vector2 pos) => SpawnNode(data, pos);

	private bool EnsureProjectOpen()
	{
		if (!ProjectManager.IsProjectOpened) { GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("请先新建或打开一个项目！"); return false; }
		return true;
	}

	private void LaunchPreview(string startNodeId = null, bool isEditMode = false)
	{
		if (!EnsureProjectOpen()) return;
		foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>()) if (_nodeDataMap.TryGetValue(node.Name, out var data)) data.SyncFromView(node);
		StoryNodeManager.SyncConnectionsAndPositions(_graphEdit, _nodeDataMap.Values.ToList());
		StoryPreviewUI.Preview(this, _nodeDataMap.Values.ToList(), startNodeId, isEditMode);
	}

	private void SetupLayout()
	{
		var oldHSplit = GetNode<HSplitContainer>("HSplitContainer");
		RemoveChild(oldHSplit);

		VBoxContainer mainVBox = new VBoxContainer { Name = "VBoxContainer" };
		mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(mainVBox);

		_menuBar = new MenuBar { CustomMinimumSize = new Vector2(0, 30) };
		mainVBox.AddChild(_menuBar);
		SetupMenus();

		mainVBox.AddChild(oldHSplit);
		oldHSplit.SizeFlagsVertical = SizeFlags.ExpandFill;
	}

	private void SetupMenus()
	{
		PopupMenu fileMenu = new PopupMenu { Name = "File" };
		fileMenu.AddItem("新建项目", 0);
		fileMenu.AddItem("打开项目文件夹", 1);
		fileMenu.AddSeparator();
		fileMenu.AddItem("保存项目", 2);
		
		PopupMenu importMenu = new PopupMenu { Name = "Import" };
		importMenu.AddItem("导入背景图", 10);
		importMenu.AddItem("导入音频", 11);
		importMenu.AddItem("导入立绘", 12);
		fileMenu.AddSeparator();
		fileMenu.AddChild(importMenu);
		fileMenu.AddSubmenuNodeItem("导入资源", importMenu);

		_menuBar.AddChild(fileMenu);
		_menuBar.SetMenuTitle(0, "文件");
		fileMenu.IdPressed += OnFileMenuIdPressed;
		importMenu.IdPressed += OnImportMenuIdPressed;

		PopupMenu editMenu = new PopupMenu { Name = "Edit" };
		editMenu.AddItem("撤销 (Ctrl+Z)", 0);
		editMenu.AddItem("重做 (Ctrl+Y)", 1);
		_menuBar.AddChild(editMenu);
		_menuBar.SetMenuTitle(1, "编辑");
		editMenu.IdPressed += OnEditMenuIdPressed;

		PopupMenu projectMenu = new PopupMenu { Name = "Project" };
		projectMenu.AddItem("项目信息", 0);
		projectMenu.AddSeparator();
		projectMenu.AddItem("导出剧情包 (.era)", 1);
		projectMenu.AddItem("导出项目压缩包 (.zip)", 2);
		_menuBar.AddChild(projectMenu);
		_menuBar.SetMenuTitle(2, "项目");
		projectMenu.IdPressed += OnProjectMenuIdPressed;

		PopupMenu charMenu = new PopupMenu { Name = "Character" };
		charMenu.AddItem("角色列表管理", 0);
		charMenu.AddItem("导出角色配置", 1);
		charMenu.AddItem("导入角色配置", 2);
		_menuBar.AddChild(charMenu);
		_menuBar.SetMenuTitle(3, "角色");
		charMenu.IdPressed += OnCharMenuIdPressed;

		PopupMenu searchMenu = new PopupMenu { Name = "Search" };
		searchMenu.AddItem("全局搜索 (Ctrl+F)", 0);
		_menuBar.AddChild(searchMenu);
		_menuBar.SetMenuTitle(4, "搜索");
		searchMenu.IdPressed += (id) => OpenSearchDialog();
	}

	private void OnFileMenuIdPressed(long id)
	{
		switch (id)
		{
			case 0: FileIOManager.OpenFolderDialog("选择新项目文件夹", (path) => { ProjectManager.CreateNewProject(path); LoadAndRender(ProjectManager.StoryFile); }); break;
			case 1: FileIOManager.OpenLoadDialog("选择项目文件", "*.uma", (path) => { if (ProjectManager.OpenProject(path)) { CharacterManager.LoadCharacters(ProjectManager.CharacterFile); LoadAndRender(ProjectManager.StoryFile); } }); break;
			case 2: if (EnsureProjectOpen()) { StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile); CharacterManager.SaveCharacters(ProjectManager.CharacterFile); ProjectManager.SaveMetadata(); GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("项目保存成功！"); } break;
		}
	}

	private void OnEditMenuIdPressed(long id) { switch (id) { case 0: _undoRedo.Undo(); break; case 1: _undoRedo.Redo(); break; } }

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.Z && !keyEvent.ShiftPressed) { _undoRedo.Undo(); GetViewport().SetInputAsHandled(); }
			else if (keyEvent.CtrlPressed && (keyEvent.Keycode == Key.Y || (keyEvent.Keycode == Key.Z && keyEvent.ShiftPressed))) { _undoRedo.Redo(); GetViewport().SetInputAsHandled(); }
			else if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.F) { OpenSearchDialog(); GetViewport().SetInputAsHandled(); }
		}
	}

	private void OnProjectMenuIdPressed(long id)
	{
		if (!EnsureProjectOpen()) return;
		switch (id)
		{
			case 0: ShowProjectInfoDialog(); break;
			case 1: StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile); CharacterManager.SaveCharacters(ProjectManager.CharacterFile); ProjectManager.SaveMetadata(); FileIOManager.OpenSaveDialog("导出剧情包", $"{ProjectManager.Metadata.Title}.era", "*.era", (path) => ProjectManager.ExportAsEra(path)); break;
			case 2: StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile); CharacterManager.SaveCharacters(ProjectManager.CharacterFile); ProjectManager.SaveMetadata(); FileIOManager.OpenSaveDialog("导出项目压缩包", $"{ProjectManager.Metadata.Title}.zip", "*.zip", (path) => ProjectManager.ExportProject(path)); break;
		}
	}

	private void ShowProjectInfoDialog()
	{
		var dialog = new AcceptDialog { Title = "项目信息" };
		var margin = new MarginContainer { CustomMinimumSize = new Vector2(400, 300) };
		margin.AddThemeConstantOverride("margin_top", 10); margin.AddThemeConstantOverride("margin_bottom", 10); margin.AddThemeConstantOverride("margin_left", 15); margin.AddThemeConstantOverride("margin_right", 15);
		var vbox = new VBoxContainer(); vbox.AddThemeConstantOverride("separation", 8); margin.AddChild(vbox);
		vbox.AddChild(new Label { Text = "修改项目基本信息", HorizontalAlignment = HorizontalAlignment.Center });
		vbox.AddChild(new HSeparator());
		var titleEdit = new LineEdit { Text = ProjectManager.Metadata.Title, PlaceholderText = "请输入项目标题" };
		vbox.AddChild(new Label { Text = "标题:" }); vbox.AddChild(titleEdit);
		var authorEdit = new LineEdit { Text = ProjectManager.Metadata.Author, PlaceholderText = "请输入作者" };
		vbox.AddChild(new Label { Text = "作者:" }); vbox.AddChild(authorEdit);
		var descEdit = new TextEdit { Text = ProjectManager.Metadata.Description, CustomMinimumSize = new Vector2(300, 100), PlaceholderText = "请输入项目简介" };
		vbox.AddChild(new Label { Text = "简介:" }); vbox.AddChild(descEdit);
		dialog.AddChild(margin); AddChild(dialog);
		dialog.Confirmed += () => { ProjectManager.Metadata.Title = titleEdit.Text; ProjectManager.Metadata.Author = authorEdit.Text; ProjectManager.Metadata.Description = descEdit.Text; ProjectManager.SaveMetadata(); GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("项目信息已更新"); dialog.QueueFree(); };
		dialog.Canceled += () => dialog.QueueFree(); dialog.PopupCentered();
	}

	private void OnImportMenuIdPressed(long id) { if (!EnsureProjectOpen()) return; switch (id) { case 10: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Background); break; case 11: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Audio); break; case 12: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Sprite); break; } }
	private void OnCharMenuIdPressed(long id) { if (!EnsureProjectOpen()) return; switch (id) { case 0: CharacterEditorUI.Open(this); break; case 1: FileIOManager.OpenSaveDialog("导出角色配置", "characters.json", "*.json", (path) => CharacterManager.SaveCharacters(path)); break; case 2: FileIOManager.OpenLoadDialog("导入角色配置", "*.json", (path) => CharacterManager.LoadCharacters(path)); break; } }
	private void SpawnNode(BaseNodeData data, Vector2? position = null) { data.OnDeleteRequested = () => DeleteNode(data.Id); data.OnVisualEditRequested = (nodeId) => LaunchPreview(nodeId, true); GraphNode gNode = data.CreateGraphNode(_graphEdit); _nodeDataMap[gNode.Name] = data; gNode.PositionOffset = position ?? (StoryNodeManager.GetViewCenter(_graphEdit) - new Vector2(100, 50)); _graphEdit.AddChild(gNode); }
	private void DeleteNode(string nodeName) { if (_graphEdit.HasNode(nodeName)) { var node = _graphEdit.GetNode<GraphNode>(nodeName); _nodeDataMap.Remove(nodeName); node.QueueFree(); } }

	private void SetupLoadingOverlay()
	{
		_loadingOverlay = new ColorRect { Color = new Color(0, 0, 0, 0.7f), Visible = false };
		_loadingOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
		VBoxContainer vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ShrinkCenter, SizeFlagsVertical = SizeFlags.ShrinkCenter };
		_loadingOverlay.AddChild(vbox); vbox.SetAnchorsPreset(LayoutPreset.Center);
		vbox.AddChild(new Label { Text = "加载大型剧本中，请稍候...", HorizontalAlignment = HorizontalAlignment.Center });
		_loadingProgress = new ProgressBar { CustomMinimumSize = new Vector2(400, 20), MinValue = 0, MaxValue = 100, Step = 1 };
		vbox.AddChild(_loadingProgress); AddChild(_loadingOverlay);
	}

	private async void LoadAndRender(string path)
	{
		_loadingOverlay.Show(); _loadingProgress.Value = 0; _graphEdit.MouseFilter = MouseFilterEnum.Ignore;
		_graphEdit.ClearConnections(); _nodeDataMap.Clear(); foreach (Node child in _graphEdit.GetChildren()) if (child is GraphNode) child.QueueFree();
		var loadedData = await System.Threading.Tasks.Task.Run(() => StoryNodeManager.LoadProject(path));
		if (loadedData.Count == 0) { FinishLoading(); return; }
		int total = loadedData.Count; int batchSize = 30; int processed = 0;
		foreach (var data in loadedData) { Vector2 savedPos = new Vector2(data.PosX, data.PosY); if (savedPos == Vector2.Zero) savedPos = StoryNodeManager.GetViewCenter(_graphEdit); SpawnNode(data, savedPos); processed++; if (processed % batchSize == 0) { _loadingProgress.Value = (processed / (float)total) * 100; await ToSignal(GetTree(), "process_frame"); } }
		_loadingProgress.Value = 100; RebuildConnections(); FinishLoading();
	}

	private void FinishLoading() { _loadingOverlay.Hide(); _graphEdit.MouseFilter = MouseFilterEnum.Stop; }

	private void RebuildConnections()
	{
		foreach (var data in _nodeDataMap.Values)
		{
			if (!string.IsNullOrEmpty(data.NextNodeId)) _graphEdit.ConnectNode(data.Id, 0, data.NextNodeId, 0);
			if (data is ChoiceNodeData choice) { for (int i = 0; i < choice.Options.Count; i++) if (!string.IsNullOrEmpty(choice.Options[i].TargetNodeId)) _graphEdit.ConnectNode(data.Id, i, choice.Options[i].TargetNodeId, 0); }
			else if (data is BranchNodeData branch) { if (!string.IsNullOrEmpty(branch.SuccessNodeId)) _graphEdit.ConnectNode(data.Id, 0, branch.SuccessNodeId, 0); if (!string.IsNullOrEmpty(branch.FailNodeId)) _graphEdit.ConnectNode(data.Id, 1, branch.FailNodeId, 0); }
		}
	}

	private void AutoFixNodeOrder()
	{
		foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>()) if (_nodeDataMap.TryGetValue(node.Name, out var data)) data.SyncFromView(node);
		StoryNodeManager.SyncConnectionsAndPositions(_graphEdit, _nodeDataMap.Values.ToList());
		bool changed = false; bool keepChecking = true;
		while (keepChecking) { keepChecking = false; foreach (var b in _nodeDataMap.Values.ToList()) { if (b is DialogueNodeData dialogueB && !string.IsNullOrEmpty(dialogueB.NextNodeId)) { if (_nodeDataMap.TryGetValue(dialogueB.NextNodeId, out var c)) { if (c is SpriteNodeData || c is BackgroundNodeData || c is MusicNodeData) { float tempX = b.PosX; float tempY = b.PosY; b.PosX = c.PosX; b.PosY = c.PosY; c.PosX = tempX; c.PosY = tempY; foreach (var n in _nodeDataMap.Values) { if (n.Id == b.Id || n.Id == c.Id) continue; if (n.NextNodeId == b.Id) n.NextNodeId = c.Id; else if (n is ChoiceNodeData choice) foreach (var opt in choice.Options) if (opt.TargetNodeId == b.Id) opt.TargetNodeId = c.Id; else if (n is BranchNodeData branch) { if (branch.SuccessNodeId == b.Id) branch.SuccessNodeId = c.Id; if (branch.FailNodeId == b.Id) branch.FailNodeId = c.Id; } } string tempNext = c.NextNodeId; c.NextNodeId = b.Id; b.NextNodeId = tempNext; changed = true; keepChecking = true; break; } } } } }
		if (changed) { _graphEdit.ClearConnections(); foreach (var data in _nodeDataMap.Values) if (_graphEdit.HasNode(data.Id)) _graphEdit.GetNode<GraphNode>(data.Id).PositionOffset = new Vector2(data.PosX, data.PosY); RebuildConnections(); GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("自动纠正完成。"); }
		UpdateNodeWarnings();
	}

	private void UpdateNodeWarnings()
	{
		foreach (var data in _nodeDataMap.Values)
		{
			if (!_graphEdit.HasNode(data.Id)) continue;
			var gNode = _graphEdit.GetNode<GraphNode>(data.Id);
			if (data is DialogueNodeData dialogue) { bool hasIssue = false; if (!string.IsNullOrEmpty(dialogue.NextNodeId) && _nodeDataMap.TryGetValue(dialogue.NextNodeId, out var toData)) if (toData is SpriteNodeData || toData is BackgroundNodeData || toData is MusicNodeData) hasIssue = true; if (hasIssue) { gNode.Title = "⚠️ " + Tr("KEY_NODE_ACTOR"); gNode.AddThemeColorOverride("title_color", new Color(1, 0.3f, 0.3f)); } else { gNode.Title = Tr("KEY_NODE_ACTOR"); gNode.RemoveThemeColorOverride("title_color"); } }
		}
	}

	// --- 高级搜索弹窗实现 ---

	private void OpenSearchDialog()
	{
		var dialog = new AcceptDialog { Title = "高级搜索与过滤", Size = new Vector2I(600, 450) };
		var vbox = new VBoxContainer();
		dialog.AddChild(vbox);

		var hbox = new HBoxContainer();
		vbox.AddChild(hbox);

		OptionButton filterMode = new OptionButton();
		filterMode.AddItem("全部内容", 0);
		filterMode.AddItem("资源引用", 1);
		filterMode.AddItem("数值变更/判定", 2);
		hbox.AddChild(filterMode);

		LineEdit queryInput = new LineEdit { PlaceholderText = "输入关键词...", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		hbox.AddChild(queryInput);

		Button searchBtn = new Button { Text = "搜索" };
		hbox.AddChild(searchBtn);

		ItemList resultsList = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill };
		vbox.AddChild(resultsList);

		var matchIds = new List<string>();

		Action doSearch = async () => {
			string query = queryInput.Text.Trim();
			if (string.IsNullOrEmpty(query)) return;
			resultsList.Clear(); matchIds.Clear();
			
			foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>()) if (_nodeDataMap.TryGetValue(node.Name, out var data)) data.SyncFromView(node);
			var nodeSnapshot = _nodeDataMap.Values.ToList();
			int mode = filterMode.Selected;

			var results = await System.Threading.Tasks.Task.Run(() => {
				var found = new List<(string id, string preview)>();
				foreach (var data in nodeSnapshot) {
					bool match = false; string preview = "";
					if (mode == 0 || mode == 1) { // 内容或资源
						if (data is DialogueNodeData d) { if (d.Content.Contains(query, StringComparison.OrdinalIgnoreCase)) { match = true; preview = $"[对话] {Truncate(d.Content)}"; } }
						else if (data is NarrativeNodeData n) { if (n.Content.Contains(query, StringComparison.OrdinalIgnoreCase)) { match = true; preview = $"[叙述] {Truncate(n.Content)}"; } }
						else if (data is ChoiceNodeData c) { foreach (var o in c.Options) if (o.Text.Contains(query, StringComparison.OrdinalIgnoreCase)) { match = true; preview = $"[选项] {Truncate(o.Text)}"; break; } }
					}
					if (!match && (mode == 0 || mode == 1)) { // 资源过滤
						if (data is BackgroundNodeData bg && bg.BackgroundFile.Contains(query, StringComparison.OrdinalIgnoreCase)) { match = true; preview = $"[背景] {bg.BackgroundFile}"; }
						else if (data is MusicNodeData m && m.AudioFile.Contains(query, StringComparison.OrdinalIgnoreCase)) { match = true; preview = $"[音频] {m.AudioFile}"; }
						else if (data is SpriteNodeData s) { var charName = CharacterManager.Characters.Find(c => c.Id == s.CharacterId)?.Name ?? ""; if (charName.Contains(query, StringComparison.OrdinalIgnoreCase)) { match = true; preview = $"[立绘] {charName} ({s.ActionType})"; } }
					}
					if (!match && (mode == 0 || mode == 2)) { // 数值过滤
						if (data is ValueNodeData v && v.TargetAttribute.Contains(query, StringComparison.OrdinalIgnoreCase)) { match = true; preview = $"[数值] {v.TargetAttribute} = {v.ChangeValue}"; }
						else if (data is BranchNodeData b && b.VariableId.Contains(query, StringComparison.OrdinalIgnoreCase)) { match = true; preview = $"[判定] {b.VariableId} >= {b.ComparisonValue}"; }
					}
					if (match) found.Add((data.Id, preview));
				}
				return found;
			});

			foreach (var res in results) { resultsList.AddItem(res.preview); matchIds.Add(res.id); }
			if (results.Count == 0) resultsList.AddItem("未找到匹配项");
		};

		searchBtn.Pressed += () => doSearch();
		queryInput.TextSubmitted += (t) => doSearch();
		
		resultsList.ItemSelected += (idx) => {
			if (idx < matchIds.Count) {
				FocusOnNode(matchIds[(int)idx]);
				// 不自动关闭弹窗，方便连续查看，但如果用户希望点击即关闭可启用下行
				// dialog.Hide(); 
			}
		};

		AddChild(dialog); dialog.PopupCentered();
		dialog.VisibilityChanged += () => { if (!dialog.Visible) { ClearHighlights(); dialog.QueueFree(); } };
	}

	private string Truncate(string text, int length = 20) => text.Length > length ? text.Substring(0, length) + "..." : text;

	private void FocusOnNode(string nodeId)
	{
		ClearHighlights();
		if (_graphEdit.HasNode(nodeId))
		{
			var node = _graphEdit.GetNode<GraphNode>(nodeId);
			node.AddThemeColorOverride("title_color", new Color(1, 0.5f, 0));
			Vector2 targetPos = node.PositionOffset + node.Size / 2;
			_graphEdit.ScrollOffset = targetPos * _graphEdit.Zoom - _graphEdit.Size / 2;
			foreach (var child in _graphEdit.GetChildren().OfType<GraphNode>()) child.Selected = false;
			node.Selected = true;
		}
	}

	private void ClearHighlights()
	{
		foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>()) node.RemoveThemeColorOverride("title_color");
		UpdateNodeWarnings();
	}
}
