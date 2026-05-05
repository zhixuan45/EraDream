import sys

filepath = 'scripts/Core/Extensions/ExtensionManager.cs'

with open(filepath, 'r') as f:
    content = f.read()

# Remove the TODO comment
content = content.replace('// TODO: 调用 ModLoader 加载 Logic/ModEntry.dll\n                ', '')

# Adding path traversal checks and instance checks
# The targetCache logic is:
# string targetCache = Path.Combine(ProjectSettings.GlobalizePath(CacheDir), id);

search_block = """            // 1. 准备解压目录
            string targetCache = Path.Combine(ProjectSettings.GlobalizePath(CacheDir), id);

            // 2. 解压逻辑 (Phase 1 实现)
            // ExtractArchive(id, targetCache);"""

replace_block = """            // 1. 准备解压目录及安全检查
            if (string.IsNullOrEmpty(id) || id.Contains("..") || id.Contains("/") || id.Contains("\\\\"))
            {
                GD.PrintErr($"[ExtensionManager] Invalid extension id for activation: {id}");
                return Task.FromResult(false);
            }

            string baseCacheDir = Path.GetFullPath(ProjectSettings.GlobalizePath(CacheDir));
            string targetCache = Path.GetFullPath(Path.Combine(baseCacheDir, id));

            // 验证 targetCache 必须在 baseCacheDir 内，防止路径穿越攻击
            if (!targetCache.StartsWith(baseCacheDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !targetCache.Equals(baseCacheDir, StringComparison.OrdinalIgnoreCase))
            {
                GD.PrintErr($"[ExtensionManager] Security violation: target cache path {targetCache} escapes base cache directory.");
                return Task.FromResult(false);
            }

            // 2. 解压逻辑 (Phase 1 实现)
            // ExtractArchive(id, targetCache);"""

content = content.replace(search_block, replace_block)

search_block2 = """                GD.Print($"[ExtensionManager] {id} is a Gameplay pack. Logic injection pending...");

                string dllPath = Path.Combine(targetCache, "Logic", "ModEntry.dll");"""

replace_block2 = """                GD.Print($"[ExtensionManager] {id} is a Gameplay pack. Logic injection pending...");

                string dllPath = Path.Combine(targetCache, "Logic", "ModEntry.dll");

                // 检查模块依赖是否可用
                if (ModLoader.Instance == null)
                {
                    GD.PrintErr($"[ExtensionManager] ModLoader instance is not available. Cannot load DLL for {id}.");
                }
                else """

content = content.replace(search_block2, replace_block2)

search_block3 = """                // 加载行为包
                string behaviorPath = Path.Combine(targetCache, "Logic", "behavior.json");
                if (File.Exists(behaviorPath))
                {
                    BehaviorRegistry.Instance?.LoadBehaviorPack(behaviorPath);
                    GD.Print($"[ExtensionManager] Behavior pack loaded for {id}");
                }"""

replace_block3 = """                // 加载行为包
                string behaviorPath = Path.Combine(targetCache, "Logic", "behavior.json");
                if (BehaviorRegistry.Instance == null)
                {
                    GD.PrintErr($"[ExtensionManager] BehaviorRegistry instance is not available. Cannot load behavior.json for {id}.");
                }
                else if (File.Exists(behaviorPath))
                {
                    BehaviorRegistry.Instance.LoadBehaviorPack(behaviorPath);
                    GD.Print($"[ExtensionManager] Behavior pack loaded for {id}");
                }"""

content = content.replace(search_block3, replace_block3)

with open(filepath, 'w') as f:
    f.write(content)

print("Updated ExtensionManager.cs successfully.")
