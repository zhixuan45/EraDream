# 04 - 核心基础设施 (Core/)

> 目录: `scripts/Core/` (排除 Extensions/ 和 Mods/) | 问题: 18个 | CRITICAL: 0 | HIGH: 5

---

## [HIGH] #1 - ResponsiveManager: OnSafeAreaPaddingChanged lambda 从不取消订阅

**文件**: `scripts/Core/ResponsiveManager.cs` | **行号**: 53-56

**问题**:
```csharp
SettingsManager.Instance.OnSafeAreaPaddingChanged += (padding) => {
    // ... lambda 捕获了 this
};
```
这个 lambda 同时捕获了 `ResponsiveManager` 实例。`_ExitTree` (line 89-97) 仅清理了 `SizeChanged`，**从未移除** `OnSafeAreaPaddingChanged` 上的这个 lambda。这是典型的**事件泄漏**——即使节点被释放，SettingsManager（Autoload，永不销毁）仍持有对该 lambda 的引用，阻止 GC 回收。

**修复建议**: 使用命名方法而非 lambda，在 `_ExitTree` 中取消:
```csharp
private void OnSafeAreaPaddingChangedHandler(float padding) { ... }

// _Ready:
SettingsManager.Instance.OnSafeAreaPaddingChanged += OnSafeAreaPaddingChangedHandler;

// _ExitTree:
if (SettingsManager.Instance != null)
    SettingsManager.Instance.OnSafeAreaPaddingChanged -= OnSafeAreaPaddingChangedHandler;
```

---

## [HIGH] #2 - FileIOManager: 写入失败静默丢弃

**文件**: `scripts/Core/FileIOManager.cs` | **行号**: 121-125 (SaveJson), 168-172 (SaveBinary)

**问题**:
```csharp
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
if (file != null) {
    file.StoreString(json);
}
// file == null 时静默跳过，无任何错误指示
```
`FileAccess.Open` 可能因权限不足、磁盘满、Android scoped storage 限制等原因返回 null。调用方（SettingsManager、StoryNodeManager等）以为保存成功。

**修复建议**:
```csharp
using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
if (file == null) {
    var error = FileAccess.GetOpenError();
    GD.PushError($"[FileIOManager] Failed to open file '{path}': {error}");
    return false;
}
file.StoreString(json);
return true;
```

---

## [HIGH] #3 - FileIOManager: 无原子写入策略

**文件**: `scripts/Core/FileIOManager.cs` | **行号**: 121-125, 168-172

**问题**: 直接覆盖写入目标文件。如果在 `StoreString`/`StoreBuffer` 过程中应用崩溃，目标文件处于截断/损坏状态，无备份可恢复。

**修复建议**: 使用"写入临时文件 → 重命名"的原子写入模式:
```csharp
string tempPath = path + ".tmp";
using var file = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write);
// ... 写入内容到 tempPath
file.Close();
DirAccess.RenameAbsolute(tempPath, path);  // 原子替换
```

---

## [HIGH] #4 - SettingsManager: 损坏文件静默重置

**文件**: `scripts/Core/SettingsManager.cs` | **行号**: 111-117

**问题**:
```csharp
_currentSettings = FileIOManager.LoadJson<AppSettings>(SettingsFilePath) ?? new AppSettings();
```
如果 JSON 反序列化失败（字段不匹配、格式错误），`LoadJson` 返回 null，回退到 `new AppSettings()`。用户的**所有设置被静默重置为默认值**，无任何错误提示。

**修复建议**:
- 为 `AppSettings` 添加 `[JsonPropertyName]` 属性确保向前兼容
- 反序列化失败时先尝试备份损坏文件，再通知用户
```csharp
_currentSettings = FileIOManager.LoadJson<AppSettings>(SettingsFilePath);
if (_currentSettings == null) {
    GD.PushError("[SettingsManager] Failed to load settings, resetting to defaults");
    // 备份损坏文件
    FileIOManager.CopyFile(SettingsFilePath, SettingsFilePath + ".corrupted");
    _currentSettings = new AppSettings();
}
```

