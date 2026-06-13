using System;
using System.Collections.Generic;
using Godot;

namespace EraDream.Editor
{
    /// <summary>
    /// 剧情节点类型枚举
    /// </summary>
    public enum StoryNodeType
    {
        Dialogue,  // 对话（有角色名）
        Narrative, // 旁白/叙述（无角色名）
        Choice,    // 选项节点
        Jump,      // 跳转
        Condition  // 条件分支
    }

    /// <summary>
    /// 基础剧情节点数据结构
    /// </summary>
    public class StoryNodeData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public StoryNodeType Type { get; set; } = StoryNodeType.Dialogue;
        
        // 核心内容
        public string CharacterName { get; set; } = ""; // 旁白模式下此项为空
        public string Content { get; set; } = "";
        
        // 演出控制
        public string Emotion { get; set; } = "default";
        public string Action { get; set; } = "";
        
        // 连接逻辑
        public string NextNodeId { get; set; } = ""; 
        public List<StoryChoiceData> Choices { get; set; } = new List<StoryChoiceData>();
    }

    /// <summary>
    /// 选项数据结构
    /// </summary>
    public class StoryChoiceData
    {
        public string Text { get; set; } = "";
        public string TargetNodeId { get; set; } = "";
    }

    /// <summary>
    /// 完整的剧本文件结构
    /// </summary>
    public class StoryProjectData
    {
        public string ProjectName { get; set; } = "New Story";
        public List<StoryNodeData> Nodes { get; set; } = new List<StoryNodeData>();
        public string StartNodeId { get; set; } = "";
    }
}
