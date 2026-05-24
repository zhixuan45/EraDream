using Godot;
using System.Text.Json.Serialization;

namespace umaEraArchive.Game;

/// <summary>
/// 用于管理马娘的各项能力数值
/// </summary>
public class UmaStats
{
    // 心情状态 5 阶段
    public enum MoodStage
    {
        Terrible = 0, // 绝差
        Poor = 1,     // 差
        Normal = 2,   // 普通
        Good = 3,     // 好
        Excellent = 4 // 绝好
    }

    // 养成用实时资源
    [JsonPropertyName("mood")]
    public int Mood { get; set; } = 75; // 默认普通 (75/150)

    [JsonPropertyName("action_stamina")]
    public int ActionStamina { get; set; } = 100;

    [JsonPropertyName("max_action_stamina")]
    public int MaxActionStamina { get; set; } = 100;

    [JsonPropertyName("energy")]
    public int Energy { get; set; } = 100;

    [JsonPropertyName("max_energy")]
    public int MaxEnergy { get; set; } = 100;

    [JsonIgnore]
    public MoodStage CurrentMoodStage => Mood switch
    {
        >= 130 => MoodStage.Excellent,
        >= 100 => MoodStage.Good,
        >= 50 => MoodStage.Normal,
        >= 20 => MoodStage.Poor,
        _ => MoodStage.Terrible
    };

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

    // 动态扩展属性字典，用于剧本包自定义数值（如：粉丝数、疲劳值）
    [JsonPropertyName("custom_stats")]
    public System.Collections.Generic.Dictionary<string, int> CustomStats { get; set; } = new();

    // 最大属性上限
    public const int MaxStatValue = 1200;

    public void AddStat(StatType type, int amount)
    {
        switch (type)
        {
            case StatType.Speed: Speed = Mathf.Clamp(Speed + amount, 0, MaxStatValue); break;
            case StatType.Stamina: Stamina = Mathf.Clamp(Stamina + amount, 0, MaxStatValue); break;
            case StatType.Power: Power = Mathf.Clamp(Power + amount, 0, MaxStatValue); break;
            case StatType.Guts: Guts = Mathf.Clamp(Guts + amount, 0, MaxStatValue); break;
            case StatType.Intelligence: Intelligence = Mathf.Clamp(Intelligence + amount, 0, MaxStatValue); break;
        }
    }

    public void AddActionStamina(int amount)
    {
        ActionStamina = Mathf.Clamp(ActionStamina + amount, 0, MaxActionStamina);
    }

    public void AddEnergy(int amount)
    {
        Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
    }

    public bool ConsumeActionStamina(int amount)
    {
        if (ActionStamina >= amount)
        {
            ActionStamina -= amount;
            return true;
        }
        return false;
    }

    public bool ConsumeEnergy(int amount)
    {
        if (Energy >= amount)
        {
            Energy -= amount;
            return true;
        }
        return false;
    }

    public void AddMood(int amount)
    {
        Mood = Mathf.Clamp(Mood + amount, 0, 150);
    }

    /// <summary>
    /// 增加自定义属性值
    /// </summary>
    /// <param name="id">必须是包含命名空间的唯一ID，如 "mod:fan_count"</param>
    public void AddCustomStat(string id, int amount)
    {
        if (!CustomStats.ContainsKey(id))
        {
            CustomStats[id] = 0;
        }
        CustomStats[id] += amount;
    }

    public int GetCustomStat(string id)
    {
        return CustomStats.TryGetValue(id, out int val) ? val : 0;
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
