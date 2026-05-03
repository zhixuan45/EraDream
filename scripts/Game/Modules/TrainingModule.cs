using Godot;
using System;

namespace umaEraArchive.Game;

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
    }

    /// <summary>
    /// 执行一次训练动作
    /// </summary>
    public bool ExecuteTraining(GameState state, TrainingType type, bool isAccompanied = false)
    {
        int actionStaminaCost = GetActionStaminaCost(type);
        int playerEnergyCost = isAccompanied ? 15 : 0;
        
        // 马娘行动体力不足判断
        if (state.Uma.ActionStamina < actionStaminaCost && actionStaminaCost > 0)
        {
            return false;
        }

        // 训练员精力不足判断
        if (isAccompanied && state.Player.Energy < playerEnergyCost)
        {
            return false;
        }

        // 失败率计算：考虑马娘 ActionStamina 和训练员 Energy
        float failureRate = CalculateFailureRate(state.Uma.ActionStamina, state.Uma.MaxActionStamina, state.Player.Energy, state.Player.MaxEnergy);
        bool isFailed = _rng.Randf() < failureRate;

        // 扣减资源
        state.Uma.ConsumeActionStamina(actionStaminaCost);
        if (isAccompanied) state.Player.ConsumeEnergy(playerEnergyCost);

        if (isFailed)
        {
            // 失败惩罚，掉心情
            state.Uma.AddMood(-10);
            return false;
        }

        // 成功奖励
        ApplyTrainingRewards(state, type, isAccompanied);

        // 触发行为包 Hook
        if (UmaEraArchive.Core.Extensions.BehaviorRegistry.Instance != null)
        {
            UmaEraArchive.Core.Extensions.BehaviorRegistry.Instance.TriggerHook("OnTraining", state);
        }

        return true;
    }

    private int GetActionStaminaCost(TrainingType type)
    {
        // 智力训练恢复极少体力 (负数表示回复) 其他消耗较多
        if (type == TrainingType.Intelligence) return -5; 
        return 20;
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

    private void ApplyTrainingRewards(GameState state, TrainingType type, bool isAccompanied)
    {
        // 陪伴训练获得额外属性
        int bonus = isAccompanied ? 5 : 0;

        switch (type)
        {
            case TrainingType.Speed:
                state.Uma.AddStat(StatType.Speed, 10 + bonus);
                state.Uma.AddStat(StatType.Power, 5);
                state.Uma.SkillPoints += 2;
                break;
            case TrainingType.Stamina:
                state.Uma.AddStat(StatType.Stamina, 10 + bonus);
                state.Uma.AddStat(StatType.Guts, 5);
                state.Uma.SkillPoints += 2;
                break;
            case TrainingType.Power:
                state.Uma.AddStat(StatType.Power, 10 + bonus);
                state.Uma.AddStat(StatType.Stamina, 5);
                state.Uma.SkillPoints += 2;
                break;
            case TrainingType.Guts:
                state.Uma.AddStat(StatType.Guts, 10 + bonus);
                state.Uma.AddStat(StatType.Speed, 5);
                state.Uma.AddStat(StatType.Power, 5);
                state.Uma.SkillPoints += 2;
                break;
            case TrainingType.Intelligence:
                state.Uma.AddStat(StatType.Intelligence, 10 + bonus);
                state.Uma.AddStat(StatType.Speed, 2);
                state.Uma.SkillPoints += 5;
                state.Player.AddEnergy(5);
                break;
        }
        
        if (isAccompanied) state.Uma.Affection += 5;
    }
}

public enum TrainingType
{
    Speed,
    Stamina,
    Power,
    Guts,
    Intelligence
}
