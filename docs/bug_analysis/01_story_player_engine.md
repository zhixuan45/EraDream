# 01 - 剧情播放引擎 (StoryPlayerEngine)

> 文件: `scripts/StoryPlayerEngine.cs` (560行) | 问题: 19个 | CRITICAL: 1 | HIGH: 6

---

## [CRITICAL] #1 - 预执行视觉节点链被执行两次

**行号**: 165, 169, 513-514

**问题**: `ProcessCurrentNode`(line 168-169) 在处理对话节点后调用 `PreExecuteVisualNodes` 预执行后续视觉节点（精灵/音乐/背景切换）。用户点击后 `OnInteraction`(line 513) 调用 `GoToNextNode(dialogue.NextNodeId)`，正式进入这些视觉节点，导致每个节点被执行**两次**。

**后果**:
- BGM 开始播放 → 用户点击 → BGM 从头重新播放（听觉上明显重启）
- 精灵纹理被重复加载、位置被重置
- 背景切换动画执行两次

**修复建议**: 二选一
- **方案A**: 预执行时标记节点为 "已预执行"，正式流程遇到标记时跳过 Handle 直接继续
- **方案B**: 预执行后直接将 `_currentNode` 推进到视觉链末尾，正式流程从第一个非视觉节点开始

```csharp
// 方案B 示意: PreExecuteVisualNodes 应该返回最后处理的节点ID
private string PreExecuteVisualNodes(string startNodeId) {
    var currentNodeId = startNodeId;
    while (currentNodeId != null) {
        var node = FindNode(currentNodeId);
        if (node is SpriteNodeData || node is MusicNodeData || ...) {
            HandleVisualNode(node);  // 执行视觉效果
            currentNodeId = node.NextNodeId;  // 继续推进
        } else {
            return currentNodeId;  // 停在第一个非视觉节点
        }
    }
    return null;
}
```

---

## [HIGH] #2 - 非叙事/选项节点强制重置模糊和暗度

**行号**: 146-148

**问题**:
```csharp
if (_currentNode is NarrativeNodeData narrative)
    ApplyVisualEffects(narrative.BlurValue, narrative.Darkness);
else if (_currentNode is ChoiceNodeData choice)
    ApplyVisualEffects(choice.BlurValue, choice.Darkness);
else
    ApplyVisualEffects(0, 0);  // <-- 所有其他节点类型重置为0
```

当流程为 `Narrative(blur=3) → SpriteNode → Dialogue` 时，SpriteNode 触发 `else` 分支将 blur 重置为 0，对话显示时背景模糊已丢失。

**修复建议**: 视觉节点类型（SpriteNode, MusicNode, BackgroundNode, StickerNode 等）不应修改模糊/暗度状态:
```csharp
else if (_currentNode is SpriteNodeData || _currentNode is MusicNodeData 
         || _currentNode is BackgroundNodeData || _currentNode is StickerNodeData
         || _currentNode is StartNodeData || _currentNode is EndNodeData
         || _currentNode is ValueNodeData || _currentNode is BranchNodeData) {
    // 不修改视觉效果，保持当前状态
}
else {
    ApplyVisualEffects(0, 0);
}
```

---

## [HIGH] #3 - BGM 引用比较导致每次重新播放

**行号**: 457

**问题**:
```csharp
AudioStream stream = ResourceProxy.LoadAudioFromProject(file);
if (_bgmPlayer.Stream != stream) {  // 对象引用比较
    _bgmPlayer.Stream = stream;
    _bgmPlayer.Play();
}
```

`LoadAudioFromProject` 每次调用创建**新的** `AudioStream` 对象，因此 `!=` 始终为 `true`，即使同一首BGM已播放中，也会被中断并从头播放。

**修复建议**: 改为文件名或资源路径比较:
```csharp
private string _currentBgmPath;  // 新增字段

// 在 PlayBGM 中:
if (_currentBgmPath != file) {
    _currentBgmPath = file;
    _bgmPlayer.Stream = ResourceProxy.LoadAudioFromProject(file);
    _bgmPlayer.Play();
}
```

---

## [HIGH] #4 - GetNode<T> 缺失时硬崩溃

**行号**: 44-50

**问题**: 7个 `GetNode<T>()` 调用，如果场景文件中路径变更或节点被删除，会抛出异常导致引擎崩溃。

```csharp
_nameLabel = GetNode<Label>("UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox/NameLabel");
// ... 6 more GetNode<T> calls
```

**修复建议**: 使用 `GetNodeOrNull<T>()` 并加空检查:
```csharp
_nameLabel = GetNodeOrNull<Label>("UI_Layer/...");
if (_nameLabel == null) {
    GD.PushError("[StoryPlayerEngine] NameLabel not found at expected path");
}
```

---

## [HIGH] #5 - GlobalGameState.Instance 可能为 null

**行号**: 508

**问题**: `HandleBranchNode` 中直接访问 `GlobalGameState.Instance.GetVariable(branch.VariableId)`。如果 `GlobalGameState` 不是 Autoload 或未初始化，NRE 崩溃。

**修复建议**: 加空检查并提供合理的默认行为:
```csharp
var gs = GlobalGameState.Instance;
if (gs == null) {
    GD.PushError("[StoryPlayerEngine] GlobalGameState.Instance is null, branch condition cannot be evaluated");
    GoToNextNode(branch.FalseNodeId);  // 走默认分支
    return;
}
var currentVal = gs.GetVariable(branch.VariableId);
```

