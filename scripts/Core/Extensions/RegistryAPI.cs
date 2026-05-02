using Godot;
using System;
using System.Collections.Generic;

namespace UmaEraArchive.Core.Extensions
{
    /// <summary>
    /// 扩展包注册 API，强制执行唯一 ID 规范。
    /// </summary>
    public static class RegistryAPI
    {
        private static HashSet<string> _registeredStatIds = new();

        /// <summary>
        /// 注册一个新的数值属性 ID。
        /// 必须包含冒号分隔的命名空间，例如 "mod_id:stat_name"
        /// </summary>
        public static bool RegisterStat(string id)
        {
            if (string.IsNullOrEmpty(id) || !id.Contains(":"))
            {
                GD.PrintErr($"[RegistryAPI] Invalid Stat ID: '{id}'. ID must follow 'namespace:name' format.");
                return false;
            }

            if (_registeredStatIds.Contains(id))
            {
                GD.PrintErr($"[RegistryAPI] Duplicate Stat ID registration attempted: '{id}'");
                return false;
            }

            _registeredStatIds.Add(id);
            GD.Print($"[RegistryAPI] Stat registered: {id}");
            return true;
        }

        public static bool IsStatRegistered(string id) => _registeredStatIds.Contains(id);

        /// <summary>
        /// 批量验证 ID 规范
        /// </summary>
        public static void ValidateIds(IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                if (!id.Contains(":"))
                {
                    GD.PrintErr($"[RegistryAPI] ID Verification Failed: '{id}' lacks a namespace (author_name:id).");
                }
            }
        }
    }
}
