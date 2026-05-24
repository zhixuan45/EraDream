using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using umaEraArchive.Game;
using umaEraArchive.Game.Models;

namespace UmaEraArchive.Core.Extensions;

/// <summary>
/// 行为包注册表，负责解析 .behavior.json 并分发 Hook
/// </summary>
public partial class BehaviorRegistry : Node
{
    public static BehaviorRegistry Instance { get; private set; }

    private Dictionary<string, List<BehaviorRule>> _rulesByHook = new();
    private Dictionary<string, ItemDefinition> _itemDefinitions = new();
    private Dictionary<string, List<UIOption>> _menus = new();
    private RandomNumberGenerator _rng = new();

    public override void _EnterTree()
    {
        if (Instance == null) Instance = this;
        _rng.Randomize();
    }

    /// <summary>
    /// 从指定路径加载行为包
    /// </summary>
    public void LoadBehaviorPack(string jsonPath)
    {
        try
        {
            string jsonContent = File.ReadAllText(ProjectSettings.GlobalizePath(jsonPath));
            LoadBehaviorPackFromContent(jsonContent);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BehaviorRegistry] Failed to load behavior pack {jsonPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// 从 JSON 字符串直接加载行为包
    /// </summary>
    public void LoadBehaviorPackFromContent(string jsonContent)
    {
        try
        {
            var pack = JsonSerializer.Deserialize<BehaviorPack>(jsonContent);
            if (pack == null) return;

            // 加载规则
            if (pack.Rules != null)
            {
                foreach (var rule in pack.Rules)
                {
                    if (!_rulesByHook.ContainsKey(rule.Hook))
                        _rulesByHook[rule.Hook] = new List<BehaviorRule>();
                    
                    if (rule.Override)
                    {
                        var existing = _rulesByHook[rule.Hook].FirstOrDefault(r => r.Id == rule.Id);
                        if (existing != null)
                        {
                            int index = _rulesByHook[rule.Hook].IndexOf(existing);
                            _rulesByHook[rule.Hook][index] = rule;
                            GD.Print($"[BehaviorRegistry] Overridden rule {rule.Id} for hook {rule.Hook}");
                        }
                        else
                        {
                            _rulesByHook[rule.Hook].Add(rule);
                        }
                    }
                    else
                    {
                        _rulesByHook[rule.Hook].Add(rule);
                        GD.Print($"[BehaviorRegistry] Registered rule {rule.Id} for hook {rule.Hook}");
                    }
                }
            }

            // 加载物品定义
            if (pack.Items != null)
            {
                foreach (var item in pack.Items)
                {
                    if (item.Override && _itemDefinitions.ContainsKey(item.Id))
                    {
                        _itemDefinitions[item.Id] = item;
                        GD.Print($"[BehaviorRegistry] Overridden item {item.Id}: {item.Name}");
                    }
                    else
                    {
                        _itemDefinitions[item.Id] = item;
                        GD.Print($"[BehaviorRegistry] Registered item {item.Id}: {item.Name}");
                    }
                }
            }

            // 加载 UI 菜单
            if (pack.Menus != null)
            {
                foreach (var menu in pack.Menus)
                {
                    if (!_menus.ContainsKey(menu.MenuId))
                        _menus[menu.MenuId] = new List<UIOption>();

                    foreach (var option in menu.Options)
                    {
                        var existing = _menus[menu.MenuId].FirstOrDefault(o => o.Id == option.Id);
                        if (option.Override && existing != null)
                        {
                            int index = _menus[menu.MenuId].IndexOf(existing);
                            _menus[menu.MenuId][index] = option;
                            GD.Print($"[BehaviorRegistry] Overridden UI option {option.Id} in menu {menu.MenuId}");
                        }
                        else
                        {
                            _menus[menu.MenuId].Add(option);
                            GD.Print($"[BehaviorRegistry] Registered UI option {option.Id} in menu {menu.MenuId}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BehaviorRegistry] Failed to parse behavior pack content: {ex.Message}");
        }
    }

    public List<UIOption> GetValidOptions(string menuId, GameState state)
    {
        if (!_menus.TryGetValue(menuId, out var options)) return new List<UIOption>();
        return options.Where(o => EvaluateConditions(o.Conditions, state)).ToList();
    }

    public void ExecuteOptionAction(string menuId, string optionId, GameState state)
    {
        if (!_menus.TryGetValue(menuId, out var options)) return;
        var option = options.FirstOrDefault(o => o.Id == optionId);
        if (option != null)
        {
            ExecuteAction(option.Action, state);
        }
    }

    public ItemDefinition GetItemDefinition(string id)
    {
        return _itemDefinitions.TryGetValue(id, out var def) ? def : null;
    }

    public List<ItemDefinition> GetAllItemDefinitions()
    {
        return _itemDefinitions.Values.ToList();
    }

    /// <summary>
    /// 触发一个 Hook，并执行满足条件的随机规则
    /// </summary>
    public void TriggerHook(string hookName, GameState state)
    {
        if (!_rulesByHook.TryGetValue(hookName, out var rules)) return;

        // 筛选满足条件的规则
        var validRules = rules.Where(r => EvaluateConditions(r.Conditions, state)).ToList();
        if (validRules.Count == 0) return;

        // 概率判定并选择一个执行
        foreach (var rule in validRules.OrderByDescending(r => r.Probability))
        {
            if (_rng.Randf() <= rule.Probability)
            {
                ExecuteAction(rule.Action, state);
                return; // 每次 Hook 只触发一个规则
            }
        }
    }

    private bool EvaluateConditions(List<BehaviorCondition> conditions, GameState state)
    {
        if (conditions == null) return true;
        foreach (var cond in conditions)
        {
            float currentVal = GetPropertyValue(cond.Property, state);
            if (!float.TryParse(cond.Value, out float targetVal)) continue;

            bool success = cond.Operator switch
            {
                "==" => Mathf.IsEqualApprox(currentVal, targetVal),
                "!=" => !Mathf.IsEqualApprox(currentVal, targetVal),
                ">" => currentVal > targetVal,
                "<" => currentVal < targetVal,
                ">=" => currentVal >= targetVal,
                "<=" => currentVal <= targetVal,
                _ => false
            };

            if (!success) return false;
        }
        return true;
    }

    private float GetPropertyValue(string property, GameState state)
    {
        if (property.StartsWith("Variable:"))
        {
            return GlobalGameState.Instance.GetVariable(property.Substring(9));
        }

        return property switch
        {
            "Player.Money" => state.Player.Money,
            "Player.Stamina" => state.Player.Stamina,
            "Player.Energy" => state.Player.Energy,
            "Uma.Mood" => state.Uma.Mood,
            "Uma.Affection" => state.Uma.Affection,
            "Uma.Speed" => state.Uma.Speed,
            "Uma.Stamina" => state.Uma.Stamina,
            "Uma.Power" => state.Uma.Power,
            "Uma.Guts" => state.Uma.Guts,
            "Uma.Intelligence" => state.Uma.Intelligence,
            _ => 0f
        };
    }

    private void ExecuteAction(BehaviorAction action, GameState state)
    {
        GD.Print($"[BehaviorRegistry] Executing action: {action.Type} path: {action.Path}");

        if (action.Type == "DetailedStory")
        {
            // 切换到剧情引擎
            StoryPlayerEngine.CurrentStoryPath = action.Path;
            // TODO: 这里需要一个全局导航方法，暂时使用 ChangeScene
            GetTree().ChangeSceneToFile("res://scenes/StoryPlayerScreen.tscn");
        }
        else if (action.Type == "BriefStory")
        {
            // 简要剧情：目前通过通知显示
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"触发简要剧情: {action.Path}");
        }
        else if (action.Type == "ChangeStat")
        {
            if (float.TryParse(action.ValueChange, out float val))
            {
                ApplyStatChange(action.TargetProperty, val, state);
                string sign = val >= 0 ? "+" : "";
                string propName = action.TargetProperty.Split('.').Last();
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"{propName} {sign}{val}");
            }
        }
    }

    private void ApplyStatChange(string property, float value, GameState state)
    {
        int amount = (int)value;
        if (property.StartsWith("Variable:"))
        {
            string varName = property.Substring(9);
            float current = GlobalGameState.Instance.GetVariable(varName);
            GlobalGameState.Instance.SetVariable(varName, current + value);
            return;
        }

        switch (property)
        {
            case "Player.Money": state.Player.AddMoney(amount); break;
            case "Player.Stamina": state.Player.AddStamina(amount); break;
            case "Player.Energy": state.Player.AddEnergy(amount); break;
            case "Uma.Mood": state.Uma.AddMood(amount); break;
            case "Uma.Affection": state.Uma.Affection += amount; break;
            case "Uma.Speed": state.Uma.AddStat(StatType.Speed, amount); break;
            case "Uma.Stamina": state.Uma.AddStat(StatType.Stamina, amount); break;
            case "Uma.Power": state.Uma.AddStat(StatType.Power, amount); break;
            case "Uma.Guts": state.Uma.AddStat(StatType.Guts, amount); break;
            case "Uma.Intelligence": state.Uma.AddStat(StatType.Intelligence, amount); break;
        }
    }

    public void Clear()
    {
        _rulesByHook.Clear();
        _itemDefinitions.Clear();
    }

    public List<string> GetPermanentItemIds()
    {
        return _itemDefinitions.Values
            .Where(item => item.Type == ItemType.Permanent)
            .Select(item => item.Id)
            .ToList();
    }
}
