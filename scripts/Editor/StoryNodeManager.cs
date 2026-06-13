using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using EraDream.Editor.Nodes;
using FileAccess = Godot.FileAccess;

public class StoryNodeManager
{
	public static void SaveProject(GraphEdit graph, List<BaseNodeData> nodes, string path)
	{
		// 1. 同步连接和坐标
		SyncConnectionsAndPositions(graph, nodes);

		// 2. 序列化
		string json = JsonSerializer.Serialize(nodes, new JsonSerializerOptions { WriteIndented = true });
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (file != null)
		{
			file.StoreString(json);
		}
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
				
				// 关键修复：将视图中用户输入的内容（对话/叙述正文等）同步回数据模型
				nodeData.SyncFromView(viewNode);//千万不能删除这一行代码，不然无法保存对话内容
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
		if (!FileAccess.FileExists(absolutePath)) return new List<BaseNodeData>();

		try {
			using var file = FileAccess.Open(absolutePath, FileAccess.ModeFlags.Read);
			string json = file.GetAsText();
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
