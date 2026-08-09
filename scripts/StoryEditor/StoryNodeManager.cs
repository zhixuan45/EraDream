using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using EraDream.Core;
using EraDream.StoryEditor.Nodes;
using FileAccess = Godot.FileAccess;

namespace EraDream.StoryEditor
{
    // 用于序列化和版本控制的数据封装类
    public class StoryProjectData
    {
        public int SchemaVersion { get; set; } = 1;
        public List<BaseNodeData> Nodes { get; set; } = new List<BaseNodeData>();
    }

    public class StoryNodeManager
    {
        // 保存剧本数据，使用新版对象格式并记录版本号
        public static bool SaveProject(GraphEdit graph, List<BaseNodeData> nodes, string path)
        {
            SyncConnectionsAndPositions(graph, nodes);

            var projectData = new StoryProjectData { Nodes = nodes };
            if (!FileIOManager.SaveJson(path, projectData))
            {
                GD.PushError($"[StoryNodeManager] Failed to open file for writing: {path}");
                ErrorNotifier.Instance?.ShowErrorDialog("保存失败", $"无法打开文件进行写入: {path}");
                return false;
            }
            return true;
        }

        // 将 GraphEdit 视图中的实时连接和坐标数据同步到内存对象中
        public static void SyncConnectionsAndPositions(GraphEdit graph, List<BaseNodeData> nodes)
        {
            var connections = graph.GetConnectionList();
            
            foreach (var nodeData in nodes)
            {
                if (graph.HasNode(nodeData.Id))
                {
                    var viewNode = graph.GetNode<GraphNode>(nodeData.Id);
                    nodeData.PosX = (float)Math.Round(viewNode.PositionOffset.X, 2);
                    nodeData.PosY = (float)Math.Round(viewNode.PositionOffset.Y, 2);
                    
                    nodeData.SyncFromView(viewNode); //千万不能删除这一行代码，不然无法保存对话内容
                }

                if (!(nodeData is ChoiceNodeData) && !(nodeData is BranchNodeData))
                {
                    var conn = connections.FirstOrDefault(c => (string)c["from_node"] == nodeData.Id);
                    nodeData.NextNodeId = conn != null ? (string)conn["to_node"] : "";
                }
            }
        }

        // 加载剧本数据，支持向下兼容旧数组格式
        public static List<BaseNodeData> LoadProject(string absolutePath)
        {
            if (!FileAccess.FileExists(absolutePath)) return new List<BaseNodeData>();

            try {
                using var file = FileAccess.Open(absolutePath, FileAccess.ModeFlags.Read);
                if (file == null) return new List<BaseNodeData>();
                
                string json = file.GetAsText().Trim();
                if (json.StartsWith("["))
                {
                    var result = JsonSerializer.Deserialize<List<BaseNodeData>>(json);
                    return result ?? new List<BaseNodeData>();
                }
                else
                {
                    var projectData = JsonSerializer.Deserialize<StoryProjectData>(json);
                    return projectData?.Nodes ?? new List<BaseNodeData>();
                }
            } catch (Exception ex) {
                GD.PushError($"[StoryNodeManager] Failed to load project: {ex.Message}");
                ErrorNotifier.Instance?.ShowErrorDialog("加载失败", $"剧情文件损坏或加载异常:\n{ex.Message}");
                return new List<BaseNodeData>();
            }
        }

        public static Vector2 GetViewCenter(GraphEdit graph)
        {
            return (graph.Size / 2 + graph.ScrollOffset) / graph.Zoom;
        }
    }
}
