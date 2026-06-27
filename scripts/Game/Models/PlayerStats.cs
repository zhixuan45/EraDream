using Godot;
using System.Text.Json.Serialization;

namespace EraDream.Game;

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
        // 采用 long 进行计算以避免金额加法溢出
        long newMoney = (long)Money + amount;
        Money = (int)System.Math.Clamp(newMoney, 0, (long)int.MaxValue);
    }

    public bool ConsumeMoney(int amount)
    {
        if (amount < 0)
        {
            AddMoney(-amount);
            return true;
        }
        if (Money >= amount)
        {
            Money -= amount;
            return true;
        }
        return false;
    }
    
    public bool ConsumeStamina(int amount)
    {
        if (amount < 0)
        {
            AddStamina(-amount);
            return true;
        }
        if (Stamina >= amount)
        {
            Stamina -= amount;
            return true;
        }
        return false;
    }

    public bool ConsumeEnergy(int amount)
    {
        if (amount < 0)
        {
            AddEnergy(-amount);
            return true;
        }
        if (Energy >= amount)
        {
            Energy -= amount;
            return true;
        }
        return false;
    }
}
