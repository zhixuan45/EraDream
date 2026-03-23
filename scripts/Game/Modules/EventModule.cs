using Godot;

namespace umaEraArchive.Game;

/// <summary>
/// 负责抛出随机事件，并在关键节点尝试调用原有的视觉小说剧情系统
/// </summary>
public partial class EventModule : Node
{
    /// <summary>
    /// 触发一个回合开始时的随机事件
    /// </summary>
    public void CheckAndTriggerTurnEvent(GameState state)
    {
        // 未来可扩展：从事件池读取概率并触发
        // 根据纯文字事件给出对应奖励或惩罚
        GD.Print($"[EventModule] 检查 {state.CurrentTurn} 回合事件...");
    }

    /// <summary>
    /// 当满足某些羁绊或固定回合时，呼叫视觉小说引擎
    /// </summary>
    public void TriggerStoryEvent(string storyId)
    {
        var state = GameManager.Instance?.CurrentState;
        if (state != null && state.ScenarioPaths != null && state.ScenarioPaths.Count > 0)
        {
            GD.Print($"[EventModule] 准备触发来自 {state.ScenarioPaths.Count} 个已挂载剧本包中的事件: {storyId}");
            // TODO: 解析各包裹起点节点的 TriggerCondition，并实行跳转
        }
        else
        {
            GD.Print($"[EventModule] 触发事件: {storyId} 但当前未绑定具体剧本包");
        }
    }
}
