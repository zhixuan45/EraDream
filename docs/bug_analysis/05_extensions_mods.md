# 05 - 扩展/Mod系统 (Extensions & Mods)

> 目录: `scripts/Core/Extensions/` + `scripts/Core/Mods/` + `scripts/Editor/Models/ExtensionManifest.cs` | 问题: 19个 | CRITICAL: 1 | HIGH: 6

---

## [CRITICAL] #1 - 文件夹型扩展包 DLL 路径错误

**文件**: `scripts/Core/Extensions/ExtensionManager.cs` | **行号**: 324

**问题**:
```csharp
string dllPath = Path.Combine(targetCache, "Logic", "ModEntry.dll");
```
对于**文件夹型扩展包**（非 `.umaext` 压缩包），提取步骤被跳过，文件位于 `user://extensions/{id}/Logic/ModEntry.dll`。但 `targetCache` 指向的是缓存目录（`user://cache/ext/{id}/`），该目录下不存在 DLL。导致所有文件夹型 Gameplay 扩展包**加载时静默失败**——DLL 找不到，行为也未被加载。

**修复建议**: 区分压缩包和文件夹两种来源:
```csharp
string extensionRootPath;
if (isArchived) {
    extensionRootPath = targetCache;  // 解压后位于缓存目录
} else {
    extensionRootPath = _extensionPaths[id];  // 直接指向源文件夹
}
string dllPath = Path.Combine(extensionRootPath, "Logic", "ModEntry.dll");
```

---

## [HIGH] #2 - ZIP炸弹无任何防护

**文件**: `scripts/Core/Extensions/ExtensionManager.cs` | **行号**: 184-231 (`ExtractArchive` 方法)

**问题**: 解压过程中完全没有安全检查:
- **无单文件大小限制**: 单个 100MB 的条目直接读出到内存
- **无总提取大小限制**: 10000×100MB = 1TB 写入磁盘
- **无压缩比检查**: 经典的 42.zip（42KB → PB级别）直接通过
- **无文件数量限制**: 百万级小文件可耗尽文件系统 inode
- **无目录数量限制**: 大量空目录条目也可导致 DoS

**修复建议**:
```csharp
private const long MaxExtractFileSize = 50 * 1024 * 1024;  // 50MB per file
private const long MaxExtractTotalSize = 200 * 1024 * 1024; // 200MB total
private const int MaxFileCount = 1000;
private const double MaxCompressionRatio = 100.0;  // 最多100倍

long totalExtracted = 0;
int fileCount = 0;

foreach (var file in reader.GetFiles()) {
    if (++fileCount > MaxFileCount) {
        throw new InvalidOperationException($"Archive contains too many files ({fileCount})");
    }
    
    long compressedSize = reader.GetFileInfo(file).CompressedSize;
    long uncompressedSize = reader.GetFileInfo(file).UncompressedSize;
    
    if (uncompressedSize > MaxExtractFileSize) {
        throw new InvalidOperationException($"File exceeds size limit: {file}");
    }
    if (compressedSize > 0 && (double)uncompressedSize / compressedSize > MaxCompressionRatio) {
        throw new InvalidOperationException($"Suspicious compression ratio for: {file}");
    }
    
    totalExtracted += uncompressedSize;
    if (totalExtracted > MaxExtractTotalSize) {
        throw new InvalidOperationException("Total extraction size limit exceeded");
    }
    
    // ... 解压
}
```

---

## [HIGH] #3 - 安全扫描黑名单不完整，反射和P/Invoke可绕过

**文件**: `scripts/Core/Extensions/SecurityScanner.cs` | **行号**: 16-22

**问题**: 黑名单仅含4条规则，以下高危API完全未被检测:
- `System.Reflection.Assembly.Load` / `LoadFrom` — 动态代码加载
- `System.Reflection.Emit` — 动态IL生成
- `System.Reflection.MethodInfo.Invoke` — 反射动态调用
- `System.Runtime.InteropServices.NativeLibrary` — 原生库加载
- `System.Net.Http` — 网络通信（潜在的C2通道）

**更严重的是**: 扫描器是**静态字符串匹配**，以下代码完全无法检测:
```csharp
var type = Type.GetType("System.IO.File");
var method = type.GetMethod("ReadAllText");
method.Invoke(null, new[] { "/path/to/file" });
```

**修复建议**:
- 扩展黑名单覆盖反射、互操作和网络 API
- 方案A (推荐): 使用 `AssemblyLoadContext` 的沙箱功能限制命名空间
- 方案B: 至少增加反射检测（检查 `typeof()`, `Type.GetType()`, `InvokeMember` 等）

```csharp
// 添加到 _knownMaliciousSignatures:
{"System.Reflection", "反射/动态调用"},
{"System.Runtime.InteropServices", "P/Invoke 原生库加载"},
{"System.Net", "网络通信"},
{"System.Diagnostics.Process", "进程启动"},
```

---

## [HIGH] #4 - ProcessBehaviorMerge 是死代码

**文件**: `scripts/Core/Extensions/ExtensionManager.cs`

