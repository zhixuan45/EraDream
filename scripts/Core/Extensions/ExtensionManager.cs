using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EraDream.Core.Extensions
{
    /// <summary>
    /// 扩展包管理器，负责扫描、加载和资源挂载。
    /// 遵循严格隔离原则：马娘包禁止代码加载。
    /// </summary>
    public partial class ExtensionManager : Node
    {
        public static ExtensionManager Instance { get; private set; }

        private const string ExtDir = "user://extensions";
        private const string CacheDir = "user://cache/ext";

        public const string CurrentGameVersion = "1.0.0";

        // ZIP 解压限制常量
        private const long MaxExtractFileSize = 50 * 1024 * 1024;   // 单个文件 50MB
        private const long MaxExtractTotalSize = 200 * 1024 * 1024; // 总提取 200MB
        private const int MaxFileCount = 1000;                       // 最多 1000 个文件

        private Dictionary<string, ExtensionManifest> _loadedManifests = new();
        private Dictionary<string, string> _extensionPaths = new(); // ID -> Global Path
        private List<string> _activeExtensionIds = new();

        private int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1)) v1 = "0.0.0";
            if (string.IsNullOrEmpty(v2)) v2 = "0.0.0";

            var p1 = v1.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
            var p2 = v2.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();

            for (int i = 0; i < Math.Max(p1.Length, p2.Length); i++)
            {
                int val1 = i < p1.Length ? p1[i] : 0;
                int val2 = i < p2.Length ? p2[i] : 0;
                if (val1 != val2) return val1.CompareTo(val2);
            }
            return 0;
        }

        public bool ValidateManifest(ExtensionManifest manifest)
        {
            if (manifest == null) return false;
            if (string.IsNullOrWhiteSpace(manifest.Id)) return false;
            if (string.IsNullOrWhiteSpace(manifest.Name)) return false;

            // 拦截包含路径穿越的恶意 ID
            if (manifest.Id.Contains("..") || manifest.Id.Contains("/") || manifest.Id.Contains("\\"))
            {
                GD.PrintErr($"[ExtensionManager] Unsafe extension ID: '{manifest.Id}'");
                return false;
            }
            return true;
        }

        public override void _EnterTree()
        {
            if (Instance == null) Instance = this;
            EnsureDirectories();
        }

        // 启动时自动扫描并激活所有可用扩展以供手动测试
        public override void _Ready()
        {
            ScanExtensions();
            AutoActivateAllAvailable();
        }

        private void AutoActivateAllAvailable()
        {
            foreach (var ext in GetAvailableExtensions())
            {
                GD.Print($"[ExtensionManager] Auto-activating extension: {ext.Id}");
                _ = ActivateExtension(ext.Id);
            }
        }

        private void EnsureDirectories()
        {
            using var dir = DirAccess.Open("user://");
            if (!dir.DirExists(ExtDir)) dir.MakeDirRecursive(ExtDir);
            if (!dir.DirExists(CacheDir)) dir.MakeDirRecursive(CacheDir);
        }

        /// <summary>
        /// 扫描扩展文件夹，读取 manifest.json
        /// </summary>
        public void ScanExtensions()
        {
            _loadedManifests.Clear();
            _extensionPaths.Clear();
            string globalExtDir = ProjectSettings.GlobalizePath(ExtDir);
            
            if (!Directory.Exists(globalExtDir)) return;

            // 1. 扫描文件夹形式的扩展 (开发调试用)
            foreach (var dirPath in Directory.GetDirectories(globalExtDir))
            {
                string manifestPath = Path.Combine(dirPath, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    var manifest = LoadManifestFromFile(manifestPath);
                    if (ValidateManifest(manifest))
                    {
                        _extensionPaths[manifest.Id] = dirPath;
                        ProcessManifest(manifest, dirPath);
                    }
                }
            }

            // 2. 扫描 .umaext 压缩包
            foreach (var filePath in Directory.GetFiles(globalExtDir, "*.umaext"))
            {
                var manifest = LoadManifestFromArchive(filePath);
                if (ValidateManifest(manifest))
                {
                    _extensionPaths[manifest.Id] = filePath;
                    ProcessManifestArchive(manifest, filePath);
                    GD.Print($"[ExtensionManager] Found archived extension: {manifest.Name} ({manifest.Id})");
                }
            }
        }

        private void ProcessManifestArchive(ExtensionManifest manifest, string archivePath)
        {
            _loadedManifests[manifest.Id] = manifest;
        }

        private void ProcessManifest(ExtensionManifest manifest, string rootPath)
        {
            _loadedManifests[manifest.Id] = manifest;
        }

        private ExtensionManifest LoadManifestFromFile(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                return JsonSerializer.Deserialize<ExtensionManifest>(json, options);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ExtensionManager] Failed to load manifest {path}: {ex.Message}");
                return null;
            }
        }

        private ExtensionManifest LoadManifestFromArchive(string path)
        {
            using var reader = new ZipReader();
            if (reader.Open(path) != Error.Ok)
            {
                GD.PrintErr($"[ExtensionManager] Failed to open archive: {path}");
                return null;
            }

            if (!reader.FileExists("manifest.json"))
            {
                GD.PrintErr($"[ExtensionManager] Archive missing manifest.json: {path}");
                return null;
            }

            byte[] data = reader.ReadFile("manifest.json");
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                return JsonSerializer.Deserialize<ExtensionManifest>(data, options);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ExtensionManager] Failed to parse manifest from archive {path}: {ex.Message}");
                return null;
            }
        }

        private bool ExtractArchive(string archivePath, string targetDir)
        {
            using var reader = new ZipReader();
            if (reader.Open(archivePath) != Error.Ok) return false;

            string fullTargetDir = Path.GetFullPath(targetDir);

            if (!Directory.Exists(fullTargetDir))
            {
                Directory.CreateDirectory(fullTargetDir);
            }

            long totalExtracted = 0;
            int fileCount = 0;

            foreach (string file in reader.GetFiles())
            {
                // 处理目录
                string targetPath = Path.GetFullPath(Path.Combine(fullTargetDir, file));

                // 防止 Zip Slip 攻击：拒绝包含路径穿越的条目
                if (file.Contains("..") || Path.IsPathRooted(file))
                {
                    GD.PrintErr($"[ExtensionManager] Rejected unsafe archive entry: {file}");
                    continue;
                }

                // 验证解压路径必须在目标目录内
                if (!targetPath.StartsWith(fullTargetDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                    !targetPath.Equals(fullTargetDir, StringComparison.OrdinalIgnoreCase))
                {
                    GD.PrintErr($"[ExtensionManager] Rejected path traversal entry: {file}");
                    continue;
                }

                if (file.EndsWith("/") || file.EndsWith("\\"))
                {
                    if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
                    continue;
                }

                if (++fileCount > MaxFileCount)
                {
                    GD.PrintErr($"[ExtensionManager] Archive contains too many files ({fileCount})");
                    return false;
                }

                // 确保 parent 目录存在
                string dirName = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);

                byte[] data = reader.ReadFile(file);
                if (data == null) continue;

                if (data.Length > MaxExtractFileSize)
                {
                    GD.PrintErr($"[ExtensionManager] File exceeds size limit: {file} ({data.Length} bytes)");
                    return false;
                }

                totalExtracted += data.Length;
                if (totalExtracted > MaxExtractTotalSize)
                {
                    GD.PrintErr($"[ExtensionManager] Total extraction size limit exceeded ({totalExtracted} bytes)");
                    return false;
                }

                File.WriteAllBytes(targetPath, data);
            }

            return true;
        }

        /// <summary>
        /// 激活特定的扩展包（按需解压）
        /// </summary>
        public async Task<bool> ActivateExtension(string id)
        {
            return await ActivateExtensionInternal(id, new HashSet<string>());
        }

        private async Task<bool> ActivateExtensionInternal(string id, HashSet<string> activating)
        {
            if (!_loadedManifests.ContainsKey(id)) return false;
            if (_activeExtensionIds.Contains(id)) return true;

            // 环检测
            if (!activating.Add(id))
            {
                GD.PrintErr($"[ExtensionManager] Circular dependency detected: {id}");
                return false;
            }

            await Task.Yield(); // 确保异步性

            var manifest = _loadedManifests[id];

            // 游戏版本校验
            if (!string.IsNullOrEmpty(manifest.MinGameVersion))
            {
                if (CompareVersions(CurrentGameVersion, manifest.MinGameVersion) < 0)
                {
                    GD.PrintErr($"[ExtensionManager] Extension '{id}' requires game version >= {manifest.MinGameVersion}, current is {CurrentGameVersion}");
                    activating.Remove(id);
                    return false;
                }
            }

            // 0. 递归激活依赖项，加入依赖版本号校验
            if (manifest.Dependencies != null)
            {
                foreach (var dep in manifest.Dependencies)
                {
                    if (!_loadedManifests.TryGetValue(dep.Id, out var depManifest))
                    {
                        GD.PrintErr($"[ExtensionManager] Missing dependency '{dep.Id}' for extension '{id}'");
                        activating.Remove(id);
                        return false;
                    }
                    if (!string.IsNullOrEmpty(dep.Version))
                    {
                        if (CompareVersions(depManifest.Version, dep.Version) < 0)
                        {
                            GD.PrintErr($"[ExtensionManager] Dependency '{dep.Id}' version ({depManifest.Version}) is lower than required ({dep.Version}) for extension '{id}'");
                            activating.Remove(id);
                            return false;
                        }
                    }

                    if (!await ActivateExtensionInternal(dep.Id, activating))
                    {
                        GD.PrintErr($"[ExtensionManager] Failed to activate dependency {dep.Id} for {id}");
                        activating.Remove(id);
                        return false;
                    }
                }
            }

            activating.Remove(id);

            GD.Print($"[ExtensionManager] Activating {id}...");

            // 1. 准备解压目录及安全检查
            if (string.IsNullOrEmpty(id) || id.Contains("..") || id.Contains("/") || id.Contains("\\"))
            {
                GD.PrintErr($"[ExtensionManager] Invalid extension id for activation: {id}");
                return false;
            }

            string baseCacheDir;
            string targetCache;
            try
            {
                baseCacheDir = Path.GetFullPath(ProjectSettings.GlobalizePath(CacheDir));
                targetCache = Path.GetFullPath(Path.Combine(baseCacheDir, id));
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ExtensionManager] Invalid characters in extension id {id}: {ex.Message}");
                return false;
            }

            // 验证 targetCache 必须在 baseCacheDir 内，防止路径穿越攻击
            if (!targetCache.StartsWith(baseCacheDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !targetCache.Equals(baseCacheDir, StringComparison.OrdinalIgnoreCase))
            {
                GD.PrintErr($"[ExtensionManager] Security violation: target cache path {targetCache} escapes base cache directory.");
                return false;
            }
            
            // 2. 解压逻辑：若是压缩包格式，自动解压至缓存目录
            bool isArchived = _extensionPaths.TryGetValue(id, out string extPath) && extPath.EndsWith(".umaext");
            if (isArchived)
            {
                if (!ExtractArchive(extPath, targetCache))
                {
                    GD.PrintErr($"[ExtensionManager] Failed to extract archive for {id}");
                    return false;
                }
            }

            // 确定扩展真正的根路径：压缩包在缓存中，文件夹包则是原地址
            string extensionRootPath = isArchived ? targetCache : extPath;

            // 应用合并规则（启用被忽略的 override rule 合并功能）
            ApplyManifestOverrides(manifest, extensionRootPath);

            // 加载行为包，传递关联 ID 以便进行增量卸载
            string behaviorPath = Path.Combine(extensionRootPath, "Logic", "behavior.json");
            if (BehaviorRegistry.Instance == null)
            {
                GD.PrintErr($"[ExtensionManager] BehaviorRegistry instance is not available. Cannot load behavior.json for {id}.");
            }
            else if (File.Exists(behaviorPath))
            {
                BehaviorRegistry.Instance.LoadBehaviorPack(behaviorPath, id);
                GD.Print($"[ExtensionManager] Behavior pack loaded for {id}");
            }

            _activeExtensionIds.Add(id);
            return true;
        }

        private void ApplyManifestOverrides(ExtensionManifest manifest, string extensionRootPath)
        {
            if (manifest.Overrides == null || manifest.Overrides.Count == 0) return;

            foreach (var rule in manifest.Overrides)
            {
                if (string.IsNullOrEmpty(rule.Strategy)) continue;

                switch (rule.Type)
                {
                    case "behavior":
                        if (rule.Strategy == "merge")
                        {
                            ProcessBehaviorMerge(rule, extensionRootPath);
                        }
                        break;
                    // TODO: 后续可支持 resource (文件替换) 或 variable (全局变量注入)
                }
            }
        }

        private void ProcessBehaviorMerge(OverrideRule rule, string extensionRootPath)
        {
            try
            {
                string patchPath = Path.Combine(extensionRootPath, rule.Path);
                if (!File.Exists(patchPath))
                {
                    GD.PrintErr($"[ExtensionManager] Patch file not found: {patchPath}");
                    return;
                }

                string targetPath = rule.Target;
                string targetContent = "";

                // 读取目标内容（支持 res://, user:// 或物理路径）
                if (targetPath.StartsWith("res://") || targetPath.StartsWith("user://"))
                {
                    if (Godot.FileAccess.FileExists(targetPath))
                    {
                        using var file = Godot.FileAccess.Open(targetPath, Godot.FileAccess.ModeFlags.Read);
                        targetContent = file.GetAsText();
                    }
                }
                else if (File.Exists(targetPath))
                {
                    targetContent = File.ReadAllText(targetPath);
                }

                JsonNode targetNode = string.IsNullOrEmpty(targetContent) ? null : JsonNode.Parse(targetContent);
                JsonNode patchNode = JsonNode.Parse(File.ReadAllText(patchPath));

                // 调用合并引擎进行递归合并/覆盖
                var mergedNode = ExtensionJsonMerger.Merge(targetNode, patchNode);
                if (mergedNode != null)
                {
                    string mergedJson = mergedNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                    BehaviorRegistry.Instance?.LoadBehaviorPackFromContent(mergedJson);
                    GD.Print($"[ExtensionManager] Successfully merged behavior patch {rule.Path} into {rule.Target}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ExtensionManager] Error merging behavior {rule.Path}: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取已激活扩展包的物理根路径
        /// </summary>
        public string GetExtensionPath(string id)
        {
            if (!_activeExtensionIds.Contains(id)) return null;
            if (_extensionPaths.TryGetValue(id, out string sourcePath))
            {
                if (sourcePath.EndsWith(".umaext"))
                {
                    return Path.Combine(ProjectSettings.GlobalizePath(CacheDir), id);
                }
                return sourcePath;
            }
            return null;
        }

        /// <summary>
        /// 获取扩展包原始文件/文件夹路径（无论是否激活）
        /// </summary>
        public string GetRawExtensionPath(string id)
        {
            return _extensionPaths.TryGetValue(id, out string path) ? path : null;
        }

        public bool IsExtensionActive(string id) => _activeExtensionIds.Contains(id);
        public IEnumerable<ExtensionManifest> GetAvailableExtensions() => _loadedManifests.Values;

        /// <summary>
        /// 获取当前所有已激活扩展包中的剧情文件路径
        /// </summary>
        public List<string> GetActiveStoryPaths()
        {
            List<string> paths = new();
            foreach (var id in _activeExtensionIds)
            {
                if (!_extensionPaths.TryGetValue(id, out string sourcePath)) continue;

                string targetPath = sourcePath;
                if (sourcePath.EndsWith(".umaext"))
                {
                    targetPath = Path.Combine(ProjectSettings.GlobalizePath(CacheDir), id);
                }

                string storyDir = Path.Combine(targetPath, "Story");
                if (Directory.Exists(storyDir))
                {
                    foreach (var file in Directory.GetFiles(storyDir))
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        if (ext == ".json" || ext == ".era" || ext == ".zip")
                        {
                            paths.Add(file);
                        }
                    }
                }
            }
            return paths;
        }

        // 关闭停用指定的扩展包，完美卸载其 DLL 逻辑与行为规则
        public void DeactivateExtension(string id)
        {
            if (!_activeExtensionIds.Contains(id)) return;

            GD.Print($"[ExtensionManager] Deactivating extension: {id}...");
            _activeExtensionIds.Remove(id);

            if (BehaviorRegistry.Instance != null)
            {
                // 改为增量卸载，降低全盘清空重载引起的数据丢失风险
                BehaviorRegistry.Instance.UnloadBehaviorsForExtension(id);
            }
        }
    }
}
