# 03 - 养成游戏系统 (Game/)

> 目录: `scripts/Game/` | 问题: 43个 | CRITICAL: 3 | HIGH: 9 | 最密集的问题子系统

---

## [CRITICAL] #1 - GameManager._EnterTree() 重入时自我销毁

**文件**: `scripts/Game/GameManager.cs` | **行号**: 40-55

**问题**:
```csharp
public override void _EnterTree() {
    if (Instance == null) {
        Instance = this;
    } else {
        QueueFree();  // <-- 如果重入的是自身，这里销毁合法单例!
        return;
    }
    InitializeModules();
}
```
当 GameManager 作为 Autoload 被移出再重新加入场景树时，`Instance == this` 成立（单例指向自身），但代码走入 `else` 分支，调用 `QueueFree()` 销毁自身。之后所有 `GameManager.Instance` 访问返回已释放的对象。

**修复建议**:
```csharp
if (Instance == null) {
    Instance = this;
} else if (Instance != this) {
    // 另一个 GameManager 实例已存在，销毁这个重复的
    QueueFree();
    return;
}
// Instance == this，正常初始化
InitializeModules();
```

---

## [CRITICAL] #2 - AdvanceTurn() 中场景切换后的 use-after-free

**文件**: `scripts/Game/GameManager.cs` | **行号**: 308-310

**问题**: `AdvanceTurn()` 调用链:
```
OnTurnEnd → UpdateTurnEffects → 资源恢复 → NextTurn → AutoSave → 
CheckTurnStartStory() → Events.CheckAndTriggerStory() → GetTree().ChangeSceneToFile()
→ 当前场景被销毁!
→ OnTurnStart?.Invoke(CurrentState.CurrentTurn)  // use-after-free
```

`Events.CheckAndTriggerStory()` 内部调用 `GetTree().ChangeSceneToFile()` 立即销毁当前场景（包含 GameManager 的所有子模块）。返回后 `OnTurnStart?.Invoke()` 在已释放的场景对象上执行。

**修复建议**: 检查返回值，如果触发了剧情则立即 return:
```csharp
if (CheckTurnStartStory()) return;  // 剧情触发导致场景切换，中断后续逻辑
OnTurnStart?.Invoke(CurrentState.CurrentTurn);
```

```csharp
private bool CheckTurnStartStory() {
    var triggered = Events.CheckAndTriggerStory(...);
    if (triggered) {
        // 场景即将切换，不要继续执行
    }
    return triggered;
}
```

---

## [CRITICAL] #3 - _Ready() 中 HandleTurnStart 导致无限场景切换循环

**文件**: `scripts/Game/SimulationMainScreen.cs:160` + `scripts/Game/GameManager.cs:260-269`

**问题**: 循环如下:
1. `SimulationMainScreen._Ready()` → `GameManager.Instance.HandleTurnStart()`
2. `HandleTurnStart()` 触发回合开始剧情 → `ChangeSceneToFile(StoryPlayerEngine)`
3. 剧情结束，`StoryPlayerEngine.FinishStory()` → `ChangeSceneToFile(SimulationMainScreen)`
4. 新的 `SimulationMainScreen._Ready()` 再次调用 `HandleTurnStart()`
5. 如果剧情触发条件依然满足（状态未改变），重复步骤2 → 无限循环

**修复建议**: 加入"已触发过"标记:
```csharp
// GameManager 中:
private bool _hasTriggeredTurnStartThisTurn = false;

public void HandleTurnStart() {
    if (_hasTriggeredTurnStartThisTurn) return;
    _hasTriggeredTurnStartThisTurn = true;
    // ... 触发逻辑
}

// AdvanceTurn 中重置标记:
_hasTriggeredTurnStartThisTurn = false;
```

---

## [HIGH] #4 - 智力训练 ConsumeActionStamina(-5) 绕过上限检查

**文件**: `scripts/Game/Modules/TrainingModule.cs:44-45` + `scripts/Game/Models/UmaStats.cs:100-108`

**问题**:
```csharp
// TrainingModule.cs 智力训练:
state.Uma.ConsumeActionStamina(actionStaminaCost);  // actionStaminaCost = -5
```
`ConsumeActionStamina(-5)` 等价于 `ActionStamina -= (-5)` → `ActionStamina += 5`，绕过了 `AddActionStamina` 中的 `Mathf.Clamp(0, MaxActionStamina)` 上限检查。多次智力训练后，ActionStamina 可无限超过上限。

**修复建议**: 将负值消耗改为调用增加方法:
```csharp
// ConsumeActionStamina 内部:
public void ConsumeActionStamina(int amount) {
    if (amount < 0) {
        AddActionStamina(-amount);  // 走带 Clamp 的增加路径
    } else {
        ActionStamina = Mathf.Max(0, ActionStamina - amount);
    }
}
```

