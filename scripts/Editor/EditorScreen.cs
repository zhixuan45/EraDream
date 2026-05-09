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
	private UmaEraArchive.Core.CommandHistory _cmdHistory = new();

	private ColorRect _loadingOverlay;
	private ProgressBar _loadingProgress;

	// 侧边栏布局组件
	private VBoxContainer _sideScrollVBox;
	private Button _btnReturn;

	private void SetupLayout()
	{
		var oldHSplit = GetNode<HSplitContainer>("HSplitContainer");
		RemoveChild(oldHSplit);

		// 必须使用 MarginContainer 才能使 margin_left/right 生效
		MarginContainer safeArea = new MarginContainer { Name = "SafeAreaContainer" };
		safeArea.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(safeArea);

		VBoxContainer mainVBox = new VBoxContainer { Name = "VBoxContainer" };
		mainVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		mainVBox.SizeFlagsVertical = SizeFlags.ExpandFill;
		safeArea.AddChild(mainVBox);

		_menuBar = new MenuBar { CustomMinimumSize = new Vector2(0, 30) };
		mainVBox.AddChild(_menuBar);
		SetupMenus();

		mainVBox.AddChild(oldHSplit);
		oldHSplit.SizeFlagsVertical = SizeFlags.ExpandFill;

		// 监听安全区变化并应用到 MarginContainer
		if (SettingsManager.Instance != null)
		{
			SettingsManager.Instance.OnSafeAreaPaddingChanged += (p) => {
				safeArea.AddThemeConstantOverride("margin_left", (int)p);
				safeArea.AddThemeConstantOverride("margin_right", (int)p);
				safeArea.AddThemeConstantOverride("margin_top", (int)p);
				safeArea.AddThemeConstantOverride("margin_bottom", (int)p);
			};
			int pad = (int)SettingsManager.Instance.SafeAreaPadding;
			safeArea.AddThemeConstantOverride("margin_left", pad);
			safeArea.AddThemeConstantOverride("margin_right", pad);
			safeArea.AddThemeConstantOverride("margin_top", pad);
			safeArea.AddThemeConstantOverride("margin_bottom", pad);
		}
	}

	public override void _Ready()
	{
		SetupLayout();
		// 同步更新路径：加上 SafeAreaContainer 层级
		_graphEdit = GetNode<GraphEdit>("SafeAreaContainer/VBoxContainer/HSplitContainer/GraphEdit");
		_graphEdit.AddThemeColorOverride("activity_color", new Color(1, 0.2f, 0.2f)); 
		
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
		AddCategoryButton(assetCategory, "添加贴纸节点", () => {
			if (EnsureProjectOpen()) SpawnNodeWithUndo(new StickerNodeData());
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

		// 基础图表信号（使用纯 C# CommandHistory 替代 Godot UndoRedo）
		_graphEdit.ConnectionRequest += (f, fp, t, tp) => {
			_cmdHistory.Execute(
				() => ConnectNodesUndoable(f, fp, t, tp),
				() => DisconnectNodesUndoable(f, fp, t, tp)
			);
		};
		_graphEdit.DisconnectionRequest += (f, fp, t, tp) => {
			if (!EnsureProjectOpen()) return;
			_cmdHistory.Execute(
				() => DisconnectNodesUndoable(f, fp, t, tp),
				() => ConnectNodesUndoable(f, fp, t, tp)
			);
		};
		_graphEdit.DeleteNodesRequest += (nodes) => {
			if (!EnsureProjectOpen()) return;
			_cmdHistory.BeginBatch();
			foreach (string n in nodes)
			{
				if (_nodeDataMap.TryGetValue(n, out var data))
				{
					Vector2 pos = _graphEdit.GetNode<GraphNode>(n).PositionOffset;
					_cmdHistory.AddBatchStep(() => DeleteNode(n), () => SpawnNodeAt(data, pos));
				}
			}
			_cmdHistory.CommitBatch();
		};
	}

	private void SetupSidePanelStructure()
	{
		var sidePanel = GetNode<PanelContainer>("SafeAreaContainer/VBoxContainer/HSplitContainer/SidePanel");
		foreach (Node child in sidePanel.GetChildren()) child.QueueFree();

		var rootVBox = new VBoxContainer { Name = "RootVBox" };
		rootVBox.AddThemeConstantOverride("separation", 10);
		sidePanel.AddChild(rootVBox);

		var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		rootVBox.AddChild(scroll);

		_sideScrollVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		scroll.AddChild(_sideScrollVBox);

		_btnReturn = new Button { Text = "返回主菜单", CustomMinimumSize = new Vector2(0, 40) };
		_btnReturn.Pressed += () => {
			LoadingScreen.TargetScene = "res://scenes/UI/MainMenuScreen.tscn";
			GetTree().ChangeSceneToFile("res://scenes/UI/LoadingScreen.tscn");
		};
		rootVBox.AddChild(_btnReturn);
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
		charMenu.AddItem("新建角色", 0);
		charMenu.AddItem("角色列表管理", 3);
		charMenu.AddSeparator();
		charMenu.AddItem("导出角色配置", 1);
		charMenu.AddItem("导入角色配置", 2);
		_menuBar.AddChild(charMenu);
		_menuBar.SetMenuTitle(3, "角色");
		charMenu.IdPressed += OnCharMenuIdPressed;

		// 贴纸菜单
		PopupMenu stickerMenu = new PopupMenu { Name = "Sticker" };
		stickerMenu.AddItem("新建贴纸", 0);
		stickerMenu.AddItem("贴纸列表管理", 1);
		_menuBar.AddChild(stickerMenu);
		_menuBar.SetMenuTitle(4, "贴纸");
		stickerMenu.IdPressed += OnStickerMenuIdPressed;

		PopupMenu searchMenu = new PopupMenu { Name = "Search" };
		searchMenu.AddItem("全局搜索 (Ctrl+F)", 0);
		_menuBar.AddChild(searchMenu);
		_menuBar.SetMenuTitle(5, "搜索");
		searchMenu.IdPressed += (id) => OpenSearchDialog();
	}

	private void OnFileMenuIdPressed(long id)
	{
		switch (id)
		{
			case 0: FileIOManager.OpenFolderDialog("选择新项目文件夹", (path) => { ProjectManager.CreateNewProject(path); LoadAndRender(ProjectManager.StoryFile); }); break;
			case 1: FileIOManager.OpenLoadDialog("选择项目文件", "*.uma", (path) => { if (ProjectManager.OpenProject(path)) { CharacterManager.LoadCharacters(ProjectManager.CharacterFile); StickerManager.LoadStickers(ProjectManager.StickerFile); LoadAndRender(ProjectManager.StoryFile); } }); break;
			case 2: if (EnsureProjectOpen()) { StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile); CharacterManager.SaveCharacters(ProjectManager.CharacterFile); StickerManager.SaveStickers(ProjectManager.StickerFile); ProjectManager.SaveMetadata(); GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("项目保存成功！"); } break;
		}
	}

	private void OnEditMenuIdPressed(long id) { switch (id) { case 0: _cmdHistory.Undo(); break; case 1: _cmdHistory.Redo(); break; } }

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.Z && !keyEvent.ShiftPressed) { _cmdHistory.Undo(); GetViewport().SetInputAsHandled(); }
			else if (keyEvent.CtrlPressed && (keyEvent.Keycode == Key.Y || (keyEvent.Keycode == Key.Z && keyEvent.ShiftPressed))) { _cmdHistory.Redo(); GetViewport().SetInputAsHandled(); }
			else if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.F) { OpenSearchDialog(); GetViewport().SetInputAsHandled(); }
		}
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

	private void OnProjectMenuIdPressed(long id)
	{
		if (!EnsureProjectOpen()) return;
		switch (id)
		{
			case 0: ShowProjectMetadataDialog(); break;
			case 1: FileIOManager.OpenSaveDialog("导出剧情包", "project.era", "*.era", (path) => ProjectManager.ExportAsEra(path)); break;
			case 2: FileIOManager.OpenSaveDialog("导出项目压缩包", "project.zip", "*.zip", (path) => ProjectManager.ExportProject(path)); break;
		}
	}

	private void OnCharMenuIdPressed(long id)
	{
		if (!EnsureProjectOpen()) return;
		switch (id)
		{
			// 新建角色和角色列表管理都跳转到同一个界面
			case 0:
			case 3: CharacterEditorUI.Open(this); break;
			case 1: // 导出角色配置
				FileIOManager.OpenSaveDialog("导出角色配置", "characters.json", "*.json", (path) => {
					CharacterManager.SaveCharacters(path);
					GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("角色配置导出成功！");
				}); break;
			case 2: // 导入角色配置
				FileIOManager.OpenLoadDialog("导入角色配置", "*.json", (path) => {
					CharacterManager.LoadCharacters(path);
					GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("角色配置导入成功！");
				}); break;
		}
	}

	// 贴纸菜单回调：新建贴纸和贴纸列表管理都跳转到同一个界面
	private void OnStickerMenuIdPressed(long id)
	{
		if (!EnsureProjectOpen()) return;
		StickerEditorUI.Open(this);
	}

	private bool EnsureProjectOpen()
	{
		if (!ProjectManager.IsProjectOpened) { GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("请先新建或打开一个项目！"); return false; }
		return true;
	}

	private void SpawnNodeWithUndo(BaseNodeData data)
	{
		Vector2 spawnPos = StoryNodeManager.GetViewCenter(_graphEdit);
		_cmdHistory.Execute(
			() => SpawnNodeAt(data, spawnPos),
			() => DeleteNode(data.Id)
		);
	}

	private void SpawnNodeAt(BaseNodeData data, Vector2 pos)
	{
		data.PosX = pos.X; data.PosY = pos.Y;
		_nodeDataMap[data.Id] = data;
		var visualNode = data.CreateGraphNode(_graphEdit);
		_graphEdit.AddChild(visualNode);
		visualNode.PositionOffset = pos;
		BindNodeCallbacks(data, pos);
	}

	/// <summary>
	/// 统一绑定节点回调（删除、可视化编辑），避免加载后的节点无法操作
	/// </summary>
	private void BindNodeCallbacks(BaseNodeData data, Vector2 pos)
	{
		data.OnDeleteRequested = () => {
			_cmdHistory.Execute(
				() => DeleteNode(data.Id),
				() => SpawnNodeAt(data, pos)
			);
		};
		// 可视化编辑回调: 以编辑模式启动预览
		data.OnVisualEditRequested = (nodeId) => {
			StoryNodeManager.SyncConnectionsAndPositions(_graphEdit, _nodeDataMap.Values.ToList());
			LaunchPreview(nodeId, true);
		};
	}

	private void LoadAndRender(string path)
	{
		foreach (Node child in _graphEdit.GetChildren()) if (child is GraphNode) child.QueueFree();
		_nodeDataMap.Clear();
		var nodes = StoryNodeManager.LoadProject(path);
		foreach (var node in nodes)
		{
			_nodeDataMap[node.Id] = node;
			var v = node.CreateGraphNode(_graphEdit);
			_graphEdit.AddChild(v);
			v.PositionOffset = new Vector2(node.PosX, node.PosY);
			// 加载的节点也必须绑定删除和可视化编辑回调
			BindNodeCallbacks(node, v.PositionOffset);
		}
		RebuildConnections();
	}

	private void RebuildConnections()
	{
		_graphEdit.ClearConnections();
		foreach (var data in _nodeDataMap.Values)
		{
			if (!string.IsNullOrEmpty(data.NextNodeId)) _graphEdit.ConnectNode(data.Id, 0, data.NextNodeId, 0);
			if (data is ChoiceNodeData choice) { for (int i = 0; i < choice.Options.Count; i++) if (!string.IsNullOrEmpty(choice.Options[i].TargetNodeId)) _graphEdit.ConnectNode(data.Id, i, choice.Options[i].TargetNodeId, 0); }
			else if (data is BranchNodeData branch) { if (!string.IsNullOrEmpty(branch.SuccessNodeId)) _graphEdit.ConnectNode(data.Id, 0, branch.SuccessNodeId, 0); if (!string.IsNullOrEmpty(branch.FailNodeId)) _graphEdit.ConnectNode(data.Id, 1, branch.FailNodeId, 0); }
		}
	}

	private void SetupLoadingOverlay()
	{
		_loadingOverlay = new ColorRect { Name = "LoadingOverlay", Visible = false };
		_loadingOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
		_loadingOverlay.Color = new Color(0, 0, 0, 0.5f);
		AddChild(_loadingOverlay);
		var vbox = new VBoxContainer(); vbox.SetAnchorsPreset(LayoutPreset.Center); _loadingOverlay.AddChild(vbox);
		vbox.AddChild(new Label { Text = "正在处理中，请稍候...", HorizontalAlignment = HorizontalAlignment.Center });
		_loadingProgress = new ProgressBar { CustomMinimumSize = new Vector2(300, 20) }; vbox.AddChild(_loadingProgress);
	}

	private void LaunchPreview(string startNodeId, bool isEditMode)
	{
		StoryNodeManager.SyncConnectionsAndPositions(_graphEdit, _nodeDataMap.Values.ToList());
		StoryPreviewUI.Preview(this, _nodeDataMap.Values.ToList(), startNodeId, isEditMode);
	}

	private void CreateCollapsibleCategory(string title, out VBoxContainer container)
	{
		var btn = new Button { Text = "▼ " + title, Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 40) };
		var vbox = new VBoxContainer { Visible = true };
		btn.Pressed += () => { vbox.Visible = !vbox.Visible; btn.Text = (vbox.Visible ? "▼ " : "▶ ") + title; };
		_sideScrollVBox.AddChild(btn); _sideScrollVBox.AddChild(vbox);
		container = vbox;
	}

	private void AddCategoryButton(VBoxContainer container, string text, Action action)
	{
		var btn = new Button { Text = text, CustomMinimumSize = new Vector2(0, 35) };
		btn.Pressed += () => action?.Invoke();
		container.AddChild(btn);
	}

	private void DeleteNode(string nodeName) { if (_graphEdit.HasNode(nodeName)) { var node = _graphEdit.GetNode<GraphNode>(nodeName); _nodeDataMap.Remove(nodeName); node.QueueFree(); } }
	private void ShowCreateProjectDialog() { }
	private void ShowOpenProjectDialog() { }

	/// <summary>
	/// 项目元信息编辑弹窗
	/// </summary>
	private void ShowProjectMetadataDialog()
	{
		if (!EnsureProjectOpen()) return;

		// 使用 AcceptDialog 兼容所有平台（包括安卓嵌入子窗口）
		var dialog = new AcceptDialog();
		dialog.Title = "项目信息";
		dialog.Size = new Vector2I(450, 350);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);

		// 标题
		var titleEdit = new LineEdit { Text = ProjectManager.Metadata.Title, PlaceholderText = "项目标题" };
		vbox.AddChild(new Label { Text = "标题:" });
		vbox.AddChild(titleEdit);

		// 版本
		var versionEdit = new LineEdit { Text = ProjectManager.Metadata.Version, PlaceholderText = "版本号" };
		vbox.AddChild(new Label { Text = "版本:" });
		vbox.AddChild(versionEdit);

		// 作者
		var authorEdit = new LineEdit { Text = ProjectManager.Metadata.Author, PlaceholderText = "作者名" };
		vbox.AddChild(new Label { Text = "作者:" });
		vbox.AddChild(authorEdit);

		// 描述
		var descEdit = new TextEdit { Text = ProjectManager.Metadata.Description, CustomMinimumSize = new Vector2(0, 80), PlaceholderText = "项目描述..." };
		vbox.AddChild(new Label { Text = "描述:" });
		vbox.AddChild(descEdit);

		dialog.AddChild(vbox);
		AddChild(dialog);

		// 点击确认后保存
		dialog.Confirmed += () => {
			ProjectManager.Metadata.Title = titleEdit.Text;
			ProjectManager.Metadata.Version = versionEdit.Text;
			ProjectManager.Metadata.Author = authorEdit.Text;
			ProjectManager.Metadata.Description = descEdit.Text;
			ProjectManager.SaveMetadata();
			GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("项目信息已保存！");
		};

		dialog.CallDeferred("popup_centered");
	}

	/// <summary>
	/// 全局搜索弹窗（在所有节点中搜索关键字）
	/// </summary>
	private void OpenSearchDialog()
	{
		var dialog = new AcceptDialog();
		dialog.Title = "全局搜索";
		dialog.Size = new Vector2I(500, 400);
		dialog.OkButtonText = "关闭";

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 6);

		var searchInput = new LineEdit { PlaceholderText = "输入搜索关键字..." };
		vbox.AddChild(searchInput);

		var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 250) };
		var resultList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		scroll.AddChild(resultList);
		vbox.AddChild(scroll);

		// 搜索逻辑
		searchInput.TextChanged += (query) => {
			foreach (Node child in resultList.GetChildren()) child.QueueFree();
			if (string.IsNullOrWhiteSpace(query)) return;

			string lowerQuery = query.ToLower();
			foreach (var kvp in _nodeDataMap)
			{
				string nodeText = kvp.Value.GetSearchableText();
				if (nodeText.ToLower().Contains(lowerQuery))
				{
					var btn = new Button {
						Text = $"[{kvp.Value.GetType().Name}] {nodeText.Substring(0, System.Math.Min(nodeText.Length, 60))}",
						Alignment = HorizontalAlignment.Left
					};
					// 点击跳转到该节点
					string nodeId = kvp.Key;
					btn.Pressed += () => {
						if (_graphEdit.HasNode(nodeId))
						{
							var gn = _graphEdit.GetNode<GraphNode>(nodeId);
							_graphEdit.ScrollOffset = gn.PositionOffset - _graphEdit.Size / 2;
						}
					};
					resultList.AddChild(btn);
				}
			}
			if (resultList.GetChildCount() == 0)
			{
				resultList.AddChild(new Label { Text = "未找到匹配结果", HorizontalAlignment = HorizontalAlignment.Center });
			}
		};

		dialog.AddChild(vbox);
		AddChild(dialog);
		dialog.CallDeferred("popup_centered");
	}

	private void AutoFixNodeOrder() { }
	private void ConnectNodesUndoable(string f, long fp, string t, long tp) { _graphEdit.ConnectNode(f, (int)fp, t, (int)tp); if (_nodeDataMap.TryGetValue(f, out var fromData) && _nodeDataMap.TryGetValue(t, out var toData)) { if (fromData is ChoiceNodeData c) { if (fp < c.Options.Count) c.Options[(int)fp].TargetNodeId = toData.Id; } else if (fromData is BranchNodeData b) { if (fp == 0) b.SuccessNodeId = toData.Id; else b.FailNodeId = toData.Id; } else fromData.NextNodeId = toData.Id; } }
	private void DisconnectNodesUndoable(string f, long fp, string t, long tp) { _graphEdit.DisconnectNode(f, (int)fp, t, (int)tp); if (_nodeDataMap.TryGetValue(f, out var fromData)) { if (fromData is ChoiceNodeData c) { if (fp < c.Options.Count) c.Options[(int)fp].TargetNodeId = null; } else if (fromData is BranchNodeData b) { if (fp == 0) b.SuccessNodeId = null; else b.FailNodeId = null; } else fromData.NextNodeId = null; } }
}
