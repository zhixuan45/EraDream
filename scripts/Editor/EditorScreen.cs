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

	// 搜索相关
	private LineEdit _searchEdit;
	private Label _searchStatus;
	private List<string> _searchResults = new List<string>();
	private int _currentSearchResultIndex = -1;

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
		SetupSearchUI();

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
		// 清理原有的所有子节点
		foreach (Node child in sidePanel.GetChildren()) child.QueueFree();

		var rootVBox = new VBoxContainer { Name = "RootVBox" };
		rootVBox.AddThemeConstantOverride("separation", 10);
		sidePanel.AddChild(rootVBox);

		Label titleLabel = new Label { 
			Text = Tr("KEY_EDITOR_TITLE"), 
			HorizontalAlignment = HorizontalAlignment.Center 
		};
		titleLabel.AddThemeFontSizeOverride("font_size", 24);
		rootVBox.AddChild(titleLabel);
		rootVBox.AddChild(new HSeparator());
		// 滚动容器 (核心区域)
		ScrollContainer scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		rootVBox.AddChild(scroll);

		_sideScrollVBox = new VBoxContainer { Name = "ScrollVBox", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_sideScrollVBox.AddThemeConstantOverride("separation", 12);
		scroll.AddChild(_sideScrollVBox);

		// 底部固定区域
		rootVBox.AddChild(new HSeparator());
		_btnReturn = new Button { Text = Tr("KEY_RETURN_TO_MENU") };
		_btnReturn.Pressed += () => {
			LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
			GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
		};		rootVBox.AddChild(_btnReturn);
	}

	private void CreateCollapsibleCategory(string title, out VBoxContainer contentContainer)
	{
		var categoryRoot = new VBoxContainer { Name = title + "Category" };
		_sideScrollVBox.AddChild(categoryRoot);

		Button headerBtn = new Button { 
			Text = "▼ " + title, 
			Alignment = HorizontalAlignment.Left,
			ThemeTypeVariation = "HeaderSmall",
			Flat = true
		};
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

	private void SetupSearchUI()
	{
		var searchContainer = new VBoxContainer { Name = "SearchContainer" };
		_sideScrollVBox.AddChild(searchContainer);
		// 搜索通常放在最上面
		_sideScrollVBox.MoveChild(searchContainer, 0);

		searchContainer.AddChild(new Label { Text = "搜索节点内容:", ThemeTypeVariation = "HeaderSmall" });

		var inputHBox = new HBoxContainer();
		searchContainer.AddChild(inputHBox);

		_searchEdit = new LineEdit { 
			PlaceholderText = "输入关键词...", 
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill 
		};
		_searchEdit.TextSubmitted += (text) => SearchNodes(text);
		inputHBox.AddChild(_searchEdit);

		var searchBtn = new Button { Text = "🔍" };
		searchBtn.Pressed += () => SearchNodes(_searchEdit.Text);
		inputHBox.AddChild(searchBtn);

		var searchButtons = new HBoxContainer();
		searchContainer.AddChild(searchButtons);
		var prevBtn = new Button { Text = " ↑ ", TooltipText = "上一个结果" };
		prevBtn.Pressed += GoToPreviousResult;
		searchButtons.AddChild(prevBtn);

		var nextBtn = new Button { Text = " ↓ ", TooltipText = "下一个结果" };
		nextBtn.Pressed += GoToNextResult;
		searchButtons.AddChild(nextBtn);

		var clearBtn = new Button { Text = "清除", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		clearBtn.Pressed += ClearSearch;
		searchButtons.AddChild(clearBtn);

		_searchStatus = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		_searchStatus.AddThemeFontSizeOverride("font_size", 12);
		searchContainer.AddChild(_searchStatus);

		searchContainer.AddChild(new HSeparator());
	}

	private void ConnectNodesUndoable(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		_graphEdit.ConnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
		if (_nodeDataMap.TryGetValue(fromNode, out var fromData) && _nodeDataMap.TryGetValue(toNode, out var toData))
		{
			if (fromData is ChoiceNodeData c)
			{
				if (fromPort < c.Options.Count) c.Options[(int)fromPort].TargetNodeId = toData.Id;
			}
			else if (fromData is BranchNodeData b)
			{
				if (fromPort == 0) b.SuccessNodeId = toData.Id;
				else if (fromPort == 1) b.FailNodeId = toData.Id;
			}
			else
			{
				fromData.NextNodeId = toData.Id;
			}
			
			UpdateNodeWarnings();
			
			if (fromData is DialogueNodeData && (toData is SpriteNodeData || toData is BackgroundNodeData || toData is MusicNodeData))
			{
				GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("⚠️ 警告：该对话节点已标红，建议将视觉/音频节点置于其前。");
			}
		}
	}

	private void DisconnectNodesUndoable(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		_graphEdit.DisconnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
		if (_nodeDataMap.TryGetValue(fromNode, out var fromData))
		{
			if (fromData is ChoiceNodeData c)
			{
				if (fromPort < c.Options.Count) c.Options[(int)fromPort].TargetNodeId = null;
			}
			else if (fromData is BranchNodeData b)
			{
				if (fromPort == 0) b.SuccessNodeId = null;
				else if (fromPort == 1) b.FailNodeId = null;
			}
			else
			{
				fromData.NextNodeId = null;
			}
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

	private void SpawnNodeUndoable(BaseNodeData data, Vector2 pos)
	{
		SpawnNode(data, pos);
	}

	private bool EnsureProjectOpen()
	{
		if (!ProjectManager.IsProjectOpened)
		{
			GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("请先新建或打开一个项目！");
			return false;
		}
		return true;
	}

	private void LaunchPreview(string startNodeId = null, bool isEditMode = false)
	{
		if (!EnsureProjectOpen()) return;

		// 1. 同步所有节点视图内部数据
		foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>())
		{
			if (_nodeDataMap.TryGetValue(node.Name, out var data)) data.SyncFromView(node);
		}
		// 2. 同步连接关系和坐标
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
		
		GetNode<Button>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer/BtnSave").Hide();
		GetNode<Button>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer/BtnLoad").Hide();
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
	}

	private void OnFileMenuIdPressed(long id)
	{
		switch (id)
		{
			case 0:
				FileIOManager.OpenFolderDialog("选择新项目文件夹", (path) => {
					ProjectManager.CreateNewProject(path);
					LoadAndRender(ProjectManager.StoryFile);
				});
				break;
			case 1:
				FileIOManager.OpenLoadDialog("选择项目文件", "*.uma", (path) => {
					if (ProjectManager.OpenProject(path))
					{
						CharacterManager.LoadCharacters(ProjectManager.CharacterFile);
						LoadAndRender(ProjectManager.StoryFile);
					}
				});
				break;
			case 2:
				if (EnsureProjectOpen())
				{
					StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile);
					CharacterManager.SaveCharacters(ProjectManager.CharacterFile);
					ProjectManager.SaveMetadata();
					GD.Print("Project Saved Successfully.");
					GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("项目保存成功！");
				}
				break;
		}
	}

	private void OnEditMenuIdPressed(long id)
	{
		switch (id)
		{
			case 0: _undoRedo.Undo(); break;
			case 1: _undoRedo.Redo(); break;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.Z && !keyEvent.ShiftPressed)
			{
				_undoRedo.Undo();
				GetViewport().SetInputAsHandled();
			}
			else if (keyEvent.CtrlPressed && (keyEvent.Keycode == Key.Y || (keyEvent.Keycode == Key.Z && keyEvent.ShiftPressed)))
			{
				_undoRedo.Redo();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void OnProjectMenuIdPressed(long id)
	{
		if (!EnsureProjectOpen()) return;
		switch (id)
		{
			case 0: ShowProjectInfoDialog(); break;
			case 1:
				StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile);
				CharacterManager.SaveCharacters(ProjectManager.CharacterFile);
				ProjectManager.SaveMetadata();
				FileIOManager.OpenSaveDialog("导出剧情包", $"{ProjectManager.Metadata.Title}.era", "*.era", (path) => {
					ProjectManager.ExportAsEra(path);
				});
				break;
			case 2:
				StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile);
				CharacterManager.SaveCharacters(ProjectManager.CharacterFile);
				ProjectManager.SaveMetadata();
				FileIOManager.OpenSaveDialog("导出项目压缩包", $"{ProjectManager.Metadata.Title}.zip", "*.zip", (path) => {
					ProjectManager.ExportProject(path);
				});
				break;
		}
	}

	private void ShowProjectInfoDialog()
	{
		var dialog = new AcceptDialog { Title = "项目信息" };
		var margin = new MarginContainer { CustomMinimumSize = new Vector2(400, 300) };
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		margin.AddThemeConstantOverride("margin_left", 15);
		margin.AddThemeConstantOverride("margin_right", 15);
		
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);
		margin.AddChild(vbox);
		
		vbox.AddChild(new Label { Text = "修改项目基本信息", HorizontalAlignment = HorizontalAlignment.Center });
		vbox.AddChild(new HSeparator());
		
		var titleEdit = new LineEdit { Text = ProjectManager.Metadata.Title, PlaceholderText = "请输入项目标题" };
		vbox.AddChild(new Label { Text = "标题:" });
		vbox.AddChild(titleEdit);

		var authorEdit = new LineEdit { Text = ProjectManager.Metadata.Author, PlaceholderText = "请输入作者" };
		vbox.AddChild(new Label { Text = "作者:" });
		vbox.AddChild(authorEdit);

		var descEdit = new TextEdit { Text = ProjectManager.Metadata.Description, CustomMinimumSize = new Vector2(300, 100), PlaceholderText = "请输入项目简介" };
		vbox.AddChild(new Label { Text = "简介:" });
		vbox.AddChild(descEdit);

		dialog.AddChild(margin);
		AddChild(dialog);

		dialog.Confirmed += () => {
			ProjectManager.Metadata.Title = titleEdit.Text;
			ProjectManager.Metadata.Author = authorEdit.Text;
			ProjectManager.Metadata.Description = descEdit.Text;
			ProjectManager.SaveMetadata();
			GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("项目信息已更新");
			dialog.QueueFree();
		};
		dialog.Canceled += () => dialog.QueueFree();
		
		dialog.PopupCentered();
	}

	private void OnImportMenuIdPressed(long id)
	{
		if (!EnsureProjectOpen()) return;
		switch (id)
		{
			case 10: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Background); break;
			case 11: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Audio); break;
			case 12: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Sprite); break;
		}
	}

	private void OnCharMenuIdPressed(long id)
	{
		if (!EnsureProjectOpen()) return;
		switch (id)
		{
			case 0: CharacterEditorUI.Open(this); break;
			case 1: FileIOManager.OpenSaveDialog("导出角色配置", "characters.json", "*.json", (path) => CharacterManager.SaveCharacters(path)); break;
			case 2: FileIOManager.OpenLoadDialog("导入角色配置", "*.json", (path) => CharacterManager.LoadCharacters(path)); break;
		}
	}

	private void SpawnNode(BaseNodeData data, Vector2? position = null)
	{
		data.OnDeleteRequested = () => DeleteNode(data.Id);
		data.OnVisualEditRequested = (nodeId) => LaunchPreview(nodeId, true);
		GraphNode gNode = data.CreateGraphNode(_graphEdit);
		_nodeDataMap[gNode.Name] = data; 
		gNode.PositionOffset = position ?? (StoryNodeManager.GetViewCenter(_graphEdit) - new Vector2(100, 50));
		_graphEdit.AddChild(gNode);
	}

	private void DeleteNode(string nodeName)
	{
		if (_graphEdit.HasNode(nodeName))
		{
			var node = _graphEdit.GetNode<GraphNode>(nodeName);
			_nodeDataMap.Remove(nodeName);
			node.QueueFree();
		}
	}

	private void SetupLoadingOverlay()
	{
		_loadingOverlay = new ColorRect { Color = new Color(0, 0, 0, 0.7f), Visible = false };
		_loadingOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
		
		VBoxContainer vbox = new VBoxContainer {
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			SizeFlagsVertical = SizeFlags.ShrinkCenter
		};
		_loadingOverlay.AddChild(vbox);
		vbox.SetAnchorsPreset(LayoutPreset.Center);

		Label lbl = new Label { Text = "加载大型剧本中，请稍候...", HorizontalAlignment = HorizontalAlignment.Center };
		vbox.AddChild(lbl);

		_loadingProgress = new ProgressBar {
			CustomMinimumSize = new Vector2(400, 20),
			MinValue = 0, MaxValue = 100, Step = 1
		};
		vbox.AddChild(_loadingProgress);

		AddChild(_loadingOverlay);
	}

	private async void LoadAndRender(string path)
	{
		_loadingOverlay.Show();
		_loadingProgress.Value = 0;
		_graphEdit.MouseFilter = MouseFilterEnum.Ignore;
		
		_graphEdit.ClearConnections();
		_nodeDataMap.Clear();
		foreach (Node child in _graphEdit.GetChildren()) if (child is GraphNode) child.QueueFree();

		var loadedData = await System.Threading.Tasks.Task.Run(() => StoryNodeManager.LoadProject(path));
		
		if (loadedData.Count == 0) { FinishLoading(); return; }

		int total = loadedData.Count;
		int batchSize = 30;
		int processed = 0;

		foreach (var data in loadedData)
		{
			Vector2 savedPos = new Vector2(data.PosX, data.PosY);
			if (savedPos == Vector2.Zero) savedPos = StoryNodeManager.GetViewCenter(_graphEdit);
			SpawnNode(data, savedPos);

			processed++;
			if (processed % batchSize == 0)
			{
				_loadingProgress.Value = (processed / (float)total) * 100;
				await ToSignal(GetTree(), "process_frame");
			}
		}

		_loadingProgress.Value = 100;
		RebuildConnections();
		FinishLoading();
	}

	private void FinishLoading()
	{
		_loadingOverlay.Hide();
		_graphEdit.MouseFilter = MouseFilterEnum.Stop;
	}

	private void RebuildConnections()
	{
		foreach (var data in _nodeDataMap.Values)
		{
			if (!string.IsNullOrEmpty(data.NextNodeId))
			{
				_graphEdit.ConnectNode(data.Id, 0, data.NextNodeId, 0);
				if (data is DialogueNodeData && _nodeDataMap.TryGetValue(data.NextNodeId, out var toData))
				{
					if (toData is SpriteNodeData || toData is BackgroundNodeData || toData is MusicNodeData)
						_graphEdit.SetConnectionActivity(data.Id, 0, data.NextNodeId, 0, 1.0f);
				}
			}

			if (data is ChoiceNodeData choice)
			{
				for (int i = 0; i < choice.Options.Count; i++)
					if (!string.IsNullOrEmpty(choice.Options[i].TargetNodeId))
						_graphEdit.ConnectNode(data.Id, i, choice.Options[i].TargetNodeId, 0);
			}
			else if (data is BranchNodeData branch)
			{
				if (!string.IsNullOrEmpty(branch.SuccessNodeId)) _graphEdit.ConnectNode(data.Id, 0, branch.SuccessNodeId, 0);
				if (!string.IsNullOrEmpty(branch.FailNodeId)) _graphEdit.ConnectNode(data.Id, 1, branch.FailNodeId, 0);
			}
		}
	}

	private void AutoFixNodeOrder()
	{
		foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>())
			if (_nodeDataMap.TryGetValue(node.Name, out var data)) data.SyncFromView(node);
		
		StoryNodeManager.SyncConnectionsAndPositions(_graphEdit, _nodeDataMap.Values.ToList());

		bool changed = false;
		bool keepChecking = true;
		
		while (keepChecking)
		{
			keepChecking = false;
			foreach (var b in _nodeDataMap.Values.ToList())
			{
				if (b is DialogueNodeData dialogueB && !string.IsNullOrEmpty(dialogueB.NextNodeId))
				{
					if (_nodeDataMap.TryGetValue(dialogueB.NextNodeId, out var c))
					{
						if (c is SpriteNodeData || c is BackgroundNodeData || c is MusicNodeData)
						{
							float tempX = b.PosX; float tempY = b.PosY;
							b.PosX = c.PosX; b.PosY = c.PosY;
							c.PosX = tempX; c.PosY = tempY;

							foreach (var n in _nodeDataMap.Values)
							{
								if (n.Id == b.Id || n.Id == c.Id) continue;
								if (n.NextNodeId == b.Id) n.NextNodeId = c.Id;
								else if (n is ChoiceNodeData choice)
									foreach (var opt in choice.Options) if (opt.TargetNodeId == b.Id) opt.TargetNodeId = c.Id;
								else if (n is BranchNodeData branch)
								{
									if (branch.SuccessNodeId == b.Id) branch.SuccessNodeId = c.Id;
									if (branch.FailNodeId == b.Id) branch.FailNodeId = c.Id;
								}
							}

							string tempNext = c.NextNodeId;
							c.NextNodeId = b.Id;
							b.NextNodeId = tempNext;

							changed = true;
							keepChecking = true;
							break; 
						}
					}
				}
			}
		}

		if (changed)
		{
			_graphEdit.ClearConnections();
			foreach (var data in _nodeDataMap.Values)
				if (_graphEdit.HasNode(data.Id))
					_graphEdit.GetNode<GraphNode>(data.Id).PositionOffset = new Vector2(data.PosX, data.PosY);
			RebuildConnections();
			GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("自动纠正完成。");
		}
		UpdateNodeWarnings();
	}

	private void UpdateNodeWarnings()
	{
		foreach (var data in _nodeDataMap.Values)
		{
			if (!_graphEdit.HasNode(data.Id)) continue;
			var gNode = _graphEdit.GetNode<GraphNode>(data.Id);
			
			if (data is DialogueNodeData dialogue)
			{
				bool hasIssue = false;
				if (!string.IsNullOrEmpty(dialogue.NextNodeId) && _nodeDataMap.TryGetValue(dialogue.NextNodeId, out var toData))
					if (toData is SpriteNodeData || toData is BackgroundNodeData || toData is MusicNodeData)
						hasIssue = true;

				if (hasIssue)
				{
					gNode.Title = "⚠️ " + Tr("KEY_NODE_ACTOR");
					gNode.AddThemeColorOverride("title_color", new Color(1, 0.3f, 0.3f));
				}
				else
				{
					gNode.Title = Tr("KEY_NODE_ACTOR");
					gNode.RemoveThemeColorOverride("title_color");
				}
			}
		}
	}

	private Dictionary<string, int> CalculateDistancesFromStart()
	{
		var distances = new Dictionary<string, int>();
		var queue = new Queue<string>();

		var startNodes = _nodeDataMap.Values.Where(n => n is StartNodeData).Select(n => n.Id).ToList();
		if (startNodes.Count == 0 && _nodeDataMap.Count > 0)
			startNodes.Add(_nodeDataMap.Values.First().Id);

		foreach (var startId in startNodes)
		{
			if (!distances.ContainsKey(startId))
			{
				distances[startId] = 0;
				queue.Enqueue(startId);
			}
		}

		while (queue.Count > 0)
		{
			var currentId = queue.Dequeue();
			int currentDist = distances[currentId];
			if (!_nodeDataMap.TryGetValue(currentId, out var nodeData)) continue;

			var nextNodes = new List<string>();
			if (nodeData is ChoiceNodeData choice)
				nextNodes.AddRange(choice.Options.Select(o => o.TargetNodeId));
			else if (nodeData is BranchNodeData branch)
			{
				nextNodes.Add(branch.SuccessNodeId);
				nextNodes.Add(branch.FailNodeId);
			}
			else nextNodes.Add(nodeData.NextNodeId);

			foreach (var nextId in nextNodes)
			{
				if (!string.IsNullOrEmpty(nextId) && !distances.ContainsKey(nextId))
				{
					distances[nextId] = currentDist + 1;
					queue.Enqueue(nextId);
				}
			}
		}
		return distances;
	}

	private void SearchNodes(string query)
	{
		if (string.IsNullOrWhiteSpace(query)) { ClearSearch(); return; }

		foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>())
			if (_nodeDataMap.TryGetValue(node.Name, out var data)) data.SyncFromView(node);
		
		var distances = CalculateDistancesFromStart();

		_searchResults.Clear();
		foreach (var pair in _nodeDataMap)
		{
			bool match = false;
			if (pair.Value is DialogueNodeData d) match = d.Content.Contains(query, StringComparison.OrdinalIgnoreCase);
			else if (pair.Value is NarrativeNodeData n) match = n.Content.Contains(query, StringComparison.OrdinalIgnoreCase);
			else if (pair.Value is ChoiceNodeData c) match = c.Options.Any(o => o.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
			if (match) _searchResults.Add(pair.Key);
		}

		_searchResults = _searchResults.OrderBy(id => distances.ContainsKey(id) ? distances[id] : int.MaxValue).ToList();

		if (_searchResults.Count > 0)
		{
			_currentSearchResultIndex = 0;
			ApplySearchHighlights();
			FocusOnSearchResult();
		}
		else
		{
			_currentSearchResultIndex = -1;
			_searchStatus.Text = "未找到匹配项";
			ClearSearchHighlights();
		}
	}

	private void ApplySearchHighlights()
	{
		ClearSearchHighlights();
		foreach (var id in _searchResults)
			if (_graphEdit.HasNode(id))
				_graphEdit.GetNode<GraphNode>(id).AddThemeColorOverride("title_color", new Color(1, 1, 0));
	}

	private void ClearSearchHighlights()
	{
		foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>())
			node.RemoveThemeColorOverride("title_color");
		UpdateNodeWarnings();
	}

	private void FocusOnSearchResult()
	{
		if (_currentSearchResultIndex < 0 || _currentSearchResultIndex >= _searchResults.Count) return;
		ApplySearchHighlights();

		string nodeId = _searchResults[_currentSearchResultIndex];
		if (_graphEdit.HasNode(nodeId))
		{
			var node = _graphEdit.GetNode<GraphNode>(nodeId);
			node.AddThemeColorOverride("title_color", new Color(1, 0.5f, 0));
			Vector2 targetPos = node.PositionOffset + node.Size / 2;
			_graphEdit.ScrollOffset = targetPos * _graphEdit.Zoom - _graphEdit.Size / 2;

			foreach (var child in _graphEdit.GetChildren().OfType<GraphNode>()) child.Selected = false;
			node.Selected = true;
			_searchStatus.Text = $"{_currentSearchResultIndex + 1} / {_searchResults.Count} 个匹配";
		}
	}

	private void GoToNextResult()
	{
		if (_searchResults.Count == 0) return;
		_currentSearchResultIndex = (_currentSearchResultIndex + 1) % _searchResults.Count;
		FocusOnSearchResult();
	}

	private void GoToPreviousResult()
	{
		if (_searchResults.Count == 0) return;
		_currentSearchResultIndex = (_currentSearchResultIndex - 1 + _searchResults.Count) % _searchResults.Count;
		FocusOnSearchResult();
	}

	private void ClearSearch()
	{
		_searchResults.Clear();
		_currentSearchResultIndex = -1;
		_searchStatus.Text = "";
		_searchEdit.Text = "";
		ClearSearchHighlights();
	}
}
