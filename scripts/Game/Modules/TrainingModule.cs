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
    public bool ExecuteTraining(GameState state, TrainingType type)
    {
        int staminaCost = GetStaminaCost(type);
        
        // 体力不足判断
        if (state.Player.Stamina < staminaCost && staminaCost > 0)
        {
            return false;
        }

        // 失败率计算
        float failureRate = CalculateFailureRate(state.Player.Stamina, state.Player.MaxStamina);
        bool isFailed = _rng.Randf() < failureRate;

        // 扣减体力
        state.Player.ConsumeStamina(staminaCost);

        if (isFailed)
        {
            // 失败惩罚，例如掉精力
            state.Player.AddEnergy(-10);
            return false;
        }

        // 成功奖励
        ApplyTrainingRewards(state, type);
        return true;
    }

    private int GetStaminaCost(TrainingType type)
    {
        // 智力训练恢复极少体力 (负数表示回复) 其他消耗较多
        if (type == TrainingType.Intelligence) return -5; 
        return 20;
    }

    private float CalculateFailureRate(int currentStamina, int maxStamina)
    {
        float ratio = (float)currentStamina / maxStamina;
        if (ratio > 0.6f) return 0.0f;
        if (ratio > 0.4f) return 0.1f;
        if (ratio > 0.2f) return 0.3f;
        return 0.8f;
    }

    private void ApplyTrainingRewards(GameState state, TrainingType type)
    {
        switch (type)
        {
            case TrainingType.Speed:
                state.Uma.AddStat(StatType.Speed, 10);
                state.Uma.AddStat(StatType.Power, 5);
                state.Uma.SkillPoints += 2;
                break;
            case TrainingType.Stamina:
                state.Uma.AddStat(StatType.Stamina, 10);
                state.Uma.AddStat(StatType.Guts, 5);
                state.Uma.SkillPoints += 2;
                break;
            case TrainingType.Power:
                state.Uma.AddStat(StatType.Power, 10);
                state.Uma.AddStat(StatType.Stamina, 5);
                state.Uma.SkillPoints += 2;
                break;
            case TrainingType.Guts:
                state.Uma.AddStat(StatType.Guts, 10);
                state.Uma.AddStat(StatType.Speed, 5);
                state.Uma.AddStat(StatType.Power, 5);
                state.Uma.SkillPoints += 2;
                break;
            case TrainingType.Intelligence:
                state.Uma.AddStat(StatType.Intelligence, 10);
                state.Uma.AddStat(StatType.Speed, 2);
                state.Uma.SkillPoints += 5;
                state.Player.AddEnergy(5);
                break;
        }
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
