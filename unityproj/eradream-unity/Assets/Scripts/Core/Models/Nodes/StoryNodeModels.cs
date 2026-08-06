using System;
using System.Collections.Generic;

namespace EraDream.Core.Models.Nodes
{
    // 对话节点数据
    public class DialogueNodeData : BaseNodeData
    {
        public string Speaker { get; set; } = "";
        public string Text { get; set; } = "";
        public string CharacterId { get; set; } = "";
        public string VoicePath { get; set; } = "";
        public string Expression { get; set; } = "";

        public override string GetSearchableText() => $"{Speaker}: {Text}";
    }

    // 叙事/全屏旁白节点数据
    public class NarrativeNodeData : BaseNodeData
    {
        public string Text { get; set; } = "";
        public string OverlayColor { get; set; } = "#00000000";

        public override string GetSearchableText() => $"[Narrative] {Text}";
    }

    // 选项节点中的子选项
    public class ChoiceOption
    {
        public string Text { get; set; } = "";
        public string TargetNodeId { get; set; } = "";
    }

    // 选项节点数据
    public class ChoiceNodeData : BaseNodeData
    {
        public List<ChoiceOption> Options { get; set; } = new List<ChoiceOption>();
    }

    // 条件分支节点数据
    public class BranchNodeData : BaseNodeData
    {
        public string VariableId { get; set; } = "";
        public string CompareOperator { get; set; } = "=="; // ==, !=, >, <, >=, <=
        public string CompareValue { get; set; } = "0";
        public string TrueNodeId { get; set; } = "";
        public string FalseNodeId { get; set; } = "";
    }

    // 音频/音乐节点数据
    public class MusicNodeData : BaseNodeData
    {
        public string AudioPath { get; set; } = "";
        public float Volume { get; set; } = 1.0f;
        public bool IsLoop { get; set; } = true;
        public bool StopAudio { get; set; } = false;
    }

    // 背景图像变更节点数据
    public class BackgroundNodeData : BaseNodeData
    {
        public string BackgroundPath { get; set; } = "";
        public string TransitionType { get; set; } = "Fade";
        public float Duration { get; set; } = 1.0f;
    }

    // 角色立绘调配节点数据
    public class SpriteNodeData : BaseNodeData
    {
        public string CharacterId { get; set; } = "";
        public string Action { get; set; } = "Show"; // Show, Hide, Change
        public string Expression { get; set; } = "";
        public string Position { get; set; } = "Center"; // Left, Center, Right, Custom
        public float CustomX { get; set; } = 0.5f;
        public float CustomY { get; set; } = 0.5f;
    }

    // 数值修改节点数据
    public class ValueNodeData : BaseNodeData
    {
        public string VariableId { get; set; } = "";
        public string Operation { get; set; } = "Add"; // Add, Set, Subtract
        public string Value { get; set; } = "1";
    }

    // 贴纸/CG叠加节点数据
    public class StickerNodeData : BaseNodeData
    {
        public int StickerSlot { get; set; } = 0;
        public string StickerPath { get; set; } = "";
        public float PosX { get; set; } = 0.5f;
        public float PosY { get; set; } = 0.5f;
        public float Scale { get; set; } = 1.0f;
        public bool IsVisible { get; set; } = true;
    }

    // 起点节点
    public class StartNodeData : BaseNodeData { }

    // 终点节点
    public class EndNodeData : BaseNodeData
    {
        public string EndingName { get; set; } = "Ending";
    }
}
