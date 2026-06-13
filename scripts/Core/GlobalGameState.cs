using Godot;
using System.Collections.Generic;

namespace EraDream.Core
{
    /// <summary>
    /// 全局游戏状态，存储剧情运行时的所有变量 ID 和数值
    /// </summary>
    public partial class GlobalGameState : Node
    {
        public static GlobalGameState Instance { get; private set; }

        // 存储变量，如 "favor_points" -> 20
        private Dictionary<string, float> _variables = new Dictionary<string, float>();

        public override void _EnterTree()
        {
            // 提前在树进入阶段赋值，避免其他 Autoload 在 _Ready 中拿到 null
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
