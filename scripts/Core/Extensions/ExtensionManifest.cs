using System.Collections.Generic;
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

        [JsonPropertyName("dependencies")]
        public List<DependencyInfo> Dependencies { get; set; } = new();

        [JsonPropertyName("overrides")]
        public List<OverrideRule> Overrides { get; set; } = new();

        [JsonPropertyName("nested_packages")]
        public List<string> NestedPackages { get; set; } = new();

        /// <summary>
        /// 运行时检测到的权限列表（不序列化）
        /// </summary>
        [JsonIgnore]
        public List<string> DetectedPermissions { get; set; } = new();

        /// <summary>
        /// 是否包含风险权限（不序列化）
        /// </summary>
        [JsonIgnore]
        public bool IsRisky => DetectedPermissions.Count > 0;

        /// <summary>
        /// 用户是否已授权运行（针对高危包，不序列化）
        /// </summary>
        [JsonIgnore]
        public bool IsAuthorized { get; set; } = false;
    }
}
