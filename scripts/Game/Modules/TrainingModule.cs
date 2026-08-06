using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using EraDream.Core.Extensions;

namespace EraDream.Game;

/// <summary>
/// 负责处理日程安排与训练功能
/// 包含属性消耗、增长与成功率的计算。
/// </summary>
public partial class TrainingModule : Node
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();
        RegisterDefaultTrainings();
    }

    private void RegisterDefaultTrainings()
    {
        var registry = BehaviorRegistry.Instance;
        if (registry == null) return;

        foreach (TrainingType type in Enum.GetValues(typeof(TrainingType)))
        {
            string id = type.ToString();
            // 如果模组或扩展包没有覆写此默认训练定义，则注册默认定义
            if (registry.GetTrainingDefinition(id) == null)
            {
                var def = GetDefaultTrainingDefinition(type);
                var pack = new BehaviorPack();
                pack.Trainings.Add(def);
                registry.LoadBehaviorPackFromContent(JsonSerializer.Serialize(pack));
            }
        }
    }

    private TrainingDefinition GetDefaultTrainingDefinition(TrainingType type)
    {
        var def = new TrainingDefinition { Id = type.ToString(), Override = false };
        switch (type)
        {
            case TrainingType.Speed:
                def.Name = "速度训练";
                def.Description = "提升速度与力量";
                def.StaminaCost = 20;
                def.StatsRewards["Uma.Speed"] = 10;
                def.StatsRewards["Uma.Power"] = 5;
                def.StatsRewards["Uma.SkillPoints"] = 2;
                break;
            case TrainingType.Stamina:
                def.Name = "耐力训练";
                def.Description = "提升耐力与根性";
                def.StaminaCost = 20;
                def.StatsRewards["Uma.Stamina"] = 10;
                def.StatsRewards["Uma.Guts"] = 5;
                def.StatsRewards["Uma.SkillPoints"] = 2;
                break;
            case TrainingType.Power:
                def.Name = "力量训练";
                def.Description = "提升力量与耐力";
                def.StaminaCost = 20;
                def.StatsRewards["Uma.Power"] = 10;
                def.StatsRewards["Uma.Stamina"] = 5;
                def.StatsRewards["Uma.SkillPoints"] = 2;
                break;
            case TrainingType.Guts:
                def.Name = "根性训练";
                def.Description = "提升根性、速度与力量";
                def.StaminaCost = 20;
                def.StatsRewards["Uma.Guts"] = 10;
                def.StatsRewards["Uma.Speed"] = 5;
                def.StatsRewards["Uma.Power"] = 5;
                def.StatsRewards["Uma.SkillPoints"] = 2;
                break;
            case TrainingType.Intelligence:
                def.Name = "智力训练";
                def.Description = "提升智力、速度并恢复精力";
                def.StaminaCost = -5; // 恢复马娘体力
                def.StatsRewards["Uma.Intelligence"] = 10;
                def.StatsRewards["Uma.Speed"] = 2;
                def.StatsRewards["Uma.SkillPoints"] = 5;
                def.StatsRewards["Player.Energy"] = 5;
                break;
        }
        return def;
    }

    /// <summary>
    /// 向后兼容接口，执行指定类型的内置训练
    /// </summary>
    public TrainingResult ExecuteTraining(GameState state, TrainingType type, bool isAccompanied = false)
    {
        return ExecuteTraining(state, type.ToString(), isAccompanied);
    }

    /// <summary>
    /// 执行一次训练动作（支持内置与自定义训练），返回训练结果状态
    /// </summary>
    public TrainingResult ExecuteTraining(GameState state, string trainingId, bool isAccompanied = false)
    {
        var registry = BehaviorRegistry.Instance;
        if (registry == null) return TrainingResult.Failed;

        var training = registry.GetTrainingDefinition(trainingId);
        if (training == null)
        {
            // 如果传入的是内置枚举字符串但还没在 Ready 里注册成功，做一次 Fallback 解析
            if (Enum.TryParse<TrainingType>(trainingId, true, out var tType))
            {
                training = GetDefaultTrainingDefinition(tType);
            }
        }

        if (training == null)
        {
            GD.PrintErr($"[TrainingModule] Training definition not found: {trainingId}");
            return TrainingResult.Failed;
        }

        int staminaCost = training.StaminaCost;
        int playerEnergyCost = isAccompanied ? 15 : 0;
        
        // 马娘行动体力不足判断（staminaCost 为负数代表冥想等回复体力的动作，不作拦截）
        if (state.Uma.ActionStamina < staminaCost && staminaCost > 0)
        {
            return TrainingResult.InsufficientStamina;
        }

        // 训练员精力不足判断
        if (isAccompanied && state.Player.Energy < playerEnergyCost)
        {
            // 陪伴精力消耗拦截
            return TrainingResult.InsufficientTrainerEnergy;
        }

        // 失败率计算：考虑马娘 ActionStamina 和训练员 Energy
        float failureRate = CalculateFailureRate(state.Uma.ActionStamina, state.Uma.MaxActionStamina, state.Player.Energy, state.Player.MaxEnergy);
        bool isFailed = _rng.Randf() < failureRate;

        // 扣减资源
        state.Uma.ConsumeActionStamina(staminaCost);
        if (isAccompanied) state.Player.ConsumeEnergy(playerEnergyCost);

        if (isFailed)
        {
            // 失败惩罚，掉心情
            state.Uma.AddMood(-10);
            return TrainingResult.Failed;
        }

        // 成功奖励：解析 StatsRewards 字典并应用
        foreach (var kvp in training.StatsRewards)
        {
            int val = kvp.Value;
            // 陪伴提供五维基础成长加成
            if (isAccompanied && (kvp.Key == "Uma.Speed" || kvp.Key == "Uma.Stamina" || kvp.Key == "Uma.Power" || kvp.Key == "Uma.Guts" || kvp.Key == "Uma.Intelligence"))
            {
                val += 5;
            }
            registry.ApplyStatChange(kvp.Key, val, state);
        }

        // 应用 CustomStatsRewards 自定义属性增减
        if (training.CustomStatsRewards != null)
        {
            foreach (var kvp in training.CustomStatsRewards)
            {
                registry.ApplyStatChange(kvp.Key, kvp.Value, state);
            }
        }
        
        if (isAccompanied) state.Uma.Affection += 5;

        // 触发行为包 Hook
        registry.TriggerHook("OnTraining", state);
        registry.TriggerHook($"OnTraining_{training.Id}", state);

        return TrainingResult.Success;
    }

    private float CalculateFailureRate(int currentActionStamina, int maxActionStamina, int currentEnergy, int maxEnergy)
    {
        float staminaRatio = (float)currentActionStamina / maxActionStamina;
        float energyRatio = (float)currentEnergy / maxEnergy;

        // 基础失败率由马娘行动体力决定
        float baseRate = staminaRatio switch
        {
            > 0.6f => 0.0f,
            > 0.4f => 0.1f,
            > 0.2f => 0.3f,
            _ => 0.8f
        };

        // 精力影响：精力低于 50% 时额外增加失败率
        if (energyRatio < 0.2f) baseRate += 0.4f;
        else if (energyRatio < 0.5f) baseRate += 0.15f;

        return Mathf.Clamp(baseRate, 0.0f, 0.95f);
    }
}

public enum TrainingResult
{
    Success,
    Failed,
    InsufficientStamina,
    InsufficientTrainerEnergy
}

public enum TrainingType
{
    Speed,
    Stamina,
    Power,
    Guts,
    Intelligence
}
