using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using EraDream.Game.Models;

namespace EraDream.Core.Extensions;

public class BehaviorPack
{
    [JsonPropertyName("rules")]
    public List<BehaviorRule> Rules { get; set; } = new();

    [JsonPropertyName("items")]
    public List<ItemDefinition> Items { get; set; } = new();

    [JsonPropertyName("menus")]
    public List<UIMenuDefinition> Menus { get; set; } = new();

    [JsonPropertyName("races")]
    public List<RaceDefinition> Races { get; set; } = new();

    [JsonPropertyName("trainings")]
    public List<TrainingDefinition> Trainings { get; set; } = new();
}

public class UIMenuDefinition
{
    [JsonPropertyName("menu_id")]
    public string MenuId { get; set; } = "";

    [JsonPropertyName("options")]
    public List<UIOption> Options { get; set; } = new();
}

public class UIOption
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "";

    [JsonPropertyName("action")]
    public BehaviorAction Action { get; set; } = new();

    [JsonPropertyName("conditions")]
    public List<BehaviorCondition> Conditions { get; set; } = new();

    [JsonPropertyName("override")]
    public bool Override { get; set; } = false;
}

public class BehaviorRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("hook")]
    public string Hook { get; set; } = "";

    [JsonPropertyName("conditions")]
    public List<BehaviorCondition> Conditions { get; set; } = new();

    [JsonPropertyName("probability")]
    public float Probability { get; set; } = 1.0f;

    [JsonPropertyName("action")]
    public BehaviorAction Action { get; set; } = new();

    [JsonPropertyName("override")]
    public bool Override { get; set; } = false;
}

public class BehaviorCondition
{
    [JsonPropertyName("property")]
    public string Property { get; set; } = ""; // e.g. "Player.Money", "Uma.Affection", "Variable:fan_count"

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "=="; // "==", "!=", ">", "<", ">=", "<="

    [JsonPropertyName("value")]
    public string Value { get; set; } = "0";
}

public class BehaviorAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "DetailedStory"; // "BriefStory", "DetailedStory", "ChangeStat"

    [JsonPropertyName("path")]
    public string Path { get; set; } = ""; // res:// or extension relative path

    [JsonPropertyName("target_property")]
    public string TargetProperty { get; set; } = ""; // Used for ChangeStat

    [JsonPropertyName("value_change")]
    public string ValueChange { get; set; } = "0"; // Used for ChangeStat
}

public class RaceDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("turn")]
    public int Turn { get; set; } = 1;

    [JsonPropertyName("min_speed")]
    public int MinSpeed { get; set; } = 0;

    [JsonPropertyName("reward_stat")]
    public string RewardStat { get; set; } = "";

    [JsonPropertyName("reward_value")]
    public int RewardValue { get; set; } = 0;

    [JsonPropertyName("override")]
    public bool Override { get; set; } = false;
}

public class TrainingDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("stamina_cost")]
    public int StaminaCost { get; set; } = 0;

    [JsonPropertyName("energy_cost")]
    public int EnergyCost { get; set; } = 0;

    [JsonPropertyName("min_stamina")]
    public int MinStamina { get; set; } = 0;

    [JsonPropertyName("stats_rewards")]
    public Dictionary<string, int> StatsRewards { get; set; } = new();

    [JsonPropertyName("custom_stats_rewards")]
    public Dictionary<string, int> CustomStatsRewards { get; set; } = new();

    [JsonPropertyName("override")]
    public bool Override { get; set; } = false;
}
