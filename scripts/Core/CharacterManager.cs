using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using FileAccess = Godot.FileAccess;
using EraDream.Core.Models;
using EraDream.Core;

/// <summary>
/// 角色管理器，负责管理注册角色（来自资源包）和客串角色（来自剧本目录）。
/// </summary>
public static class CharacterManager
{
    // 已注册的马娘/固定角色 (ActorId -> Config)
    public static Dictionary<string, ActorConfigData> RegisteredActors { get; private set; } = new();

    // 保存马娘 ID 到其对应扩展包物理根路径的映射关系，以解决一包多马娘的路径定位问题
    public static Dictionary<string, string> ActorToExtensionPathMap { get; private set; } = new();
    
    // 当前剧本的客串角色 (ActorId -> Config)
    public static Dictionary<string, ActorConfigData> GuestActors { get; private set; } = new();

    // 兼容旧版 ID 的映射 (仅用于过渡)
    public static List<ActorConfigData> Characters => RegisteredActors.Values.Concat(GuestActors.Values).ToList();

    /// <summary>
    /// 加载全局/资源包角色配置，支持增量加载
    /// </summary>
    public static void LoadRegisteredActors(string folderPath, bool clear = true)
    {
        if (clear)
        {
            RegisteredActors.Clear();
            ActorToExtensionPathMap.Clear(); // 加载前清空映射
        }
        using var dir = DirAccess.Open(folderPath);
        if (dir == null) return;

        dir.ListDirBegin();
        string extDirName = dir.GetNext();
        while (extDirName != "")
        {
            if (dir.CurrentIsDir() && !extDirName.StartsWith("."))
            {
                // 仅允许加载当前处于激活状态的扩展包角色
                if (EraDream.Core.Extensions.ExtensionManager.Instance != null && !EraDream.Core.Extensions.ExtensionManager.Instance.IsExtensionActive(extDirName))
                {
                    extDirName = dir.GetNext();
                    continue;
                }
                string extRoot = folderPath.PathJoin(extDirName);
                
                // 1. [向下兼容] 加载包根目录下的 Data/actor_config.json
                string configPath = extRoot.PathJoin("Data/actor_config.json");
                if (FileAccess.FileExists(configPath))
                {
                    var config = LoadConfig(configPath);
                    if (config != null)
                    {
                        RegisteredActors[config.ActorId] = config;
                        ActorToExtensionPathMap[config.ActorId] = extRoot;
                    }
                }

                // 2. [多马娘支持] 扫描 Data/Characters/ 子目录
                string multiCharDir = extRoot.PathJoin("Data/Characters");
                if (DirAccess.DirExistsAbsolute(multiCharDir))
                {
                    using var charDir = DirAccess.Open(multiCharDir);
                    if (charDir != null)
                    {
                        charDir.ListDirBegin();
                        string charSubDir = charDir.GetNext();
                        while (charSubDir != "")
                        {
                            if (charDir.CurrentIsDir() && !charSubDir.StartsWith("."))
                            {
                                string charConfigPath = multiCharDir.PathJoin(charSubDir).PathJoin("actor_config.json");
                                if (FileAccess.FileExists(charConfigPath))
                                {
                                    var config = LoadConfig(charConfigPath);
                                    if (config != null)
                                    {
                                        RegisteredActors[config.ActorId] = config;
                                        ActorToExtensionPathMap[config.ActorId] = extRoot;
                                    }
                                }
                            }
                            charSubDir = charDir.GetNext();
                        }
                    }
                }
            }
            extDirName = dir.GetNext();
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
    public static BarkData GetBestBark(string actorId, EraDream.Game.GameState state)
    {
        if (!RegisteredActors.TryGetValue(actorId, out var config)) return null;
        
        // 简单模拟条件评估
        foreach (var bark in config.Barks)
        {
            if (EvaluateBarkCondition(bark.Condition, state)) return bark;
        }
        return null;
    }

    private static bool EvaluateBarkCondition(string condition, EraDream.Game.GameState state)
    {
        if (string.IsNullOrEmpty(condition)) return true;
        try
        {
            string clean = condition.Replace(" ", "");
            if (clean.Contains(">="))
            {
                var parts = clean.Split(new[] { ">=" }, StringSplitOptions.None);
                return GetUmaPropValue(parts[0], state.Uma) >= int.Parse(parts[1]);
            }
            if (clean.Contains("<="))
            {
                var parts = clean.Split(new[] { "<=" }, StringSplitOptions.None);
                return GetUmaPropValue(parts[0], state.Uma) <= int.Parse(parts[1]);
            }
            if (clean.Contains("<"))
            {
                var parts = clean.Split('<');
                return GetUmaPropValue(parts[0], state.Uma) < int.Parse(parts[1]);
            }
            if (clean.Contains(">"))
            {
                var parts = clean.Split('>');
                return GetUmaPropValue(parts[0], state.Uma) > int.Parse(parts[1]);
            }
        }
        catch (Exception ex) { GD.PrintErr($"[CharacterManager] EvaluateBarkCondition error: {ex.Message}"); }
        return false;
    }

    // 从马娘状态数据中动态获取指定属性，支持五维、体力和好感度等
    private static int GetUmaPropValue(string prop, EraDream.Game.UmaStats uma)
    {
        return prop.ToLower() switch
        {
            "energy" => uma.Energy,
            "actionstamina" or "action_stamina" => uma.ActionStamina,
            "affection" => uma.Affection,
            "speed" => uma.Speed,
            "stamina" => uma.Stamina,
            "power" => uma.Power,
            "guts" => uma.Guts,
            "intelligence" => uma.Intelligence,
            _ => uma.GetCustomStat(prop)
        };
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
    public static bool SaveCharacters(string path)
    {
        // 简单实现：将 GuestActors 序列化回文件
        return FileIOManager.SaveJson(path, GuestActors.Values.ToList());
    }

    /// <summary>
    /// 从扩展包物理路径加载马娘的养成配置 (simulation.json)
    /// </summary>
    public static EraDream.Core.Models.SimulationData LoadUmaSimulationData(string actorId)
    {
        // 1. 优先检查内存中已加载的 ActorConfigData 里是否已内嵌 SimulationData
        if (RegisteredActors.TryGetValue(actorId, out var actorConfig) && actorConfig?.Simulation != null)
        {
            GD.Print($"[CharacterManager] Found inline simulation data for {actorId}. skipping disk load.");
            return actorConfig.Simulation;
        }

        // 2. 否则向后兼容，在路径字典中查找该马娘所在的扩展包物理根路径进行物理加载
        if (!ActorToExtensionPathMap.TryGetValue(actorId, out string extRoot)) return null;
        if (string.IsNullOrEmpty(extRoot)) return null;

        // 优先定位 Data/Characters/[actorId]/simulation.json 目录，无则回退
        string simPath = System.IO.Path.Combine(extRoot, "Data", "Characters", actorId, "simulation.json");
        if (!System.IO.File.Exists(simPath))
        {
            simPath = System.IO.Path.Combine(extRoot, "Data", "simulation.json");
        }

        if (!System.IO.File.Exists(simPath)) return null;

        try
        {
            string json = System.IO.File.ReadAllText(simPath);
            return JsonSerializer.Deserialize<EraDream.Core.Models.SimulationData>(json);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CharacterManager] Failed to load simulation.json for {actorId}: {ex.Message}");
            return null;
        }
    }
}
