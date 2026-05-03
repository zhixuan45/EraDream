using Godot;
using System;
using UmaEraArchive.Core.Extensions;

namespace umaEraArchive.Game;

/// <summary>
/// 负责处理外出功能
/// 增加马娘心情，触发行为包 Hook。
/// </summary>
public partial class OutingModule : Node
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();
    }

    /// <summary>
    /// 执行一次外出动作
    /// </summary>
    public bool ExecuteOuting(GameState state)
    {
        // 基础消耗
        int staminaCost = 15;
        int energyCost = 10;

        // 资源判断
        if (state.Player.Stamina < staminaCost || state.Player.Energy < energyCost)
        {
            return false;
        }

        // 扣减资源
        state.Player.ConsumeStamina(staminaCost);
        state.Player.AddEnergy(-energyCost);

        // 心情提升 (0-150)
        int currentMood = state.Uma.Mood;
        int gain = _rng.RandiRange(20, 40);
        state.Uma.Mood = Mathf.Clamp(currentMood + gain, 0, 150);

        GD.Print($"[OutingModule] Outing executed. Mood: {currentMood} -> {state.Uma.Mood}");

        // 埋入 Behavior Hook
        if (BehaviorRegistry.Instance != null)
        {
            BehaviorRegistry.Instance.TriggerHook("OnOuting", state);
        }

        return true;
    }
}
