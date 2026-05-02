using Godot;
using System.Text.Json.Serialization;

namespace umaEraArchive.Game;

/// <summary>
/// 作为整个养成状态的包装器，聚合属性并提供序列化基础
/// </summary>
public class GameState
{
    [JsonPropertyName("current_turn")]
    public int CurrentTurn { get; set; } = 1;

    [JsonPropertyName("max_turns")]
    public int MaxTurns { get; set; } = 72; // 默认3年，每年24个回合（半月一回合）相当于72回合

    [JsonPropertyName("scenario_paths")]
    public System.Collections.Generic.List<string> ScenarioPaths { get; set; } = new System.Collections.Generic.List<string>();

    [JsonPropertyName("character_paths")]
    public System.Collections.Generic.List<string> CharacterPaths { get; set; } = new System.Collections.Generic.List<string>();

    [JsonPropertyName("mod_paths")]
    public System.Collections.Generic.List<string> ModPaths { get; set; } = new System.Collections.Generic.List<string>();

    [JsonPropertyName("player_stats")]
    public PlayerStats Player { get; set; } = new PlayerStats();

    [JsonPropertyName("uma_stats")]
    public UmaStats Uma { get; set; } = new UmaStats();

    public bool IsGameOver => CurrentTurn > MaxTurns;

    public void NextTurn()
    {
        CurrentTurn++;
    }
}
