using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EraDream.Game;
using EraDream.Game.Models;

namespace EraDream.Core.Extensions;

/// <summary>
/// 行为包注册表，负责解析 .behavior.json 并分发 Hook
/// </summary>
public partial class BehaviorRegistry : Node
{
    public static BehaviorRegistry Instance { get; private set; }

    private Dictionary<string, List<BehaviorRule>> _rulesByHook = new();
    private Dictionary<string, ItemDefinition> _itemDefinitions = new();
    private Dictionary<string, List<UIOption>> _menus = new();
    private Dictionary<string, RaceDefinition> _raceDefinitions = new();
    private Dictionary<string, TrainingDefinition> _trainingDefinitions = new();
    private RandomNumberGenerator _rng = new();

    public class ExtensionRegistration
    {
        public List<(string hook, string ruleId)> Rules { get; } = new();
        public List<string> Items { get; } = new();
        public List<(string menuId, string optionId)> Menus { get; } = new();
        public List<string> Races { get; } = new();
        public List<string> Trainings { get; } = new();
    }

    // 追踪每个扩展包注册的内容，方便增量卸载。ID -> ExtensionRegistration
    private Dictionary<string, ExtensionRegistration> _registeredByExtension = new();

    public override void _EnterTree()
    {
        if (Instance == null) Instance = this;
        _rng.Randomize();
    }

