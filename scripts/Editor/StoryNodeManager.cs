using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using UmaArchive.Editor.Nodes;

public class StoryNodeManager
{
    private static string _savePath = "user://story_project.json";

    public static void SaveProject(GraphEdit graph, List<BaseNodeData> nodes, string path)
    {
        // 1. 同步连接和坐标
        SyncConnectionsAndPositions(graph, nodes);

        // 2. 序列化
        string json = JsonSerializer.Serialize(nodes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// 将 GraphEdit 视图中的实时连接和坐标数据同步到内存对象中
    /// </summary>
    public static void SyncConnectionsAndPositions(GraphEdit graph, List<BaseNodeData> nodes)
    {
        var connections = graph.GetConnectionList();
        
        foreach (var nodeData in nodes)
        {
            // 同步坐标 (保留2位小数)
            if (graph.HasNode(nodeData.Id))
            {
                var viewNode = graph.GetNode<GraphNode>(nodeData.Id);
                nodeData.PosX = (float)Math.Round(viewNode.PositionOffset.X, 2);
                nodeData.PosY = (float)Math.Round(viewNode.PositionOffset.Y, 2);
            }

            // 同步单输出节点的 NextNodeId
            if (!(nodeData is ChoiceNodeData) && !(nodeData is BranchNodeData))
            {
                var conn = connections.FirstOrDefault(c => (string)c["from_node"] == nodeData.Id);
                nodeData.NextNodeId = conn != null ? (string)conn["to_node"] : "";
            }
        }
    }

    public static List<BaseNodeData> LoadProject(string absolutePath)
    {
        if (!File.Exists(absolutePath)) return new List<BaseNodeData>();

        try {
            string json = File.ReadAllText(absolutePath);
            var result = JsonSerializer.Deserialize<List<BaseNodeData>>(json);
            return result ?? new List<BaseNodeData>();
        } catch {
            return new List<BaseNodeData>();
        }
    }

    public static Vector2 GetViewCenter(GraphEdit graph)
    {
        return (graph.Size / 2 + graph.ScrollOffset) / graph.Zoom;
    }
}
