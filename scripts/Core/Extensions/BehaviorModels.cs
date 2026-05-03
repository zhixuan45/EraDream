using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using umaEraArchive.Game.Models;

namespace UmaEraArchive.Core.Extensions;

public class BehaviorPack
{
    [JsonPropertyName("rules")]
    public List<BehaviorRule> Rules { get; set; } = new();

    [JsonPropertyName("items")]
    public List<ItemDefinition> Items { get; set; } = new();
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
