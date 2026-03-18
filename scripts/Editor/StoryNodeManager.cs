using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using UmaArchive.Editor.Nodes;

public class StoryNodeManager
{
    private static string _savePath = "user://story_project.json";

    // 用于生成逻辑视图中心坐标
    public static Vector2 GetViewCenter(GraphEdit graphEdit)
    {
        Vector2 viewportCenter = graphEdit.Size / 2;
        return (graphEdit.ScrollOffset + viewportCenter) / graphEdit.Zoom;
    }

    // 保存所有数据到指定物理路径
    public static void SaveProject(GraphEdit graphEdit, List<BaseNodeData> nodes, string absolutePath)
    {
        foreach (Node child in graphEdit.GetChildren())
        {
            if (child is GraphNode gNode)
            {
                BaseNodeData data = nodes.Find(n => n.Id == gNode.Name);
                if (data != null)
                {
                    data.SyncFromView(gNode);
                    
                    data.NextNodeId = ""; 
                    foreach (var conn in graphEdit.GetConnectionList())
                    {
                        if (conn["from_node"].AsString() == gNode.Name)
                        {
                            data.NextNodeId = conn["to_node"].AsString();
                            break; 
                        }
                    }
                }
            }
        }

        string json = JsonSerializer.Serialize(nodes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(absolutePath, json);
        GD.Print($"Project Saved to: {absolutePath}");
    }

    // 从指定物理路径读取
    public static List<BaseNodeData> LoadProject(string absolutePath)
    {
        if (!File.Exists(absolutePath)) return new List<BaseNodeData>();

        string json = File.ReadAllText(absolutePath);
        return JsonSerializer.Deserialize<List<BaseNodeData>>(json);
    }
}