**问题**: `ProcessBehaviorMerge` 方法和 `ExtensionJsonMerger` 全部合并逻辑已实现，但 `ApplyManifestOverrides` **从未被任何代码路径调用**。扩展包清单中声明的所有覆盖规则 (`OverrideRules`) 被完全忽略，合并系统完全不工作。

**修复建议**: 在 `ActivateExtensionInternal` 的适当位置（行为包加载前）添加调用:
```csharp
// 加载行为包之前，先应用覆盖规则
ApplyManifestOverrides(manifest, extensionRootPath);
// 然后加载行为包
BehaviorRegistry.Instance.LoadBehaviorPack(behaviorPath);
```

---

## [HIGH] #5 - 编辑器/运行时 Type 字段不兼容导致解析崩溃

**文件**: 
- `scripts/Editor/Models/ExtensionManifest.cs:20` — `public string Type { get; set; } = "character"` (小写字符串)
- `scripts/Core/Extensions/ExtensionManifest.cs:55` — `public PackType Type` (枚举，PascalCase: `Character`, `Gameplay`)

**问题**: 编辑器创建的清单中 Type 值为 `"character"`（小写 string），但运行时使用 `JsonStringEnumConverter` 反序列化为 `PackType` 枚举。`"character"` 不是有效的 `PackType` 枚举值（枚举值是 `Character`, `Gameplay`），导致 **`JsonException` 抛出**。所有从编辑器创建的扩展包在运行时加载时均因解析异常而失败。

**修复建议**: 统一类型系统，运行时反序列化时使用 `JsonStringEnumConverter` 并允许大小写不敏感:
```csharp
var options = new JsonSerializerOptions();
options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));  // 或使用 case-insensitive
```
同时统一编辑器侧也使用 `PackType` 枚举而非 string。

---

## [HIGH] #6 - 编辑器/运行时 Dependencies 类型不兼容

**文件**:
- `scripts/Editor/Models/ExtensionManifest.cs:29` — `public List<object> Dependencies`
- `scripts/Core/Extensions/ExtensionManifest.cs:61` — `public List<DependencyInfo> Dependencies`

**问题**: 编辑器使用 `List<object>` 存储依赖，运行时使用强类型 `List<DependencyInfo>`。反序列化时 `JsonSerializer.Deserialize<List<DependencyInfo>>(json)` 尝试将 `object` 数组（可能是 `string` 或 `JsonElement`）转换为 `DependencyInfo`，可能失败或产生空对象。

**修复建议**: 编辑器侧也使用 `DependencyInfo` 类型。

---

## [HIGH] #7 - MinGameVersion 和依赖版本号完全忽略

**文件**: `scripts/Core/Extensions/ExtensionManager.cs` | **行号**: 258-269, 清单中各文件

**问题**:
- `manifest.MinGameVersion` 字段在激活流程中从未被读取，扩展包可以在不兼容的游戏版本上激活
- `DependencyInfo.Version` 字段虽然存在，但在依赖检查时仅通过 ID 匹配，完全忽略版本约束

**修复建议**: 在激活前添加版本检查:
```csharp
// 游戏版本检查
if (!string.IsNullOrEmpty(manifest.MinGameVersion)) {
    if (CompareVersions(ProjectSettings.GetSetting("...version"), manifest.MinGameVersion) < 0) {
        GD.PushError($"Extension '{id}' requires game version >= {manifest.MinGameVersion}");
        return false;
    }
}

// 依赖版本检查
foreach (var dep in manifest.Dependencies) {
    if (!CheckDependencyVersion(dep.Id, dep.Version)) {
        return false;
    }
}
```

---

## [MEDIUM] #8 - AssemblyLoadContext 卸载不彻底

**文件**: `scripts/Core/Mods/ModLoader.cs` | **行号**: 160

**问题**:
```csharp
modInfo.LoadContext.Unload();
_loadedMods.Remove(modId);
```
`AssemblyLoadContext.Unload()` 是异步操作——只标记上下文待卸载，实际释放在下一次 GC 时发生。如果调用 `Unload()` 后没有调用 `GC.Collect()` + `GC.WaitForPendingFinalizers()`:
- DLL 文件保持锁定状态，无法被删除或替换
- 如果 `OnUnload()` 抛异常，或外部代码持有对 Mod 实例的引用，ALC 永远无法被回收

**修复建议**:
```csharp
modInfo.LoadContext.Unload();
_loadedMods.Remove(modId);
for (int i = 0; i < 3; i++) {
    GC.Collect();
    GC.WaitForPendingFinalizers();
}
```

---

## [MEDIUM] #9 - 卸载时清除后从磁盘重载可能导致数据丢失

**文件**: `scripts/Core/Extensions/ExtensionManager.cs` | **行号**: 476, 478-493

**问题**: `DeactivateExtension` 先调用 `BehaviorRegistry.Instance.Clear()` 清空所有行为数据，然后逐个从磁盘重新加载剩余活跃扩展包的行为。如果某个 `behavior.json` 在首次加载后被删除或损坏，对应扩展包的行为数据永久丢失（内存中无备份）。

