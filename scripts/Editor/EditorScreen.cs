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

    public override void _Ready()
    {
        _graphEdit = GetNode<GraphEdit>("HSplitContainer/GraphEdit");
        
        // 绑定工厂按钮
        GetNode<Button>("HSplitContainer/SidePanel/VBoxContainer/BtnAddNode").Pressed += () => SpawnNode(new DialogueNodeData());
        AddSideButton("添加旁白节点", () => SpawnNode(new NarrativeNodeData()));
        AddSideButton("添加选项节点", () => SpawnNode(new ChoiceNodeData()));
        AddSideButton("添加判定节点", () => SpawnNode(new BranchNodeData()));
        AddSideButton("添加音乐节点", () => SpawnNode(new MusicNodeData()));

        // 保存逻辑：唤醒平台原生保存对话框
        GetNode<Button>("HSplitContainer/SidePanel/VBoxContainer/BtnSave").Pressed += () => {
            FileIOManager.OpenSaveDialog("保存剧情剧本", "story_data.json", "*.json", (path) => {
                StoryNodeManager.SaveProject(_graphEdit, _nodeDataMap.Values.ToList(), path);
            });
        };

        // 加载逻辑：唤醒平台原生打开对话框
        GetNode<Button>("HSplitContainer/SidePanel/VBoxContainer/BtnLoad").Pressed += () => {
            FileIOManager.OpenLoadDialog("打开剧情剧本", "*.json", (path) => {
                LoadAndRender(path);
            });
        };

        GetNode<Button>("HSplitContainer/SidePanel/VBoxContainer/BtnReturn").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenuScreen.tscn");

        // 基础图表信号
        _graphEdit.ConnectionRequest += (f, fp, t, tp) => _graphEdit.ConnectNode(f, (int)fp, t, (int)tp);
        _graphEdit.DisconnectionRequest += (f, fp, t, tp) => _graphEdit.DisconnectNode(f, (int)fp, t, (int)tp);
        _graphEdit.DeleteNodesRequest += (nodes) => { 
            foreach (string n in nodes) DeleteNode(n); 
        };
    }

    private void AddSideButton(string text, Action onPressed)
    {
        Button btn = new Button { Text = text };
        btn.Pressed += onPressed;
        GetNode<VBoxContainer>("HSplitContainer/SidePanel/VBoxContainer").AddChild(btn);
        GetNode<VBoxContainer>("HSplitContainer/SidePanel/VBoxContainer").MoveChild(btn, GetNode<VBoxContainer>("HSplitContainer/SidePanel/VBoxContainer").GetChildCount() - 4);
    }

    private void SpawnNode(BaseNodeData data, Vector2? position = null)
    {
        data.OnDeleteRequested = () => DeleteNode(data.Id);

        GraphNode gNode = data.CreateGraphNode(_graphEdit);
        _nodeDataMap[gNode.Name] = data; 
        
        gNode.PositionOffset = position ?? (StoryNodeManager.GetViewCenter(_graphEdit) - (new Vector2(250, 150) / 2));
        
        _graphEdit.AddChild(gNode);
    }

    private void DeleteNode(string nodeName)
    {
        if (_graphEdit.HasNode(nodeName))
        {
            var node = _graphEdit.GetNode<GraphNode>(nodeName);
            _graphEdit.RemoveChild(node);
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
        int i = 0;
        foreach (var data in loadedData)
        {
            SpawnNode(data, StoryNodeManager.GetViewCenter(_graphEdit) + new Vector2(i * 40, i * 40));
            i++;
        }

        CallDeferred(nameof(RebuildConnections));
    }

    private void RebuildConnections()
    {
        foreach (var data in _nodeDataMap.Values)
        {
            if (!string.IsNullOrEmpty(data.NextNodeId))
            {
                _graphEdit.ConnectNode(data.Id, 0, data.NextNodeId, 0);
            }
        }
    }
}
