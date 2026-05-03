using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using FileAccess = Godot.FileAccess;
using UmaEraArchive.Core.Models;

/// <summary>
/// 角色管理器，负责管理注册角色（来自资源包）和客串角色（来自剧本目录）。
/// </summary>
public static class CharacterManager
{
    // 已注册的马娘/固定角色 (ActorId -> Config)
    public static Dictionary<string, ActorConfigData> RegisteredActors { get; private set; } = new();
    
    // 当前剧本的客串角色 (ActorId -> Config)
    public static Dictionary<string, ActorConfigData> GuestActors { get; private set; } = new();

    // 兼容旧版 ID 的映射 (仅用于过渡)
    public static List<ActorConfigData> Characters => RegisteredActors.Values.Concat(GuestActors.Values).ToList();

    /// <summary>
    /// 加载全局/资源包角色配置
    /// </summary>
    public static void LoadRegisteredActors(string folderPath)
    {
        RegisteredActors.Clear();
        using var dir = DirAccess.Open(folderPath);
        if (dir == null) return;

        dir.ListDirBegin();
        string subDir = dir.GetNext();
        while (subDir != "")
        {
            if (dir.CurrentIsDir() && !subDir.StartsWith("."))
            {
                string configPath = folderPath.PathJoin(subDir).PathJoin("Data/actor_config.json");
                if (FileAccess.FileExists(configPath))
                {
                    var config = LoadConfig(configPath);
                    if (config != null) RegisteredActors[config.ActorId] = config;
                }
            }
            subDir = dir.GetNext();
        }
    }

    /// <summary>
    /// 加载剧本专有的客串角色
    /// </summary>
    public static void LoadGuestActors(string path)
    {
        GuestActors.Clear();
        if (!FileAccess.FileExists(path)) return;

        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            var configs = JsonSerializer.Deserialize<List<ActorConfigData>>(file.GetAsText());
            if (configs != null)
            {
                foreach (var cfg in configs) GuestActors[cfg.ActorId] = cfg;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CharacterManager] Failed to load guest actors: {ex.Message}");
        }
    }

    private static ActorConfigData LoadConfig(string path)
    {
        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            return JsonSerializer.Deserialize<ActorConfigData>(file.GetAsText());
        }
        catch { return null; }
    }

    /// <summary>
    /// 获取最匹配当前状态的悬浮对话 (Bark)
    /// </summary>
    public static BarkData GetBestBark(string actorId, umaEraArchive.Game.GameState state)
    {
        if (!RegisteredActors.TryGetValue(actorId, out var config)) return null;
        
        // 简单模拟条件评估
        foreach (var bark in config.Barks)
        {
            if (EvaluateBarkCondition(bark.Condition, state)) return bark;
        }
        return null;
    }

    private static bool EvaluateBarkCondition(string condition, umaEraArchive.Game.GameState state)
    {
        if (string.IsNullOrEmpty(condition)) return true;
        // 此处可复用 EventModule 的逻辑，暂时简化
        if (condition.Contains("energy < 30")) return state.Uma.GetCustomStat("energy") < 30;
        return false;
    }

    public static ActorConfigData GetActor(string id)
    {
        if (RegisteredActors.TryGetValue(id, out var r)) return r;
        if (GuestActors.TryGetValue(id, out var g)) return g;
        return null;
    }

    // 兼容层：按索引获取角色 (用于旧版 OptionButton 适配)
    public static ActorConfigData GetActorByIndex(int index)
    {
        var all = Characters;
        if (index >= 0 && index < all.Count) return all[index];
        return null;
    }

    public static void LoadCharacters(string path) => LoadGuestActors(path);
    public static void SaveCharacters(string path)
    {
        // 简单实现：将 GuestActors 序列化回文件
        string json = JsonSerializer.Serialize(GuestActors.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file?.StoreString(json);
    }
}