**修复建议**: 改为增量移除——只移除卸载扩展包的行为数据，不需清空再重载:
```csharp
BehaviorRegistry.Instance.UnloadBehaviorsForExtension(id);  // 增量移除
```

---

## [MEDIUM] #10 - RegistryAPI 无法取消注册，重复加载失败

**文件**: `scripts/Core/Extensions/RegistryAPI.cs`

**问题**: `_registeredStatIds` 只有 `Add` 方法，没有 `Remove`。扩展包卸载后其注册的统计 ID 仍然残留在 HashSet 中。如果同一扩展包被重新加载，第二次的 `RegisterStat` 因为 `ContainsKey` 检查而失败。

**修复建议**: 添加 `UnregisterStat` 方法:
```csharp
public static bool UnregisterStat(string id) {
    return _registeredStatIds.Remove(id);
}
```

---

## [MEDIUM] #11 - 安全扫描仅扫入口DLL，依赖DLL绕过

**文件**: `scripts/Core/Mods/ModLoader.cs` | **行号**: 82

**问题**: `SecurityScanner.Scan` 仅在入口点 DLL 上执行。`ModAssemblyLoadContext.OnResolving` 从同一目录静默加载依赖 DLL，这些依赖 DLL **完全不被扫描**。恶意代码只需放在依赖 DLL 中即可绕过安全扫描。

**修复建议**: 在 `OnResolving` 中对依赖 DLL 也执行扫描:
```csharp
protected override Assembly Load(AssemblyName assemblyName) {
    string depPath = Path.Combine(_modDir, assemblyName.Name + ".dll");
    if (File.Exists(depPath)) {
        SecurityScanner.Scan(depPath);  // 扫描依赖DLL
        return LoadFromAssemblyPath(depPath);
    }
    return null;
}
```

---

## [MEDIUM] #12 - JSON合并数组无限增长

**文件**: `scripts/Core/Extensions/ExtensionJsonMerger.cs` | **行号**: 68-75

**问题**: 数组合并始终是追加模式，无去重。如果扩展包被反复激活/停用（每次停用不清除合并结果），数组会无限增长。

**修复建议**: 添加去重逻辑（基于元素的 `id` 字段）或替代模式:
```csharp
// 按 id 字段去重的追加
if (targetArray != null && sourceArray != null) {
    foreach (var srcItem in sourceArray) {
        var srcId = srcItem["id"]?.GetValue<string>();
        if (srcId != null) {
            var existing = targetArray.FirstOrDefault(t => t["id"]?.GetValue<string>() == srcId);
            if (existing != null) targetArray.Remove(existing);  // 替换
        }
        targetArray.Add(srcItem.DeepClone());
    }
}
```

---

## [MEDIUM] #13 - 清单验证完全缺失

**文件**: `scripts/Core/Extensions/ExtensionManager.cs` | **行号**: 143, 157

**问题**: `LoadManifestFromFile` 和 `LoadManifestFromArchive` 反序列化后**无任何验证**:
- `Id`、`Name`、`Version` 为空时无检查
- ID 中包含 `/` 或 `\` 直到激活时才检查（为时已晚）
- 版本号格式无验证（`"potato"` 被接受）
- 依赖 ID 是否对应已存在的扩展包不做预检
- `OverrideRule.Path` 和 `Target` 无沙箱边界检查

**修复建议**: 在 `ScanExtensions` 阶段添加验证:
```csharp
private bool ValidateManifest(ExtensionManifest manifest, string id) {
    if (string.IsNullOrWhiteSpace(manifest.Id)) return false;
    if (string.IsNullOrWhiteSpace(manifest.Name)) return false;
    if (manifest.Id.Contains("/") || manifest.Id.Contains("\\")) return false;
    // 检查所有 OverrideRule.Target 不逃逸扩展包沙箱
    foreach (var rule in manifest.OverrideRules) {
        if (Path.IsPathRooted(rule.Target)) return false;
    }
    return true;
}
```

---

## [LOW] #14~19 - 其他低优先级问题

| # | 文件:行 | 问题 | 修复 |
|---|---------|------|------|
| 14 | `SecurityScanner.cs:111` | `StartsWith("System.IO")` 误伤 `System.IO.Path` / `MemoryStream` | 使用更精确的匹配 |
| 15 | `SecurityScanner.cs` | 扫描只在主线程阻塞执行 | 考虑异步扫描 |
| 16 | `ModLoader.cs:101` | 多个 `IUmaPlugin` 实现时选择第一个（未定义顺序） | 加日志或抛异常 |
| 17 | `ModLoader.cs:98` | 程序集绑定冲突（版本不匹配） | 加 AssemblyName 版本检查 |
| 18 | `ExtensionManager.cs:470` | 停用时不清除临时缓存目录 | 在 `DeactivateExtension` 中清理 `user://cache/ext/{id}` |
| 19 | `ExtensionManifest.cs` | `NestedPackages` 字段完全未处理 | 实现嵌套包加载或移除该字段 |
