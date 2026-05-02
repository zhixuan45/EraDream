using System.Text.Json.Serialization;

namespace UmaEraArchive.Core.Extensions
{
    public enum PackType
    {
        [JsonPropertyName("character")]
        Character,
        
        [JsonPropertyName("gameplay")]
        Gameplay
    }

    public class ExtensionManifest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("type")]
        public PackType Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("min_game_version")]
        public string MinGameVersion { get; set; }
    }
}
