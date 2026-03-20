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

    private Control _loadingOverlay;
    private ProgressBar _loadingProgress;

    public override void _Ready()
    {
        SetupLayout();
        _graphEdit = GetNode<GraphEdit>("VBoxContainer/HSplitContainer/GraphEdit");
        _graphEdit.AddThemeColorOverride("activity_color", new Color(1, 0.2f, 0.2f)); // 设置激活连线颜色为警告红
        
        SetupLoadingOverlay();

        // 绑定工厂按钮
        GetNode<Button>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer/BtnAddNode").Pressed += () => {
            if (EnsureProjectOpen()) SpawnNode(new DialogueNodeData());
        };
        AddSideButton("添加旁白节点", () => {
            if (EnsureProjectOpen()) SpawnNode(new NarrativeNodeData());
        });
        AddSideButton("添加背景节点", () => {
            if (EnsureProjectOpen()) SpawnNode(new BackgroundNodeData());
        });
        AddSideButton("添加立绘节点", () => {
            if (EnsureProjectOpen()) SpawnNode(new SpriteNodeData());
        });
        AddSideButton("添加选项节点", () => {
            if (EnsureProjectOpen()) SpawnNode(new ChoiceNodeData());
        });
        AddSideButton("添加判定节点", () => {
            if (EnsureProjectOpen()) SpawnNode(new BranchNodeData());
        });
        AddSideButton("添加音乐节点", () => {
            if (EnsureProjectOpen()) SpawnNode(new MusicNodeData());
        });
        AddSideButton("添加开始节点", () => {
            if (EnsureProjectOpen()) SpawnNode(new StartNodeData());
        });
        AddSideButton("添加结束节点", () => {
            if (EnsureProjectOpen()) SpawnNode(new EndNodeData());
        });

        AddSideButton("自动纠正节点顺序", () => {
            if (EnsureProjectOpen()) AutoFixNodeOrder();
        });
        AddSideButton(" 预览剧情 ", () => {
            if (EnsureProjectOpen()) LaunchPreview(null, false);
        });

        GetNode<Button>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer/BtnReturn").Pressed += () => {
            LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        };

        // 基础图表信号
        _graphEdit.ConnectionRequest += (f, fp, t, tp) => {
            _graphEdit.ConnectNode(f, (int)fp, t, (int)tp);
            if (_nodeDataMap.TryGetValue(f, out var fromData) && _nodeDataMap.TryGetValue(t, out var toData))
            {
                // 手动同步一下连接关系到内存
                if (fromData is DialogueNodeData d) d.NextNodeId = toData.Id;
                UpdateNodeWarnings();
                
                if (fromData is DialogueNodeData && (toData is SpriteNodeData || toData is BackgroundNodeData || toData is MusicNodeData))
                {
                    GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("⚠️ 警告：该对话节点已标红，建议将视觉/音频节点置于其前。");
                }
            }
        };
        _graphEdit.DisconnectionRequest += (f, fp, t, tp) => {
            if (!EnsureProjectOpen()) return;
            _graphEdit.DisconnectNode(f, (int)fp, t, (int)tp);
            if (_nodeDataMap.TryGetValue(f, out var fromData))
            {
                if (fromData is DialogueNodeData d) d.NextNodeId = null;
                UpdateNodeWarnings();
            }
        };
        _graphEdit.DeleteNodesRequest += (nodes) => { 
            if (!EnsureProjectOpen()) return;
            foreach (string n in nodes) DeleteNode(n); 
        };
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
        fileMenu.AddItem("导出剧情包 (.era)", 3);
        fileMenu.AddItem("导出项目压缩包 (.zip)", 4);
        fileMenu.AddSeparator();
        
        PopupMenu importMenu = new PopupMenu { Name = "Import" };
        importMenu.AddItem("导入背景图", 10);
        importMenu.AddItem("导入音频", 11);
        importMenu.AddItem("导入立绘", 12);
        fileMenu.AddChild(importMenu);
        fileMenu.AddSubmenuNodeItem("导入资源", importMenu);

        _menuBar.AddChild(fileMenu);
        _menuBar.SetMenuTitle(0, "文件");
        fileMenu.IdPressed += OnFileMenuIdPressed;
        importMenu.IdPressed += OnImportMenuIdPressed;

        PopupMenu charMenu = new PopupMenu { Name = "Character" };
        charMenu.AddItem("角色列表管理", 0);
        charMenu.AddItem("导出角色配置", 1);
        charMenu.AddItem("导入角色配置", 2);

        _menuBar.AddChild(charMenu);
        _menuBar.SetMenuTitle(1, "角色");
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
            case 3:
                if (EnsureProjectOpen())
                {
                    StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile);
                    CharacterManager.SaveCharacters(ProjectManager.CharacterFile);
                    ProjectManager.SaveMetadata();
                    
                    FileIOManager.OpenSaveDialog("导出剧情包", $"{ProjectManager.Metadata.Title}.era", "*.era", (path) => {
                        ProjectManager.ExportAsEra(path);
                    });
                }
                break;
            case 4:
                if (EnsureProjectOpen())
                {
                    StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile);
                    CharacterManager.SaveCharacters(ProjectManager.CharacterFile);
                    ProjectManager.SaveMetadata();
                    
                    FileIOManager.OpenSaveDialog("导出项目压缩包", $"{ProjectManager.Metadata.Title}.zip", "*.zip", (path) => {
                        ProjectManager.ExportProject(path);
                    });
                }
                break;
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

    private void AddSideButton(string text, Action onPressed)
    {
        Button btn = new Button { Text = text };
        btn.Pressed += onPressed;
        GetNode<VBoxContainer>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer").AddChild(btn);
        GetNode<VBoxContainer>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer").MoveChild(btn, GetNode<VBoxContainer>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer").GetChildCount() - 4);
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
        _loadingOverlay = new ColorRect {
            Color = new Color(0, 0, 0, 0.7f),
            Visible = false
        };
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
        // 1. 显示遮罩，禁用图表
        _loadingOverlay.Show();
        _loadingProgress.Value = 0;
        _graphEdit.MouseFilter = MouseFilterEnum.Ignore;
        
        _graphEdit.ClearConnections();
        _nodeDataMap.Clear();
        foreach (Node child in _graphEdit.GetChildren()) if (child is GraphNode) child.QueueFree();

        // 2. 在工作线程加载数据以防反序列化卡顿
        var loadedData = await System.Threading.Tasks.Task.Run(() => StoryNodeManager.LoadProject(path));
        
        if (loadedData.Count == 0)
        {
            FinishLoading();
            return;
        }

        // 3. 分批实例化节点 (Godot API 必须在主线程)
        int total = loadedData.Count;
        int batchSize = 30; // 每帧实例化的节点数，平衡速度与流畅度
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
                // 挂起当前协程，让出主线程一帧用于渲染
                await ToSignal(GetTree(), "process_frame");
            }
        }

        // 4. 重建连线并结束
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
                // 加载时也检查是否需要标红
                if (data is DialogueNodeData && _nodeDataMap.TryGetValue(data.NextNodeId, out var toData))
                {
                    if (toData is SpriteNodeData || toData is BackgroundNodeData || toData is MusicNodeData)
                    {
                        _graphEdit.SetConnectionActivity(data.Id, 0, data.NextNodeId, 0, 1.0f);
                    }
                }
            }

            if (data is ChoiceNodeData choice)
            {
                for (int i = 0; i < choice.Options.Count; i++)
                {
                    if (!string.IsNullOrEmpty(choice.Options[i].TargetNodeId))
                    {
                        _graphEdit.ConnectNode(data.Id, i, choice.Options[i].TargetNodeId, 0);
                    }
                }
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
        // 1. 同步最新数据
        foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>())
        {
            if (_nodeDataMap.TryGetValue(node.Name, out var data)) data.SyncFromView(node);
        }
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
                            // 逻辑交换
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
            // 2. 原地更新视图：不删除节点，只更新坐标和连线
            _graphEdit.ClearConnections();
            foreach (var data in _nodeDataMap.Values)
            {
                if (_graphEdit.HasNode(data.Id))
                {
                    var gNode = _graphEdit.GetNode<GraphNode>(data.Id);
                    gNode.PositionOffset = new Vector2(data.PosX, data.PosY);
                }
            }
            RebuildConnections();
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("自动纠正完成：已交换对话与视觉节点顺序。");
        }
        else
        {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("无需纠正：当前没有发现错位的节点。");
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
                {
                    if (toData is SpriteNodeData || toData is BackgroundNodeData || toData is MusicNodeData)
                        hasIssue = true;
                }

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
}
