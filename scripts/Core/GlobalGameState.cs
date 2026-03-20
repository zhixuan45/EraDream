using Godot;
using System.Collections.Generic;

namespace UmaEraArchive.Core
{
    /// <summary>
    /// 全局游戏状态，存储剧情运行时的所有变量 ID 和数值
    /// </summary>
    public partial class GlobalGameState : Node
    {
        public static GlobalGameState Instance { get; private set; }

        // 存储变量，如 "favor_points" -> 20
        private Dictionary<string, float> _variables = new Dictionary<string, float>();

        public override void _Ready()
        {
            Instance = this;
        }

        public void SetVariable(string id, float value)
        {
            _variables[id] = value;
            GD.Print($"Variable Updated: {id} = {value}");
        }

        public float GetVariable(string id)
        {
            return _variables.ContainsKey(id) ? _variables[id] : 0f;
        }

        public void Reset()
        {
            _variables.Clear();
        }
    }
}
