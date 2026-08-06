using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EraDream.Core;
using EraDream.StoryEditor.Nodes;

namespace EraDream.Game;

/// <summary>
/// 养成系统主核心调度器，可作为Autoload或场景根节点
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    public const string AutoSavePath = "user://autosave.sav";
    
    // 子模块引用
    public TrainingModule Training { get; private set; }
    public OutingModule Outing { get; private set; }
    public RestModule Rest { get; private set; }
    public EventModule Events { get; private set; }
    public WorkModule Work { get; private set; }
    public ShopModule Shop { get; private set; }
    public InventoryModule Inventory { get; private set; }

    // 全局扩展引用
    public EraDream.Core.Extensions.ExtensionManager ExtensionManager { get; private set; }
    public EraDream.Core.Extensions.BehaviorRegistry BehaviorRegistry { get; private set; }

    // 生命周期事件钩子
    public event Action<int> OnTurnStart;
    public event Action<int> OnTurnEnd;
    public event Action OnGameStarted;
    public event Action OnGameOver;

    public override void _EnterTree()
    {
        GD.Print("[GameManager] _EnterTree called.");
        if (Instance == null)
        {
            Instance = this;
            GD.Print("[GameManager] Singleton Instance assigned.");
            InitializeModules();
        }
        else if (Instance != this)
        {
            GD.Print("[GameManager] Singleton Instance already exists, freeing this duplicate.");
            QueueFree();
            return;
        }
    }

    private void InitializeModules()
    {
        // 动态创建并挂载子模块节点
        Training = new TrainingModule();
        AddChild(Training);

        Outing = new OutingModule();
        AddChild(Outing);

        Rest = new RestModule();
        AddChild(Rest);

        Events = new EventModule();
        AddChild(Events);

        Work = new WorkModule();
        AddChild(Work);

        Shop = new ShopModule();
        AddChild(Shop);

        Inventory = new InventoryModule();
        AddChild(Inventory);

        // 初始化扩展与行为引擎
        ExtensionManager = EraDream.Core.Extensions.ExtensionManager.Instance;
        BehaviorRegistry = EraDream.Core.Extensions.BehaviorRegistry.Instance;

        if (BehaviorRegistry == null)
        {
            BehaviorRegistry = new EraDream.Core.Extensions.BehaviorRegistry();
            AddChild(BehaviorRegistry);
        }

        if (Events != null)
        {
            Events.StoryTriggered += OnStoryTriggered;
        }

        StartNewGame(new System.Collections.Generic.List<string>());
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            if (Events != null)
            {
                Events.StoryTriggered -= OnStoryTriggered;
            }
            Instance = null;
        }
    }

    private void OnStoryTriggered(string path, string startNodeId)
    {
        StoryPlayerEngine.CurrentStoryPath = path;
        StoryPlayerEngine.StartNodeId = startNodeId;
        StoryPlayerEngine.ReturnScenePath = "res://scenes/SimulationMainScreen.tscn";
        
        GetTree().ChangeSceneToFile("res://scenes/StoryPlayerEngine.tscn");
    }

    /// <summary>
    /// 开启新的一局游戏
    /// </summary>
    public void StartNewGame(List<string> scenarioPaths, List<string> characterPaths = null, List<string> modPaths = null)
    {
        CurrentState = new GameState();
        if (scenarioPaths == null || scenarioPaths.Count == 0)
        {
            // 至少传入默认测试剧本路径
            scenarioPaths = new List<string> { "res://scenarios/default/" };
        }
        CurrentState.ScenarioPaths.AddRange(scenarioPaths);
        if (characterPaths != null)
        {
            CurrentState.CharacterPaths.AddRange(characterPaths);
        }
        if (modPaths != null)
        {
            CurrentState.ModPaths.AddRange(modPaths);
        }

        // 初始化刷新马娘池
        RefreshScoutPool();

        Events.LoadEventPool(CurrentState.ScenarioPaths);
        GD.Print($"[GameManager] New game: {CurrentState.ScenarioPaths.Count} scenarios, {CurrentState.CharacterPaths.Count} characters, {CurrentState.ModPaths.Count} mods.");

        OnGameStarted?.Invoke();
        BehaviorRegistry?.TriggerHook("OnScenarioStart", CurrentState);
        OnTurnStart?.Invoke(CurrentState.CurrentTurn);
    }

    /// <summary>
    /// 刷新运动场上的随机马娘签约池，固定提供 3 位马娘
    /// </summary>
    public void RefreshScoutPool()
    {
        if (CurrentState == null) return;
        CurrentState.CurrentScoutPool.Clear();

        var availableIds = new List<string>();
        // 获取所有可用的马娘 ID
        foreach (var charData in CharacterManager.Characters)
        {
            if (!string.IsNullOrEmpty(charData.ActorId)) availableIds.Add(charData.ActorId);
        }

        // 若无已加载马娘，注入默认测试 ID 供手动测试使用
        if (availableIds.Count == 0)
        {
            availableIds.Add("test.manual_uma");
            availableIds.Add("special_uma_silence");
            availableIds.Add("special_uma_goldship");
        }

        var rng = new Random();
        var selected = availableIds.OrderBy(x => rng.Next()).Take(3).ToList();
        CurrentState.CurrentScoutPool.AddRange(selected);
        GD.Print($"[GameManager] Scout pool refreshed with {CurrentState.CurrentScoutPool.Count} characters.");
    }

    /// <summary>
    /// 扣除金币并刷新随机马娘签约池
    /// </summary>
    public bool RefreshScoutPoolWithCost(int cost)
    {
        if (CurrentState == null) return false;
        if (cost <= 0)
        {
            GD.PushError($"[GameManager] RefreshScoutPoolWithCost called with invalid cost: {cost}");
            return false;
        }
        if (CurrentState.Player.Money < cost) return false;

        CurrentState.Player.AddMoney(-cost);
        RefreshScoutPool();
        return true;
    }

    /// <summary>
    /// 签约指定的马娘并初始化其在 GameState.Uma 中的养成数值
    /// </summary>
    public bool ContractUma(string id)
    {
        if (CurrentState == null) return false;
        if (!CurrentState.CurrentScoutPool.Contains(id)) return false;

        CurrentState.ActiveUmaId = id;
        CurrentState.CurrentScoutPool.Clear();

        // 尝试加载该马娘的 simulation.json 数据（支持从内嵌或物理文件加载）
        var simData = CharacterManager.LoadUmaSimulationData(id);
        var stats = simData?.Stats;

        var initial = stats?.Initial;
        CurrentState.Uma.Speed = initial?.GetValueOrDefault("speed", 100) ?? 100;
        CurrentState.Uma.Stamina = initial?.GetValueOrDefault("stamina", 100) ?? 100;
        CurrentState.Uma.Power = initial?.GetValueOrDefault("power", 100) ?? 100;
        CurrentState.Uma.Guts = initial?.GetValueOrDefault("guts", 100) ?? 100;
        CurrentState.Uma.Intelligence = initial?.GetValueOrDefault("intelligence", 100) ?? 100;
        CurrentState.Uma.SkillPoints = initial?.GetValueOrDefault("skill_points", 10) ?? 10;

        var conditions = stats?.Conditions;
        int moodVal = 75;
        if (conditions != null && conditions.ContainsKey("motivation"))
        {
            int mot = conditions["motivation"];
            moodVal = mot switch {
                1 => 10, 2 => 35, 3 => 75, 4 => 110, 5 => 140, _ => 75
            };
        }
        else if (conditions != null && conditions.ContainsKey("mood"))
        {
            moodVal = conditions["mood"];
        }
        CurrentState.Uma.Mood = moodVal;
        CurrentState.Uma.Energy = conditions?.GetValueOrDefault("energy", 100) ?? 100;
        CurrentState.Uma.MaxEnergy = conditions?.GetValueOrDefault("energy", 100) ?? 100;
        CurrentState.Uma.Affection = conditions?.GetValueOrDefault("affection", 0) ?? 0;

        if (stats?.CustomStats != null)
        {
            foreach (var kvp in stats.CustomStats)
            {
                CurrentState.Uma.CustomStats[kvp.Key] = kvp.Value;
            }
        }
        GD.Print($"[GameManager] Contracted {id}. Base stats loaded (with optional fallback options).");

        BehaviorRegistry?.TriggerHook($"OnContract_{id}", CurrentState);
        BehaviorRegistry?.TriggerHook("OnContract", CurrentState);
        AutoSave();
        return true;
    }

    public void SaveGame(string path)
    {
        if (CurrentState == null) return;
        FileIOManager.SaveBinary(path, CurrentState);
        GD.Print($"[GameManager] Game binary saved to: {path}");
        
        // 记录最近存档路径
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.LastSavePath = path;
        }
    }

    public void LoadGame(string path)
    {
        var loadedState = FileIOManager.LoadBinary<GameState>(path);
        if (loadedState != null)
        {
            CurrentState = loadedState;
            Events.LoadEventPool(CurrentState.ScenarioPaths);
            GD.Print($"[GameManager] Game binary loaded from: {path}");
        }
        else
        {
            GD.PrintErr($"[GameManager] Binary load failed, file might not exist or corrupted: {path}");
        }
    }

    public void AutoSave()
    {
        SaveGame(AutoSavePath);
    }

    private bool _hasTriggeredTurnStartThisTurn = false;

    /// <summary>
    /// 回合开始时的逻辑（由 UI 或系统调用）
    /// </summary>
    public void HandleTurnStart()
    {
        if (CurrentState == null || CurrentState.IsGameOver) return;
        if (_hasTriggeredTurnStartThisTurn) return;

        _hasTriggeredTurnStartThisTurn = true;
        BehaviorRegistry?.TriggerHook("OnTurnStart", CurrentState);
        // 检查回合开始剧情，如果触发了剧情则直接跳转
        if (Events.CheckAndTriggerStory(TriggerTiming.TurnStart, CurrentState))
        {
            return;
        }
    }

    /// <summary>
    /// 推进回合（在玩家选择休息或本周结束时调用）
    /// </summary>
    public void AdvanceTurn()
    {
        if (CurrentState == null || CurrentState.IsGameOver) return;

        BehaviorRegistry?.TriggerHook("OnTurnEnd", CurrentState);
        OnTurnEnd?.Invoke(CurrentState.CurrentTurn);

        // 更新物品效果 (持续物品扣减回合 & 触发 Tick)
        Inventory.UpdateTurnEffects(CurrentState);

        // 每周结束时的资源恢复逻辑
        CurrentState.Player.AddStamina(30); // 基础恢复
        CurrentState.Player.AddEnergy(10);

        // 仅在已签约马娘时恢复马娘属性
        if (!string.IsNullOrEmpty(CurrentState.ActiveUmaId))
        {
            CurrentState.Uma.AddActionStamina(40);
            CurrentState.Uma.AddEnergy(20);
            // 心情随时间自然衰减（普通以下不衰减，绝好/好缓慢衰减）
            if (CurrentState.Uma.Mood > 75) CurrentState.Uma.AddMood(-5);
        }
        else
        {
            // 若未签约，则新一回合自动刷新运动场签约池
            RefreshScoutPool();
        }

        CurrentState.NextTurn();
        _hasTriggeredTurnStartThisTurn = false; // 新回合重置触发状态
        GD.Print($"[GameManager] Advanced to turn {CurrentState.CurrentTurn}");

        if (CurrentState.IsGameOver)
        {
            TriggerEnding();
            return;
        }

        // 自动存档
        AutoSave();

        // 检查回合开始剧情，若触发则直接返回（防 use-after-free）
        if (CheckTurnStartStory())
        {
            return;
        }

        BehaviorRegistry?.TriggerHook("OnTurnStart", CurrentState);
        OnTurnStart?.Invoke(CurrentState.CurrentTurn);
    }

    private bool CheckTurnStartStory()
    {
        if (CurrentState == null) return false;
        // 如果此回合开始就已经通过 HandleTurnStart 触发过了，就不要再触发了
        if (_hasTriggeredTurnStartThisTurn) return false;
        
        bool triggered = Events.CheckAndTriggerStory(TriggerTiming.TurnStart, CurrentState);
        if (triggered)
        {
            _hasTriggeredTurnStartThisTurn = true;
        }
        return triggered;
    }

    private void TriggerEnding()
    {
        GD.Print("[GameManager] Game over! Reached max turns.");
        OnGameOver?.Invoke();
    }
}
