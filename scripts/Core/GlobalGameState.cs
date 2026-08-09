using Godot;
using System.Collections.Generic;
using EraDream.Game;

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
            // 剧情变量变更与普通养成状态共用游戏自动存档防抖器。
            GameManager.Instance?.MarkSaveDirty("剧情全局变量变更");
        }

        public float GetVariable(string id)
        {
            return _variables.ContainsKey(id) ? _variables[id] : 0f;
        }

        // 存档系统使用副本，避免外部代码直接持有内部可变字典。
        public Dictionary<string, float> ExportVariables()
        {
            return new Dictionary<string, float>(_variables);
        }

        // 读档时一次性回灌剧情变量，不逐条触发新的自动存档。
        public void ImportVariables(Dictionary<string, float> variables)
        {
            _variables = variables != null
                ? new Dictionary<string, float>(variables)
                : new Dictionary<string, float>();
        }

        public void Reset()
        {
            _variables.Clear();
        }
    }
}