---

## [HIGH] #5 - FileIOManager.LoadJson: JSON 大小写反序列化问题

**文件**: `scripts/Core/FileIOManager.cs` | **行号**: 146

**问题**:
```csharp
return JsonSerializer.Deserialize<T>(json);  // 无 JsonSerializerOptions
```
`System.Text.Json` 默认区分大小写。如果 `AppSettings` 的属性名和 JSON 中的键名大小写不一致（如 C# 中 `IsDarkMode` vs JSON 中 `is_dark_mode`），`Deserialize` 静默回退到默认值。
[关联: 此问题也与 SettingsManager#4 相互作用——无 JsonPropertyName 属性 + 区分大小写 = 静默重置]

**修复建议**:
```csharp
var options = new JsonSerializerOptions {
    PropertyNameCaseInsensitive = true,
    IncludeFields = true
};
return JsonSerializer.Deserialize<T>(json, options);
```
同时为 `AppSettings` 所有属性添加 `[JsonPropertyName("xxx")]` 提供显式映射。

---

## [MEDIUM] #6 - Path.Combine 与 Godot res:// 路径混合

**文件**: `scripts/Core/ResourceProxy.cs` | **行号**: 56

**问题**:
```csharp
Path.Combine(ProjectManager.AudioDir, fileName)
```
Windows 上 `Path.Combine` 使用 `\` 分隔符，产生 `res://audio\file.mp3`。Godot 在 Windows 上可能容忍反斜杠，但 Linux/macOS 上 `\` 是文件名的合法字符，导致路径错误。

**修复建议**: Godot 内部路径统一使用 `/`:
```csharp
string path = $"{ProjectManager.AudioDir}/{fileName}";  // 字符串插值，使用 /
```

---

## [MEDIUM] #7 - SettingsOverlay: 所有 Toggle 订阅从不取消

**文件**: `scripts/Core/SettingsOverlay.UI.cs` | **行号**: 83, 92, 100, 122-124

**问题**: 6个事件连接（3个 `Toggled`、3个 `ValueChanged`/`DragStarted`/`DragEnded`），全部使用 lambda，无任何对应的 `-=`。

```csharp
_darkModeToggle.Toggled += OnDarkModeToggled;      // 行号83
_embeddedWindowToggle.Toggled += OnEmbeddedWindowToggled;  // 行号92
_mouseCursorToggle.Toggled += OnMouseCursorToggled;  // 行号100
_safeAreaSlider.ValueChanged += OnSafeAreaSliderChanged;  // 行号122
// ... 无 _ExitTree 中的清理
```

**修复建议**: 添加 `_ExitTree` 重写:
```csharp
public override void _ExitTree() {
    if (_darkModeToggle != null) _darkModeToggle.Toggled -= OnDarkModeToggled;
    if (_embeddedWindowToggle != null) _embeddedWindowToggle.Toggled -= OnEmbeddedWindowToggled;
    if (_mouseCursorToggle != null) _mouseCursorToggle.Toggled -= OnMouseCursorToggled;
    if (_safeAreaSlider != null) {
        _safeAreaSlider.ValueChanged -= OnSafeAreaSliderChanged;
    }
}
```

---

## [MEDIUM] #8 - SafeAreaAdapter: NaN 导致 int.MinValue

**文件**: `scripts/Core/SafeAreaAdapter.cs` | **行号**: 31

**问题**:
```csharp
int p = (int)padding;  // 如果 padding 是 NaN，结果 = int.MinValue
```
如果设置文件损坏或反序列化异常，`padding` 可能是 `float.NaN`。`(int)float.NaN` 在所有 .NET 版本中返回 `int.MinValue` (-2147483648)，彻底破坏布局。

**修复建议**:
```csharp
int p = float.IsNaN(padding) || float.IsInfinity(padding) ? 0 : Mathf.Clamp((int)padding, 0, 100);
```

---

## [MEDIUM] #9 - GlobalGameState.Instance 在 _Ready 中设置

**文件**: `scripts/Core/GlobalGameState.cs` | **行号**: 18

**问题**:
```csharp
public override void _Ready() {
    Instance = this;
}
```
Autoload 的 `_Ready` 按顺序调用。如果其他 Autoload 在其 `_Ready` 中访问 `GlobalGameState.Instance`，但 GlobalGameState 的 `_Ready` 尚未执行，则返回 null。

**修复建议**: 移至 `_EnterTree`:
```csharp
public override void _EnterTree() {
    Instance = this;
}
```

---

## [MEDIUM] #10 - ErrorNotifier: 非主线程调用 Godot API

**文件**: `scripts/Core/ErrorNotifier.cs` | **行号**: 80-82, 108-127

**问题**: `ShowToast` 和 `ShowErrorDialog` 是 public 方法，直接操作 Godot 节点（`PopupCentered`, `CreateTween`, 修改 `Modulate`）。如果从工作线程（如后台加载、网络回调）调用，Godot 运行时会崩溃。

**修复建议**: 使用 `CallDeferred`:
```csharp
public void ShowToast(string message) {
    CallDeferred(nameof(ShowToastDeferred), message);
}

private void ShowToastDeferred(string message) {
    // 原来的实现代码
}
```

---

## [MEDIUM] #11 - ErrorNotifier: toast 队列无上限

**文件**: `scripts/Core/ErrorNotifier.cs`

**问题**: `_toastQueue` 是一个 `Queue<string>`，如果 toast 被快速连续调用且显示速度赶不上，队列无限增长。

**修复建议**: 加最大队列长度:
```csharp
private const int MaxToastQueue = 20;
if (_toastQueue.Count >= MaxToastQueue) {
    _toastQueue.Dequeue();  // 丢弃最旧的消息
}
_toastQueue.Enqueue(message);
```

---

## [MEDIUM] #12 - SettingsManager: 每次属性变更同步写磁盘

**文件**: `scripts/Core/SettingsManager.cs` | **行号**: 24, 39, 52, 67, 79

**问题**: 每个属性 setter 都调用 `SaveSettings()`，导致频繁的同步磁盘IO。启动时顺序设置多个属性会产生多次写入。

**修复建议**: 加入延迟保存:
```csharp
private Timer _saveDebounceTimer;

private void SaveSettingsDebounced() {
    if (_saveDebounceTimer == null) {
        _saveDebounceTimer = new Timer { OneShot = true, WaitTime = 0.5 };
        _saveDebounceTimer.Timeout += SaveSettings;
        AddChild(_saveDebounceTimer);
    }
    _saveDebounceTimer.Stop();
    _saveDebounceTimer.Start();
}
```

---

## [LOW] #13~18 - 其他低优先级问题

| # | 文件:行 | 问题 | 修复 |
|---|---------|------|------|
| 13 | `FileIOManager.cs:208-256` | Android `content://` URI 解析硬编码路径，无验证 | 解析后检查 `FileAccess.FileExists` |
| 14 | `ErrorNotifier.cs:117` | Toast 硬编码 `-100` 偏移量，未考虑安全区/键盘 | 使用 `SafeAreaAdapter` 获取实际安全区域 |
| 15 | `UIUtils.cs:38` | `DisplayServer.MouseGetPosition()` 在 Wayland 上返回 Zero | 改用 `GetViewport().GetMousePosition()` |
| 16 | `SettingsOverlay.cs:34-43` | `ShowOverlay` 不检查 `_overlayRoot` 是否为 null | 添加空检查 |
| 17 | `CommandHistory.cs` | 无锁的 Stack 操作（但当前单线程使用） | 如需多线程则改用 ConcurrentStack |
| 18 | `ErrorNotifier.cs` | `_toastTimer`/Tween 在 `_ExitTree` 中未清理 | 添加 `_ExitTree` 清理 |
