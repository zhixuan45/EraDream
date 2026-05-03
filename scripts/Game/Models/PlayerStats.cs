using Godot;
using System.Text.Json.Serialization;

namespace umaEraArchive.Game;

/// <summary>
/// 用于管理训练员（玩家）的资源属性
/// </summary>
public class PlayerStats
{
    [JsonPropertyName("player_name")]
    public string PlayerName { get; set; } = "训练员";

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

    public void AddMoney(int amount)
    {
        Money += amount;
    }

    public bool ConsumeMoney(int amount)
    {
        if (Money >= amount)
        {
            Money -= amount;
            return true;
        }
        return false;
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

    public bool ConsumeEnergy(int amount)
    {
        if (Energy >= amount)
        {
            Energy -= amount;
            return true;
        }
        return false;
    }
}