---

## [HIGH] #6 - 空故事文件无任何反馈

**行号**: 129

**问题**:
```csharp
if (_storyNodes.Count == 0) return;  // 静默返回，玩家看到空白屏幕
```
`StoryNodeManager.LoadProject` 在加载失败时 catch 空块返回空列表，此处静默返回，玩家看到空白屏幕、无错误提示。

**修复建议**:
```csharp
if (_storyNodes.Count == 0) {
    ErrorNotifier.Instance?.ShowErrorDialog("无法加载剧情", "文件为空或格式错误");
    GetTree().ChangeSceneToFile(_returnScenePath);
    return;
}
```

---

## [MEDIUM] #7 - TryGetValue 传入 null Key 抛异常

**行号**: 288, 296

**问题**: `_activeSprites.TryGetValue(data.CharacterId, ...)` — 如果反序列化的数据中 `CharacterId` 为 null，`Dictionary.TryGetValue(null, ...)` 抛 `ArgumentNullException`。

**修复建议**:
```csharp
if (string.IsNullOrEmpty(data.CharacterId)) return;
var key = data.CharacterId;
// 现在 TryGetValue 安全
```

---

## [MEDIUM] #8 - Pressed 信号累积订阅

**行号**: 86

**问题**:
```csharp
_interactButton.Pressed += OnInteraction;
```
`_Ready()` 每次被调用时都会添加一次订阅。如果节点被移除再重新加入场景树，`OnInteraction` 会被调用多次。

**修复建议**: 在 `_ExitTree` 中取消订阅:
```csharp
public override void _ExitTree() {
    if (_interactButton != null)
        _interactButton.Pressed -= OnInteraction;
    if (_textTween != null && _textTween.IsValid())
        _textTween.Kill();
}
```

---

## [MEDIUM] #9 - 空 _ExitTree 导致 Tween 未清理

**行号**: 122-124

**问题**: `_ExitTree()` 方法体为空。`_textTween` 和视觉效果 Tween 在场景切换时未被 Kill，回调可能操作已释放的节点。

**修复建议**: 见 #8 修复代码中的 `_textTween.Kill()` 部分。

---

## [MEDIUM] #10 - 10+方法无异常处理

**行号**: 135-201, 293-344 等

**问题**: 只有 `HandleValueNode` 有 try-catch。其他方法（HandleSpriteNode, HandleStickerNode, ShowChoiceButtons, HandleBranchNode, PlayBGM, UpdateBackground 等）任何异常都会传播到 Godot 引擎导致脚本执行终止。

**修复建议**: 至少在关键方法外层包裹 try-catch 并显示错误:
```csharp
try {
    HandleSpriteNode(sprite);
} catch (Exception ex) {
    GD.PushError($"[StoryPlayerEngine] Error handling sprite node: {ex.Message}");
}
```

---

## [LOW] #11 - 每 _Input 事件分配新 List

**行号**: 359

**问题**:
```csharp
var allSprites = new List<CharacterSprite>(_activeSprites.Values);
allSprites.AddRange(_activeStickerSprites.Values);
```
鼠标移动事件每秒60+次，每次分配并填充新 List。

**修复建议**: 缓存为成员字段，仅在精灵增删时重建。

---

## [LOW] #12 - O(n) 节点查找

**行号**: 519, 528

**问题**: `_storyNodes.FirstOrDefault(n => n.Id == nextId)` — 每次节点切换做线性查找。

**修复建议**: 在 `LoadStory` 时构建 `Dictionary<string, BaseNodeData> _nodeMap`:
```csharp
_nodeMap = _storyNodes.ToDictionary(n => n.Id);
```

---

## [LOW] #13 - 冗余纹理加载

**行号**: 289

**问题**: 每条对话都调用 `UpdateCharacter(characterId, emotion)` 重新加载纹理，即使角色和表情未变。

**修复建议**: 在 `CharacterSprite` 中缓存当前 `characterId + emotion` 组合:
```csharp
if (existingSprite.CurrentCharacterId == dialogue.CharacterId 
    && existingSprite.CurrentEmotion == dialogue.Emotion)
    return;  // 跳过重复加载
```

---

## [LOW] #14~19 - 其他低优先级问题

| # | 行号 | 问题 | 修复 |
|---|------|------|------|
| 14 | 512-515 | 交互按钮在非文本节点上仍可见但点击无反应 | 非文本节点时隐藏 `_interactButton` |
| 15 | 487 | 文本动画时长无上限（200字符=10秒） | 加最大时长限制或允许快速跳过 |
| 16 | 507 | `float.TryParse` 失败默认0无日志 | 失败时输出警告 |
| 17 | 313 | stickerKey计算可能溢出 | 使用更安全的键生成方式 |
| 18 | 457 | BGM文件加载失败无降级处理 | 返回 null 时跳过不崩溃 |
| 19 | 288 | NodePath 硬编码过长 | 提取为常量 |

---

## 关联问题

- [03_game_system.md](./03_game_system.md) #7 — `FinishStory()` 返回 SimulationMainScreen 可能导致循环
- [04_core_infrastructure.md](./04_core_infrastructure.md) — `ResourceProxy.LoadAudioFromProject` 每次新建对象是 BGM Bug 的根因
