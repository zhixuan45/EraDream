using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UmaEraArchive.Core;

namespace umaEraArchive.Game;

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
    public RestModule Rest { get; private set; }
    public EventModule Events { get; private set; }

    // 生命周期事件钩子
    public event Action<int> OnTurnStart;
    public event Action<int> OnTurnEnd;
    public event Action OnGameStarted;

    public override void _EnterTree()
    {
        GD.Print("[GameManager] _EnterTree called.");
        if (Instance == null)
        {
            Instance = this;
            GD.Print("[GameManager] Singleton Instance assigned.");
        }
        else
        {
            GD.Print("[GameManager] Singleton Instance already exists, freeing this duplicate.");
            QueueFree();
            return;
        }

        InitializeModules();
    }

    private void InitializeModules()
    {
        // 动态创建并挂载子模块节点
        Training = new TrainingModule();
        AddChild(Training);

        Rest = new RestModule();
        AddChild(Rest);

        Events = new EventModule();
        AddChild(Events);
        
        StartNewGame(new System.Collections.Generic.List<string>());
    }

    /// <summary>
    /// 开启新的一局游戏
    /// </summary>
    public void StartNewGame(List<string> scenarioPaths, List<string> characterPaths = null, List<string> modPaths = null)
    {
        CurrentState = new GameState();
        if (scenarioPaths != null)
        {
            CurrentState.ScenarioPaths.AddRange(scenarioPaths);
        }
        if (characterPaths != null)
        {
            CurrentState.CharacterPaths.AddRange(characterPaths);
        }
        if (modPaths != null)
        {
            CurrentState.ModPaths.AddRange(modPaths);
        }

        Events.LoadEventPool(CurrentState.ScenarioPaths);
        GD.Print($"[GameManager] New game: {CurrentState.ScenarioPaths.Count} scenarios, {CurrentState.CharacterPaths.Count} characters, {CurrentState.ModPaths.Count} mods.");

        OnGameStarted?.Invoke();
        OnTurnStart?.Invoke(CurrentState.CurrentTurn);
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

    /// <summary>
    /// 回合开始时的逻辑（由 UI 或系统调用）
    /// </summary>
    public void HandleTurnStart()
    {
        if (CurrentState == null || CurrentState.IsGameOver) return;

        // 检查回合开始剧情，如果触发了剧情则直接跳转
        if (Events.CheckAndTriggerStory(TriggerTiming.TurnStart, CurrentState))
        {
            return;
        }
    }

    /// <summary>
    /// 推进回合（在执行完主要指令后调用）
    /// </summary>
    public void AdvanceTurn()
    {
        if (CurrentState == null || CurrentState.IsGameOver) return;

        OnTurnEnd?.Invoke(CurrentState.CurrentTurn);

        CurrentState.NextTurn();
        GD.Print($"[GameManager] Advanced to turn {CurrentState.CurrentTurn}");

        // 自动存档
        AutoSave();

        // 检查回合结束剧情
        CheckTurnEndStory();

        OnTurnStart?.Invoke(CurrentState.CurrentTurn);
    }

    private void CheckTurnEndStory()
    {
        if (CurrentState == null) return;
        Events.CheckAndTriggerStory(TriggerTiming.TurnEnd, CurrentState);
    }
}
