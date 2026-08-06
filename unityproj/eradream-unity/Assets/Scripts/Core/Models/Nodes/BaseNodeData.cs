using System;
using System.Text.Json.Serialization;

namespace EraDream.Core.Models.Nodes
{
    // 故事图图节点的核心抽象基类（纯 POCO 数据模型，解耦引擎 UI）
    [JsonDerivedType(typeof(DialogueNodeData), typeDiscriminator: "dialogue")]
    [JsonDerivedType(typeof(NarrativeNodeData), typeDiscriminator: "narrative")]
    [JsonDerivedType(typeof(MusicNodeData), typeDiscriminator: "music")]
    [JsonDerivedType(typeof(ChoiceNodeData), typeDiscriminator: "choice")]
    [JsonDerivedType(typeof(BranchNodeData), typeDiscriminator: "branch")]
    [JsonDerivedType(typeof(StartNodeData), typeDiscriminator: "start")]
    [JsonDerivedType(typeof(EndNodeData), typeDiscriminator: "end")]
    [JsonDerivedType(typeof(BackgroundNodeData), typeDiscriminator: "background")]
    [JsonDerivedType(typeof(SpriteNodeData), typeDiscriminator: "sprite")]
    [JsonDerivedType(typeof(StickerNodeData), typeDiscriminator: "sticker")]
    [JsonDerivedType(typeof(ValueNodeData), typeDiscriminator: "value")]
    public abstract class BaseNodeData
    {
        // 节点唯一标识
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // 默认连接的下一个节点 ID
        public string NextNodeId { get; set; } = "";

        // UI 视图折叠状态标志
        public bool IsExpanded { get; set; } = false;

        // 图表节点 X 轴坐标
        public float PosX { get; set; } = 0;

        // 图表节点 Y 轴坐标
        public float PosY { get; set; } = 0;

        /// <summary>
        /// 返回可供全局搜索匹配的文本，子类可覆盖
        /// </summary>
        public virtual string GetSearchableText() => GetType().Name + " " + Id;
    }
}
