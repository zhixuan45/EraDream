using Godot;
using System;
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

    public const string AutoSavePath = "user://autosave.json";
    
    // 子模块引用
    public TrainingModule Training { get; private set; }
    public RestModule Rest { get; private set; }
    public EventModule Events { get; private set; }

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
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
    public void StartNewGame(System.Collections.Generic.List<string> scenarioPaths)
    {
        CurrentState = new GameState();
        if (scenarioPaths != null)
        {
            CurrentState.ScenarioPaths.AddRange(scenarioPaths);
        }
        GD.Print($"[GameManager] Started new simulation game with {CurrentState.ScenarioPaths.Count} scenarios.");
    }

    public void SaveGame(string path)
    {
        if (CurrentState == null) return;
        FileIOManager.SaveJson(path, CurrentState);
        GD.Print($"[GameManager] Game saved to: {path}");
    }

    public void LoadGame(string path)
    {
        var loadedState = FileIOManager.LoadJson<GameState>(path);
        if (loadedState != null)
        {
            CurrentState = loadedState;
            GD.Print($"[GameManager] Game loaded from: {path}");
        }
        else
        {
            GD.PrintErr($"[GameManager] Load failed, file might not exist or corrupted: {path}");
        }
    }

    public void AutoSave()
    {
        SaveGame(AutoSavePath);
    }

    /// <summary>
    /// 推进回合（在执行完主要指令后调用）
    /// </summary>
    public void AdvanceTurn()
    {
        if (CurrentState == null || CurrentState.IsGameOver) return;

        CurrentState.NextTurn();
        GD.Print($"[GameManager] Advanced to turn {CurrentState.CurrentTurn}");

        // 回合推进后检查事件
        Events.CheckAndTriggerTurnEvent(CurrentState);

        // 自动存档
        AutoSave();
    }
}
