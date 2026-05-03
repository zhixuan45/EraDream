using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UmaEraArchive.Editor.Nodes;
using UmaEraArchive.Core;

namespace umaEraArchive.Game;

/// <summary>
/// 负责剧情事件的触发与调度，支持基于属性条件的自动匹配
/// </summary>
public partial class EventModule : Node
{
    private class RegisteredEvent
    {
        public string ProjectPath;
        public StartNodeData StartNode;
    }

    private List<RegisteredEvent> _eventPool = new List<RegisteredEvent>();

    /// <summary>
    /// 预加载所有剧本包中的触发事件，并进行合法性校验
    /// </summary>
    public void LoadEventPool(List<string> scenarioPaths)
    {
        _eventPool.Clear();
        if (scenarioPaths == null) return;

        var errorNotifier = GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier");

        foreach (var path in scenarioPaths)
        {
            try
            {
                var nodes = StoryNodeManager.LoadProject(path);
                string storyId = System.IO.Path.GetFileNameWithoutExtension(path);

                // 校验所有数值节点
                foreach (var node in nodes)
                {
                    if (node is ValueNodeData vn)
                    {
                        bool isInvalid = string.IsNullOrEmpty(vn.TargetAttribute) || 
                                       (vn.TargetAttribute == "Custom" && string.IsNullOrWhiteSpace(vn.CustomId));
                        
                        if (isInvalid)
                        {
                            string valueId = string.IsNullOrWhiteSpace(vn.CustomId) ? "NULL" : vn.CustomId;
                            errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
                        }
                    }
                }

                var startNodes = nodes.OfType<StartNodeData>().Where(n => !string.IsNullOrEmpty(n.TriggerCondition));
                
                foreach (var start in startNodes)
                {
                    _eventPool.Add(new RegisteredEvent { ProjectPath = path, StartNode = start });
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[EventModule] Failed to load scenario {path}: {ex.Message}");
            }
        }
        GD.Print($"[EventModule] Registered {_eventPool.Count} story events.");
    }

    /// <summary>
    /// 检查并触发特定时机的剧情事件
    /// </summary>
    /// <returns>是否触发了剧情（若触发则会阻断后续养成逻辑）</returns>
    public bool CheckAndTriggerStory(TriggerTiming timing, GameState state)
    {
        var candidates = _eventPool.Where(e => e.StartNode.Timing == timing).ToList();
        
        foreach (var evt in candidates)
        {
            if (EvaluateCondition(evt.StartNode.TriggerCondition, state))
            {
                GD.Print($"[EventModule] Condition met! Triggering: {evt.ProjectPath} at {timing}");
                TriggerStory(evt.ProjectPath, evt.StartNode.Id);
                return true;
            }
        }
        return false;
    }

    private void TriggerStory(string path, string startNodeId)
    {
        StoryPlayerEngine.CurrentStoryPath = path;
        StoryPlayerEngine.StartNodeId = startNodeId;
        StoryPlayerEngine.ReturnScenePath = "res://scenes/SimulationMainScreen.tscn";
        
        // 切换到剧情引擎场景
        GetTree().ChangeSceneToFile("res://scenes/StoryPlayerEngine.tscn");
    }

    /// <summary>
    /// 简易条件解析器：支持 Property >= Value, Property < Value, Property == Value 等
    /// </summary>
    private bool EvaluateCondition(string condition, GameState state)
    {
        if (string.IsNullOrWhiteSpace(condition)) return false;

        try
        {
            string[] operators = { ">=", "<=", ">", "<", "==" };
            string op = operators.FirstOrDefault(o => condition.Contains(o));
            if (op == null) return false;

            var parts = condition.Split(new[] { op }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            string property = parts[0].Trim().ToLower();
            if (!float.TryParse(parts[1].Trim(), out float targetVal)) return false;

            float currentVal = GetPropertyValue(property, state);

            return op switch
            {
                ">=" => currentVal >= targetVal,
                "<=" => currentVal <= targetVal,
                ">" => currentVal > targetVal,
                "<" => currentVal < targetVal,
                "==" => Math.Abs(currentVal - targetVal) < 0.001f,
                _ => false
            };
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[EventModule] Condition parse error '{condition}': {ex.Message}");
            return false;
        }
    }

    private float GetPropertyValue(string prop, GameState state)
    {
        if (prop.StartsWith("custom:"))
        {
            string customId = prop.Substring(7);
            return state.Uma.GetCustomStat(customId);
        }

        return prop switch
        {
            "money" => state.Player.Money,
            "vitality" => state.Player.Stamina, // Matches Editor's Trainer Stamina
            "stamina" => state.Player.Stamina,  // Legacy/Alias for Trainer Stamina
            "energy" => state.Player.Energy,
            "speed" => state.Uma.Speed,
            "endurance" => state.Uma.Stamina,   // Legacy Alias
            "uma_stamina" => state.Uma.Stamina, // Explicit Horse Girl Stamina
            "power" => state.Uma.Power,
            "guts" => state.Uma.Guts,
            "intelligence" => state.Uma.Intelligence,
            "skill_points" => state.Uma.SkillPoints,
            "affection" => state.Uma.Affection,
            "turn" => state.CurrentTurn,
            _ => 0
        };
    }

    [Obsolete("Use CheckAndTriggerStory instead")]
    public void CheckAndTriggerTurnEvent(GameState state) { }
}
