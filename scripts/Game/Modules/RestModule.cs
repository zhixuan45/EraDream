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
    public void ExecuteRest(GameState state)
    {
        // 随机回复一定量体力
        int healAmount = _rng.RandiRange(50, 70);
        state.Player.AddStamina(healAmount);
        
        // 休息也可以微量恢复精力
        state.Player.AddEnergy(10);
    }
}
