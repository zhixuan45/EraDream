using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UmaEraArchive.Core.Models
{
    /// <summary>
    /// 马娘养成数值配置 (Data/simulation.json)
    /// </summary>
    public class SimulationData
    {
        [JsonPropertyName("identity")]
        public CharacterIdentity Identity { get; set; } = new();

        [JsonPropertyName("stats")]
        public SimulationStats Stats { get; set; } = new();
    }

    public class CharacterIdentity
    {
        [JsonPropertyName("internal_id")]
        public string InternalId { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }

        [JsonPropertyName("personality_id")]
        public string PersonalityId { get; set; }
    }

    public class SimulationStats
    {
        [JsonPropertyName("initial")]
        public Dictionary<string, int> Initial { get; set; } = new();

        [JsonPropertyName("conditions")]
        public Dictionary<string, int> Conditions { get; set; } = new();

        [JsonPropertyName("growth_bonus")]
        public Dictionary<string, float> GrowthBonus { get; set; } = new();

        [JsonPropertyName("custom_stats")]
        public Dictionary<string, int> CustomStats { get; set; } = new();
    }

    /// <summary>
    /// 角色剧情表现配置 (Data/actor_config.json)
    /// </summary>
    public class ActorConfigData
    {
        [JsonPropertyName("actor_id")]
        public string ActorId { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("visuals")]
        public ActorVisuals Visuals { get; set; } = new();

        [JsonPropertyName("barks")]
        public List<BarkData> Barks { get; set; } = new();

        [JsonPropertyName("audio")]
        public ActorAudio Audio { get; set; } = new();
    }

    public class ActorVisuals
    {
        [JsonPropertyName("default_sprite")]
        public string DefaultSprite { get; set; }

        [JsonPropertyName("expressions")]
        public Dictionary<string, string> Expressions { get; set; } = new();

        [JsonPropertyName("stickers")]
        public Dictionary<string, StickerConfig> Stickers { get; set; } = new();
    }

    public class StickerConfig
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("offset")]
        public float[] Offset { get; set; } = new float[2];

        [JsonPropertyName("scale")]
        public float Scale { get; set; } = 1.0f;
    }

    public class BarkData
    {
        [JsonPropertyName("condition")]
        public string Condition { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("expression")]
        public string Expression { get; set; }

        [JsonPropertyName("sticker")]
        public string Sticker { get; set; }

        [JsonPropertyName("voice")]
        public string Voice { get; set; }
    }

    public class ActorAudio
    {
        [JsonPropertyName("typing_sound")]
        public string TypingSound { get; set; }

        [JsonPropertyName("fallback_voices")]
        public List<string> FallbackVoices { get; set; } = new();
    }
}
