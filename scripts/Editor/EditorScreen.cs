using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UmaArchive.Editor.Nodes;
using UmaArchive.Core;

public partial class EditorScreen : Control
{
    private GraphEdit _graphEdit;
    private Dictionary<string, BaseNodeData> _nodeDataMap = new Dictionary<string, BaseNodeData>();
    private MenuBar _menuBar;

    public override void _Ready()
    {
        SetupLayout();
        _graphEdit = GetNode<GraphEdit>("VBoxContainer/HSplitContainer/GraphEdit");
        
        // 绑定工厂按钮
        GetNode<Button>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer/BtnAddNode").Pressed += () => SpawnNode(new DialogueNodeData());
        AddSideButton("添加旁白节点", () => SpawnNode(new NarrativeNodeData()));
        AddSideButton("添加背景节点", () => SpawnNode(new BackgroundNodeData()));
        AddSideButton("添加立绘节点", () => SpawnNode(new SpriteNodeData()));
        AddSideButton("添加选项节点", () => SpawnNode(new ChoiceNodeData()));
        AddSideButton("添加判定节点", () => SpawnNode(new BranchNodeData()));
        AddSideButton("添加音乐节点", () => SpawnNode(new MusicNodeData()));
        AddSideButton("添加开始节点", () => SpawnNode(new StartNodeData()));
        AddSideButton("添加结束节点", () => SpawnNode(new EndNodeData()));

        AddSideButton(" 预览剧情 ", () => {
            // 1. 同步所有节点视图内部数据
            foreach (var node in _graphEdit.GetChildren().OfType<GraphNode>())
            {
                if (_nodeDataMap.TryGetValue(node.Name, out var data)) data.SyncFromView(node);
            }
            // 2. 同步连接关系和坐标
            StoryNodeManager.SyncConnectionsAndPositions(_graphEdit, _nodeDataMap.Values.ToList());
            
            StoryPreviewUI.Preview(this, _nodeDataMap.Values.ToList());
        });

        GetNode<Button>("VBoxContainer/HSplitContainer/SidePanel/VBoxContainer/BtnReturn").Pressed += () => {
            LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        };

        // 基础图表信号
        _graphEdit.ConnectionRequest += (f, fp, t, tp) => _graphEdit.ConnectNode(f, (int)fp, t, (int)tp);
        _graphEdit.DisconnectionRequest += (f, fp, t, tp) => _graphEdit.DisconnectNode(f, (int)fp, t, (int)tp);
        _graphEdit.DeleteNodesRequest += (nodes) => { 
            foreach (string n in nodes) DeleteNode(n); 
        };
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
                if (ProjectManager.IsProjectOpened)
                {
                    StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), ProjectManager.StoryFile);
                    CharacterManager.SaveCharacters(ProjectManager.CharacterFile);
                    ProjectManager.SaveMetadata();
                    GD.Print("Project Saved Successfully.");
                }
                break;
        }
    }

    private void OnImportMenuIdPressed(long id)
    {
        switch (id)
        {
            case 10: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Background); break;
            case 11: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Audio); break;
            case 12: ResourceManagerUI.OpenImportDialog(ResourceManagerUI.ResourceType.Sprite); break;
        }
    }

    private void OnCharMenuIdPressed(long id)
    {
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

    private void LoadAndRender(string path)
    {
        _graphEdit.ClearConnections();
        _nodeDataMap.Clear();
        foreach (Node child in _graphEdit.GetChildren()) if (child is GraphNode) child.QueueFree();

        var loadedData = StoryNodeManager.LoadProject(path);
        foreach (var data in loadedData)
        {
            Vector2 savedPos = new Vector2(data.PosX, data.PosY);
            if (savedPos == Vector2.Zero) savedPos = StoryNodeManager.GetViewCenter(_graphEdit);
            SpawnNode(data, savedPos);
        }
        CallDeferred(nameof(RebuildConnections));
    }

    private void RebuildConnections()
    {
        foreach (var data in _nodeDataMap.Values)
        {
            if (!string.IsNullOrEmpty(data.NextNodeId))
                _graphEdit.ConnectNode(data.Id, 0, data.NextNodeId, 0);

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
}
