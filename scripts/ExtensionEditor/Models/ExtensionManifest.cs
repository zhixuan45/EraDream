using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EraDream.Editor.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PackType
    {
        Character,
        Gameplay
    }

    public class DependencyInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    public class OverrideRule
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } // "resource", "variable", "behavior"

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("target")]
        public string Target { get; set; }

        [JsonPropertyName("strategy")]
        public string Strategy { get; set; } // "replace", "append", "merge"
    }

    // 编辑器所用的扩展包清单模型，与核心运行时保持结构一致
    public class ExtensionManifest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "com.example.new_uma";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "传说中的马娘包";

        [JsonPropertyName("author")]
        public string Author { get; set; } = "ExampleAuthor";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("type")]
        public PackType Type { get; set; } = PackType.Character;

        [JsonPropertyName("description")]
        public string Description { get; set; } = "添加了一个具有全新数值逻辑的角色。";

        [JsonPropertyName("min_game_version")]
        public string MinGameVersion { get; set; } = "0.5.0";

        [JsonPropertyName("dependencies")]
        public List<DependencyInfo> Dependencies { get; set; } = new();

        [JsonPropertyName("overrides")]
        public List<OverrideRule> Overrides { get; set; } = new();

        [JsonPropertyName("nested_packages")]
        public List<string> NestedPackages { get; set; } = new();
    }
}