    /// <summary>
    /// 从指定路径加载行为包，关联特定的扩展包 ID
    /// </summary>
    public void LoadBehaviorPack(string jsonPath, string extensionId = null)
    {
        try
        {
            string jsonContent = File.ReadAllText(ProjectSettings.GlobalizePath(jsonPath));
            LoadBehaviorPackFromContent(jsonContent, extensionId);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BehaviorRegistry] Failed to load behavior pack {jsonPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// 从 JSON 字符串直接加载行为包，关联特定的扩展包 ID
    /// </summary>
    public void LoadBehaviorPackFromContent(string jsonContent, string extensionId = null)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var pack = JsonSerializer.Deserialize<BehaviorPack>(jsonContent, options);
            if (pack == null) return;

            if (extensionId != null && !_registeredByExtension.ContainsKey(extensionId))
            {
                _registeredByExtension[extensionId] = new ExtensionRegistration();
            }

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

                    if (extensionId != null)
                    {
                        _registeredByExtension[extensionId].Rules.Add((rule.Hook, rule.Id));
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

                    if (extensionId != null)
                    {
                        _registeredByExtension[extensionId].Items.Add(item.Id);
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

                        if (extensionId != null)
                        {
                            _registeredByExtension[extensionId].Menus.Add((menu.MenuId, option.Id));
                        }
                    }
                }
            }

            // 加载比赛定义
            if (pack.Races != null)
            {
                foreach (var race in pack.Races)
                {
                    if (race.Override && _raceDefinitions.ContainsKey(race.Id))
                    {
                        _raceDefinitions[race.Id] = race;
                        GD.Print($"[BehaviorRegistry] Overridden race {race.Id}: {race.Name}");
                    }
                    else
                    {
                        _raceDefinitions[race.Id] = race;
                        GD.Print($"[BehaviorRegistry] Registered race {race.Id}: {race.Name}");
                    }

                    if (extensionId != null)
                    {
                        _registeredByExtension[extensionId].Races.Add(race.Id);
                    }
                }
            }

            // 加载自定义训练定义
            if (pack.Trainings != null)
            {
                foreach (var training in pack.Trainings)
                {
                    if (training.Override && _trainingDefinitions.ContainsKey(training.Id))
                    {
                        _trainingDefinitions[training.Id] = training;
                        GD.Print($"[BehaviorRegistry] Overridden training {training.Id}: {training.Name}");
                    }
                    else
                    {
                        _trainingDefinitions[training.Id] = training;
                        GD.Print($"[BehaviorRegistry] Registered training {training.Id}: {training.Name}");
                    }

                    if (extensionId != null)
                    {
                        _registeredByExtension[extensionId].Trainings.Add(training.Id);
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

    public RaceDefinition GetRaceDefinition(string id)
    {
        return _raceDefinitions.TryGetValue(id, out var def) ? def : null;
    }

    public List<RaceDefinition> GetRacesForTurn(int turn)
    {
        return _raceDefinitions.Values.Where(r => r.Turn == turn).ToList();
    }

    public List<RaceDefinition> GetAllRaces()
    {
        return _raceDefinitions.Values.ToList();
    }

    public TrainingDefinition GetTrainingDefinition(string id)
    {
        return _trainingDefinitions.TryGetValue(id, out var def) ? def : null;
    }

    public List<TrainingDefinition> GetAllTrainings()
    {
        return _trainingDefinitions.Values.ToList();
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

        if (property.StartsWith("Uma.CustomStats:"))
        {
            return state.Uma.GetCustomStat(property.Substring(16));
        }

        return property switch
        {
            "Game.CurrentTurn" or "CurrentTurn" => state.CurrentTurn,
            "Player.Money" => state.Player.Money,
            "Player.Stamina" => state.Player.Stamina,
            "Player.Energy" => state.Player.Energy,
            "Uma.Mood" => state.Uma.Mood,
            "Uma.ActionStamina" => state.Uma.ActionStamina,
            "Uma.Energy" => state.Uma.Energy,
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
            GetTree().ChangeSceneToFile("res://scenes/StoryPlayerScreen.tscn");
        }
        else if (action.Type == "BriefStory")
        {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"触发简要剧情: {action.Path}");
        }
        else if (action.Type == "ChangeStat")
        {
            if (float.TryParse(action.ValueChange, out float val))
            {
                ApplyStatChange(action.TargetProperty, val, state);
                string sign = val >= 0 ? "+" : "";
                string propName = action.TargetProperty.Split('.').Last();
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"{propName} {sign}{val}");
            }
        }
    }

    public void ApplyStatChange(string property, float value, GameState state)
    {
        int amount = (int)value;
        if (property.StartsWith("Variable:"))
        {
            string varName = property.Substring(9);
            float current = GlobalGameState.Instance.GetVariable(varName);
            GlobalGameState.Instance.SetVariable(varName, current + value);
            return;
        }

        if (property.StartsWith("Uma.CustomStats:"))
        {
            state.Uma.AddCustomStat(property.Substring(16), amount);
            return;
        }

        if (property.Contains(":"))
        {
            state.Uma.AddCustomStat(property, amount);
            return;
        }

        switch (property)
        {
            case "Player.Money": state.Player.AddMoney(amount); break;
            case "Player.Stamina": state.Player.AddStamina(amount); break;
            case "Player.Energy": state.Player.AddEnergy(amount); break;
            case "Uma.Mood": state.Uma.AddMood(amount); break;
            case "Uma.ActionStamina": state.Uma.AddActionStamina(amount); break;
            case "Uma.Energy": state.Uma.AddEnergy(amount); break;
            case "Uma.Affection": state.Uma.Affection += amount; break;
            case "Uma.Speed": state.Uma.AddStat(StatType.Speed, amount); break;
            case "Uma.Stamina": state.Uma.AddStat(StatType.Stamina, amount); break;
            case "Uma.Power": state.Uma.AddStat(StatType.Power, amount); break;
            case "Uma.Guts": state.Uma.AddStat(StatType.Guts, amount); break;
            case "Uma.Intelligence": state.Uma.AddStat(StatType.Intelligence, amount); break;
        }
    }

    public void UnloadBehaviorsForExtension(string extensionId)
    {
        if (string.IsNullOrEmpty(extensionId) || !_registeredByExtension.TryGetValue(extensionId, out var registered)) return;

        // 1. 移除规则
        foreach (var r in registered.Rules)
        {
            if (_rulesByHook.TryGetValue(r.hook, out var list))
            {
                var rule = list.FirstOrDefault(rule => rule.Id == r.ruleId);
                if (rule != null) list.Remove(rule);
                if (list.Count == 0) _rulesByHook.Remove(r.hook);
            }
        }

        // 2. 移除物品定义
        foreach (var itemId in registered.Items)
        {
            _itemDefinitions.Remove(itemId);
        }

        // 3. 移除菜单 UIOption
        foreach (var m in registered.Menus)
        {
            if (_menus.TryGetValue(m.menuId, out var list))
            {
                var opt = list.FirstOrDefault(o => o.Id == m.optionId);
                if (opt != null) list.Remove(opt);
                if (list.Count == 0) _menus.Remove(m.menuId);
            }
        }

        // 4. 移除赛事定义
        foreach (var raceId in registered.Races)
        {
            _raceDefinitions.Remove(raceId);
        }

        // 5. 移除自定义训练定义
        foreach (var trainingId in registered.Trainings)
        {
            _trainingDefinitions.Remove(trainingId);
        }

        _registeredByExtension.Remove(extensionId);
        GD.Print($"[BehaviorRegistry] Unloaded behavior pack for extension: {extensionId}");
    }

    public void Clear()
    {
        _rulesByHook.Clear();
        _itemDefinitions.Clear();
        _menus.Clear();
        _raceDefinitions.Clear();
        _trainingDefinitions.Clear();
        _registeredByExtension.Clear();
    }

    public List<string> GetPermanentItemIds()
    {
        return _itemDefinitions.Values
            .Where(item => item.Type == ItemType.Permanent)
            .Select(item => item.Id)
            .ToList();
    }
}
