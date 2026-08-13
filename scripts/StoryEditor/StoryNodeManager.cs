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

        /// <summary>
        /// 加载剧本数据，支持向下兼容旧数组格式。
        /// </summary>
        public static List<BaseNodeData> LoadProject(string absolutePath)
        {
            TryLoadProject(absolutePath, out var nodes, out _);
            return nodes;
        }

        /// <summary>
        /// 加载并校验剧本，同时保留可供调用方展示的明确失败原因。
        /// </summary>
        public static bool TryLoadProject(string absolutePath, out List<BaseNodeData> nodes, out string error)
        {
            nodes = new List<BaseNodeData>();
            error = "";
            if (string.IsNullOrWhiteSpace(absolutePath) || !FileAccess.FileExists(absolutePath))
            {
                error = $"剧情文件不存在: {absolutePath}";
                return false;
            }

            try
            {
                using var file = FileAccess.Open(absolutePath, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    error = $"无法打开剧情文件: {absolutePath}";
                    return false;
                }

                string json = file.GetAsText().Trim();
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "剧情文件为空。";
                    return false;
                }

                if (json.StartsWith("["))
                {
                    nodes = JsonSerializer.Deserialize<List<BaseNodeData>>(json) ?? new List<BaseNodeData>();
                }
                else
                {
                    var projectData = JsonSerializer.Deserialize<StoryProjectData>(json);
                    nodes = projectData?.Nodes ?? new List<BaseNodeData>();
                }

                var validationErrors = ValidateNodes(nodes);
                if (validationErrors.Count > 0)
                {
                    error = string.Join("\n", validationErrors);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = $"剧情文件损坏或格式无效: {ex.Message}";
                GD.PushError($"[StoryNodeManager] Failed to load project: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 校验会破坏运行时寻址的节点 ID 与连接引用。
        /// </summary>
        public static List<string> ValidateNodes(IReadOnlyList<BaseNodeData> nodes)
        {
            var errors = new List<string>();
            if (nodes == null)
            {
                errors.Add("剧情节点集合为空。");
                return errors;
            }
            if (nodes.Count == 0)
            {
                errors.Add("剧情中没有可播放的节点。");
                return errors;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < nodes.Count; i++)
            {
                BaseNodeData node = nodes[i];
                if (node == null)
                {
                    errors.Add($"第 {i + 1} 个剧情节点为空。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.Id))
                    errors.Add($"第 {i + 1} 个剧情节点缺少 ID。");
                else if (!ids.Add(node.Id))
                    errors.Add($"剧情节点 ID 重复: {node.Id}");
            }

            foreach (BaseNodeData node in nodes.Where(node => node != null))
            {
                ValidateReference(node.Id, "下一节点", node.NextNodeId, ids, errors);
                if (node is ChoiceNodeData choice)
                {
                    if (choice.Options == null || choice.Options.Count == 0)
                    {
                        errors.Add($"节点 {node.Id} 的选项集合为空。");
                    }
                    else
                    {
                        for (int i = 0; i < choice.Options.Count; i++)
                        {
                            if (choice.Options[i] == null)
                            {
                                // 空选项会在创建预览按钮时触发空引用，必须在加载阶段拒绝。
                                errors.Add($"节点 {node.Id} 的选项 {i + 1} 为空。");
                                continue;
                            }

                            ValidateReference(node.Id, $"选项 {i + 1}", choice.Options[i].TargetNodeId, ids, errors);
                        }
                    }
                }
                else if (node is BranchNodeData branch)
                {
                    ValidateReference(node.Id, "成功分支", branch.SuccessNodeId, ids, errors);
                    ValidateReference(node.Id, "失败分支", branch.FailNodeId, ids, errors);
                }
            }

            return errors;
        }

        private static void ValidateReference(string sourceId, string label, string targetId, HashSet<string> ids, List<string> errors)
        {
            // 空引用表示该出口未连接，仅拒绝指向不存在节点的非空引用。
            if (!string.IsNullOrWhiteSpace(targetId) && !ids.Contains(targetId))
                errors.Add($"节点 {sourceId} 的{label}指向不存在的节点: {targetId}");
        }

        public static Vector2 GetViewCenter(GraphEdit graph)
        {
            return (graph.Size / 2 + graph.ScrollOffset) / graph.Zoom;
        }
    }
}
