using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Godot;
using UmaEraArchive.Core.Extensions;

namespace UmaEraArchive.Core.Mods
{
    /// <summary>
    /// 自定义的 AssemblyLoadContext，用于隔离加载每个 Mod
    /// </summary>
    public class ModAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _modDirectory;

        public ModAssemblyLoadContext(string name, string modDirectory) : base(name, isCollectible: true)
        {
            _modDirectory = modDirectory;
            // 注册解析事件，以便主类导入同目录其他 DLL 时能被自动发现并加载
            Resolving += OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string assemblyPath = Path.Combine(_modDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(assemblyPath))
            {
                return context.LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // 默认让基础上下文尝试加载
            return null;
        }
    }

    /// <summary>
    /// 全局 Mod 加载与管理器
    /// </summary>
    public class ModLoader
    {
        public static ModLoader Instance { get; } = new ModLoader();

        private readonly Dictionary<string, ModInfo> _loadedMods = new();

        public class ModInfo
        {
            public string ModId { get; set; }
            public ModAssemblyLoadContext LoadContext { get; set; }
            public IUmaPlugin ModInstance { get; set; }
        }

        /// <summary>
        /// 加载指定的 Mod DLL
        /// </summary>
        /// <param name="modId">扩展包 ID</param>
        /// <param name="dllPath">主类 DLL 的绝对路径</param>
        /// <returns>是否成功加载</returns>
        public bool LoadMod(string modId, string dllPath)
        {
            if (_loadedMods.ContainsKey(modId))
            {
                GD.PrintErr($"[ModLoader] Mod '{modId}' is already loaded.");
                return false;
            }

            if (!File.Exists(dllPath))
            {
                GD.PrintErr($"[ModLoader] DLL not found: {dllPath}");
                return false;
            }

            ModAssemblyLoadContext loadContext = null;
            try
            {
                string modDirectory = Path.GetDirectoryName(dllPath);

                // 创建一个独立的 LoadContext 以便后续可以卸载或热重载
                loadContext = new ModAssemblyLoadContext($"ModContext_{modId}", modDirectory);

                // 加载主 DLL
                Assembly modAssembly = loadContext.LoadFromAssemblyPath(dllPath);

                // 寻找实现了 IUmaPlugin 接口的类
                var modType = modAssembly.GetTypes().FirstOrDefault(t => typeof(IUmaPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                if (modType == null)
                {
                    GD.PrintErr($"[ModLoader] Could not find any class implementing IUmaPlugin in {dllPath}.");
                    loadContext.Unload();
                    return false;
                }

                // 实例化
                var modInstance = (IUmaPlugin)Activator.CreateInstance(modType);

                // 初始化
                modInstance.OnLoad();

                var modInfo = new ModInfo
                {
                    ModId = modId,
                    LoadContext = loadContext,
                    ModInstance = modInstance
                };

                _loadedMods[modId] = modInfo;
                GD.Print($"[ModLoader] Successfully loaded mod '{modId}'.");

                return true;
            }
            catch (ReflectionTypeLoadException ex)
            {
                loadContext?.Unload();
                string loaderExceptions = string.Join("\n", ex.LoaderExceptions.Select(e => e.Message));
                GD.PrintErr($"[ModLoader] Failed to load mod '{modId}' from {dllPath}. ReflectionTypeLoadException: {ex.Message}\nLoaderExceptions:\n{loaderExceptions}\n{ex.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                loadContext?.Unload();
                GD.PrintErr($"[ModLoader] Failed to load mod '{modId}' from {dllPath}: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 卸载特定的 Mod，支持热重载
        /// </summary>
        public void UnloadMod(string modId)
        {
            if (_loadedMods.TryGetValue(modId, out var modInfo))
            {
                try
                {
                    modInfo.ModInstance.OnUnload();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[ModLoader] Error unloading mod '{modId}': {ex.Message}");
                }

                modInfo.LoadContext.Unload();
                _loadedMods.Remove(modId);
                GD.Print($"[ModLoader] Successfully unloaded mod '{modId}'.");
            }
        }

        /// <summary>
        /// 获取所有已加载的 Mods
        /// </summary>
        public IEnumerable<ModInfo> GetLoadedMods()
        {
            return _loadedMods.Values;
        }
    }
}
