using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace umaEraArchive.Game.Models;

/// <summary>
/// 物品类型定义
/// </summary>
public enum ItemType
{
    Consumable, // 一次性消耗品
    Duration,   // 持续性效果（多回合）
    Permanent   // 长期持有（被动）
}

/// <summary>
/// 物品的基础定义（通常由行为包提供）
/// </summary>
public class ItemDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("icon")]
    public string IconPath { get; set; } = "";

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ItemType Type { get; set; } = ItemType.Consumable;

    [JsonPropertyName("max_stack")]
    public int MaxStack { get; set; } = 99;

    [JsonPropertyName("duration_turns")]
    public int DurationTurns { get; set; } = 0; // 仅对 Duration 类型有效

    [JsonPropertyName("price")]
    public int Price { get; set; } = 0; // 商店价格

    [JsonPropertyName("override")]
    public bool Override { get; set; } = false;
}

/// <summary>
/// 训练员背包状态数据
/// </summary>
public class InventoryState
{
    [JsonPropertyName("items")]
    public Dictionary<string, int> Items { get; set; } = new(); // ItemID -> Count

    [JsonPropertyName("active_effects")]
    public List<ActiveEffect> ActiveEffects { get; set; } = new(); // 正在生效的持续性物品
}

/// <summary>
/// 正在生效的持续性效果记录
/// </summary>
public class ActiveEffect
{
    [JsonPropertyName("item_id")]
    public string ItemId { get; set; } = "";

    [JsonPropertyName("remaining_turns")]
    public int RemainingTurns { get; set; } = 0;
}
