using System;
using System.Collections.Generic;

namespace EraDream.Core
{
    // 全局剧情变量状态管理器
    public class GlobalGameState
    {
        private static GlobalGameState _instance;
        public static GlobalGameState Instance => _instance ??= new GlobalGameState();

        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>();

        public event Action<string, object> OnVariableChanged;

        public void SetVariable(string key, object value)
        {
            _variables[key] = value;
            OnVariableChanged?.Invoke(key, value);
        }

        public T GetVariable<T>(string key, T defaultValue = default)
        {
            if (_variables.TryGetValue(key, out var val))
            {
                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public bool HasVariable(string key) => _variables.ContainsKey(key);

        public void Clear()
        {
            _variables.Clear();
        }
    }
}