---

## [HIGH] #5 - RefreshScoutPoolWithCost 负成本攻击

**文件**: `scripts/Game/GameManager.cs` | **行号**: 155-163

**问题**:
```csharp
public bool RefreshScoutPoolWithCost(int cost) {
    if (CurrentState.Player.Money < cost) return false;  // cost = -100 → 100 < -100 = false
    CurrentState.Player.AddMoney(-cost);  // AddMoney(100) = 增加100金币!
    // ...
}
```
传入负 `cost` 时，金钱检查被绕过（正数永远不小于负数），然后 `AddMoney(-(-100))` = `AddMoney(100)` 反而给玩家增加金币。

**修复建议**:
```csharp
public bool RefreshScoutPoolWithCost(int cost) {
    if (cost <= 0) {
        GD.PushError($"[GameManager] RefreshScoutPoolWithCost called with invalid cost: {cost}");
        return false;
    }
    // ...
}
```

---

## [HIGH] #6 - 回合边界导致自动存档死档

**文件**: `scripts/Game/Models/GameState.cs` | **行号**: 43

**问题**:
```csharp
public bool IsGameOver => CurrentTurn > MaxTurns;  // MaxTurns = 72
```
在最后一回合（72），玩家操作后 `AdvanceTurn()`:
1. `CurrentTurn` 72 → 73
2. `IsGameOver` 变为 true
3. `AutoSave()` 保存游戏结束状态
4. 玩家读档 → `IsGameOver` = true → 几乎所有操作被阻断 → 死档

**修复建议**:
- **方案A**: `IsGameOver` 前先检查是否已到达结局，到达则不自动存档
- **方案B**: 回合72的 `AdvanceTurn` 直接进入结局流程，不调用 `AutoSave`
```csharp
public void AdvanceTurn() {
    // ...
    NextTurn();
    if (!IsGameOver) {
        AutoSave();
    } else {
        TriggerEnding();
    }
}
```

---

## [HIGH] #7 - InitializeModules 空 ScenarioPaths 导致永无事件触发

**文件**: `scripts/Game/GameManager.cs` | **行号**: 91, 116

**问题**:
```csharp
StartNewGame(new List<string>());  // 空的 scenarioPaths
// →
Events.LoadEventPool(CurrentState.ScenarioPaths);  // 空列表 → _eventPool 始终为空
// →
CheckAndTriggerStory() 永远不会触发任何事件
```

**修复建议**: 至少传入默认剧本路径:
```csharp
var defaultScenarios = new List<string> { "res://scenarios/default/" };
StartNewGame(defaultScenarios);
```

---

## [HIGH] #8 - AddMoney 无上/下限保护

**文件**: `scripts/Game/Models/PlayerStats.cs` | **行号**: 15, 41

**问题**:
```csharp
public void AddMoney(int amount) {
    Money += amount;  // 无 Mathf.Clamp
}
```
通过剧情 `ValueNode` 或扩展钩子可设置任意值（包括负值和溢出值）。

**修复建议**:
```csharp
public void AddMoney(int amount) {
    Money = Mathf.Clamp(Money + amount, 0, int.MaxValue);
}
```

---

## [HIGH] #9 - SkillPoints 和 Affection 无任何钳制

**文件**: `scripts/Game/Models/UmaStats.cs` | **行号**: 64-69

**问题**: 五维属性（速度/耐力等）经过 `AddStat()` 的 `Mathf.Clamp` 保护，但 `SkillPoints` 和 `Affection` 是裸 `get; set;`，通过 `+=` 直接修改，可变为负数。

**修复建议**:
```csharp
private int _skillPoints;
public int SkillPoints {
    get => _skillPoints;
    set => _skillPoints = Mathf.Max(0, value);
}
```

---

## [HIGH] #10 - EventModule.TriggerStory 跨模块场景切换

**文件**: `scripts/Game/Modules/EventModule.cs` | **行号**: 93-101

**问题**: 作为一个子 Node，`EventModule` 直接调用 `GetTree().ChangeSceneToFile()` 销毁整个场景树（包括自身的父节点 GameManager）。违反分层原则。

**修复建议**: EventModule 发出信号，由上层决定如何处理:
```csharp
// EventModule:
[Signal] public delegate void StoryTriggeredEventHandler(string scenePath, string startNodeId);
// 触发时: EmitSignal(SignalName.StoryTriggered, projectPath, startNodeId);

// GameManager 接收信号后处理场景切换
```

---

## [HIGH] #11 - 训练失败的错误提示不匹配

