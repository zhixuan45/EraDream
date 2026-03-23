using Godot;
using System.Text.Json.Serialization;

namespace umaEraArchive.Game;

/// <summary>
/// 用于管理马娘的各项能力数值
/// </summary>
public partial class UmaStats : RefCounted
{
    // 五维属性
    [JsonPropertyName("speed")]
    public int Speed { get; set; } = 1;

    [JsonPropertyName("stamina")]
    public int Stamina { get; set; } = 1;

    [JsonPropertyName("power")]
    public int Power { get; set; } = 1;

    [JsonPropertyName("guts")]
    public int Guts { get; set; } = 1;

    [JsonPropertyName("intelligence")]
    public int Intelligence { get; set; } = 1;

    // 技能点
    [JsonPropertyName("skill_points")]
    public int SkillPoints { get; set; } = 0;

    // 好感度/羁绊
    [JsonPropertyName("affection")]
    public int Affection { get; set; } = 0;

    // 最大属性上限
    public const int MaxStatValue = 1200;

    public void AddStat(StatType type, int amount)
    {
        switch (type)
        {
            case StatType.Speed: Speed = Mathf.Min(Speed + amount, MaxStatValue); break;
            case StatType.Stamina: Stamina = Mathf.Min(Stamina + amount, MaxStatValue); break;
            case StatType.Power: Power = Mathf.Min(Power + amount, MaxStatValue); break;
            case StatType.Guts: Guts = Mathf.Min(Guts + amount, MaxStatValue); break;
            case StatType.Intelligence: Intelligence = Mathf.Min(Intelligence + amount, MaxStatValue); break;
        }
    }
}

public enum StatType
{
    Speed,
    Stamina,
    Power,
    Guts,
    Intelligence
}
