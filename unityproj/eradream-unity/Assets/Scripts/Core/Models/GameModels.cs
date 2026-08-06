using System;
using System.Collections.Generic;

namespace EraDream.Core.Models
{
    // 马娘/角色养成面板属性
    public class UmaStats
    {
        public int Speed { get; set; } = 100;
        public int Stamina { get; set; } = 100;
        public int Power { get; set; } = 100;
        public int Guts { get; set; } = 100;
        public int Wisdom { get; set; } = 100;
        public int SkillPoints { get; set; } = 0;
        public int Energy { get; set; } = 100;
        public int MaxEnergy { get; set; } = 100;
        public int Motivation { get; set; } = 3; // 1-5 阶段 (绝不佳, 差, 普通, 良好, 绝佳)
    }

    // 玩家/训练员面板数据
    public class PlayerStats
    {
        public string PlayerName { get; set; } = "Trainer";
        public int Money { get; set; } = 1000;
        public int Turn { get; set; } = 1;
        public int MaxTurns { get; set; } = 72;
    }

    // 道具数据项
    public class ItemModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Count { get; set; } = 0;
        public string IconPath { get; set; } = "";
    }

    // 养成游戏总体运行时状态镜像
    public class SimulationGameState
    {
        public PlayerStats Player { get; set; } = new PlayerStats();
        public UmaStats Uma { get; set; } = new UmaStats();
        public List<ItemModel> Inventory { get; set; } = new List<ItemModel>();
        public Dictionary<string, string> Flags { get; set; } = new Dictionary<string, string>();
    }
}