**文件**: `scripts/Game/SimulationMainScreen.cs` | **行号**: 320-321

**问题**: 训练失败时（可能是随机失败或状态不足），显示的消息始终是"马娘行动力不足，无法进行训练！"，与实际失败原因不符。

**修复建议**: 根据 `TrainingModule.ExecuteTraining` 的返回值区分失败原因。

---

## [HIGH] #12 - InventoryModule foreach 中可能修改集合

**文件**: `scripts/Game/Modules/InventoryModule.cs` | **行号**: 132

**问题**:
```csharp
foreach (var itemId in state.Inventory.Items.Keys) {
    var def = BehaviorRegistry.Instance.GetItemDefinition(itemId);
    if (def?.Type == ItemType.Permanent)
        BehaviorRegistry.Instance.TriggerHook($"OnItemTick_{itemId}", state);
}
```
`TriggerHook` 触发的扩展钩子可能修改 `state.Inventory.Items`，在 `foreach` 中修改被枚举的集合 → `InvalidOperationException`。

**修复建议**: 先复制 Key 集合再遍历:
```csharp
var itemIds = state.Inventory.Items.Keys.ToList();
foreach (var itemId in itemIds) {
    // ...
}
```

---

## [MEDIUM] #13~43 - 中低优先级问题

| # | 文件:行 | 严重度 | 问题 | 修复 |
|---|---------|--------|------|------|
| 13 | `GameManager.cs:169-221` | MED | `ContractUma` 未检查已有马娘 → 覆盖旧数据 | 签约前检查 `ActiveUmaId` |
| 14 | `GameManager.cs:238-249` | MED | `LoadGame` 不触发生命周期事件 | 读档后调用 `OnGameLoaded` |
| 15 | `GameManager.cs:238-249` | MED | `LoadGame` 不验证 `ActiveUmaId` 有效性 | 校验角色是否存在 |
| 16 | `Game.DebugConsole.cs:131` | MED | 调试 `set turn` 可设负数回合 | 加范围检查 |
| 17 | `InventoryModule.cs:17-30` | MED | `AddItem` 超堆叠上限静默丢弃 | 返回实际添加数量 |
| 18 | `InventoryModule.cs:82-85` | MED | Permanent物品持有量检查多余 | 移除或加注释说明 |
| 19 | `InventoryModule.cs:94-106` | MED | Duration物品 RemainingTurns 可为负数 | 加 >=0 检查 |
| 20 | `GameManager.cs:196-197` | MED | ContractUma中 MaxEnergy 从 conditions 取值不当 | 固定为角色种族基础值 |
| 21 | `EventModule.cs:78-91` | MED | 事件触发仅取第一个匹配，无优先级 | 加优先级排序 |
| 22 | `EventModule.cs:112-131` | MED | 条件不支持 `=` 运算符 | 增加支持 |
| 23 | `SimulationMainScreen.cs:396-399` | MED | 购买物品立即使用，不可囤积 | 去掉自动 UseItem |
| 24 | `TrainingMenuUI.cs:117-119` | MED | Close() 多次调用累积 AnimationFinished 订阅 | 加 `_isClosing` 标记 |
| 25 | `TrainingMenuUI.cs:117-119` | MED | PlayBackwards 无正向动画时 AnimationFinished 不触发 → 内存泄漏 | 加超时兜底 QueueFree |
| 26 | `SimulationMainScreen.cs:217-222` | MED | `_ExitTree` 仅清理了 ResponsiveManager 订阅 | 补充按钮事件取消订阅 |
| 27 | `DebugConsole.cs:86-89` | MED | Debug `load` 命令不刷新UI | 调用 `UpdateUI()` |
| 28 | `SimulationMainScreen.cs:207-211` | MED | 多语音同时播放堆积 | 播放新语音前停止旧的 |
| 29 | `OutingModule.cs:37` | LOW | 使用 `AddEnergy(-cost)` 而非 `ConsumeEnergy(cost)` | 统一API |
| 30 | `DebugConsole.cs:26-27` | LOW | `TextSubmitted` 未取消订阅 | 加 `_ExitTree` |
| 31 | 多处 | LOW | 场景路径硬编码散布在各处 | 提取为常量/ScenePaths类 |
| 32-43 | 各文件 | LOW | 详见完整报告 | — |

---

## 关联问题

- [01_story_player_engine.md](./01_story_player_engine.md) — FinishStory() 返回流程
- [04_core_infrastructure.md](./04_core_infrastructure.md) — FileIOManager 的 SaveBinary 用于存档
- [05_extensions_mods.md](./05_extensions_mods.md) — BehaviorRegistry.TriggerHook 是 InventoryModule 集合修改异常的关键触发方
