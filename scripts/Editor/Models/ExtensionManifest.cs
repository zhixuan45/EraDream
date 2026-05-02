using System.Text.Json.Serialization;

namespace UmaEraArchive.Editor.Models
{
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
        public string Type { get; set; } = "character"; // "character" or "gameplay"

        [JsonPropertyName("description")]
        public string Description { get; set; } = "添加了一个具有全新数值逻辑的角色。";

        [JsonPropertyName("min_game_version")]
        public string MinGameVersion { get; set; } = "0.5.0";
    }
}
