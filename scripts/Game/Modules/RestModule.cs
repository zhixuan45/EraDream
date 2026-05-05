using Godot;

namespace umaEraArchive.Game;

/// <summary>
/// 处理休息指令和状态恢复逻辑
/// </summary>
public partial class RestModule : Node
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();
    }

    /// <summary>
    /// 执行休息操作，恢复体力与微量精力
    /// </summary>
    public bool ExecuteRest(GameState state)
    {
        if (string.IsNullOrEmpty(state.ActiveUmaId))
        {
            // 无马娘状态：随机回复训练员一定量体力
            int healAmount = _rng.RandiRange(50, 70);
            state.Player.AddStamina(healAmount);

            // 休息也可以微量恢复精力
            state.Player.AddEnergy(10);
            return true;
        }
        else
        {
            // 有马娘状态：消耗训练员精力，回复马娘行动体力
            int energyCost = 20;
            if (state.Player.Energy < energyCost)
            {
                return false;
            }

            state.Player.ConsumeEnergy(energyCost);

            // 回复马娘行动力
            int healAmount = 50;
            state.Uma.AddActionStamina(healAmount);
            return true;
        }
    }
}
