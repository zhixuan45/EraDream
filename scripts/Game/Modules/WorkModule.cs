using Godot;

namespace EraDream.Game;

/// <summary>
/// 处理打工逻辑，消耗训练员体力与精力获取金钱
/// </summary>
public partial class WorkModule : Node
{
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _rng.Randomize();
    }

    /// <summary>
    /// 执行打工操作
    /// </summary>
    public bool ExecuteWork(GameState state)
    {
        int staminaCost = 30;
        int energyCost = 20;

        if (state.Player.Stamina < staminaCost || state.Player.Energy < energyCost)
        {
            return false;
        }

        state.Player.ConsumeStamina(staminaCost);
        state.Player.ConsumeEnergy(energyCost);

        // 基础收益 200~300
        int reward = _rng.RandiRange(200, 300);
        state.Player.AddMoney(reward);

        return true;
    }
}
