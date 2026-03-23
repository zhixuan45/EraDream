using Godot;
using System.Text.Json.Serialization;

namespace umaEraArchive.Game;

/// <summary>
/// 用于管理训练员（玩家）的资源属性
/// </summary>
public partial class PlayerStats : RefCounted
{
    [JsonPropertyName("money")]
    public int Money { get; set; } = 0;

    [JsonPropertyName("stamina")]
    public int Stamina { get; set; } = 100;

    [JsonPropertyName("max_stamina")]
    public int MaxStamina { get; set; } = 100;

    [JsonPropertyName("energy")]
    public int Energy { get; set; } = 100;

    [JsonPropertyName("max_energy")]
    public int MaxEnergy { get; set; } = 100;

    public void AddStamina(int amount)
    {
        Stamina = Mathf.Clamp(Stamina + amount, 0, MaxStamina);
    }

    public void AddEnergy(int amount)
    {
        Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
    }
    
    public bool ConsumeStamina(int amount)
    {
        if (Stamina >= amount)
        {
            Stamina -= amount;
            return true;
        }
        return false;
    }
}
