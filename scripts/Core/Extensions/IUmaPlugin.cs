namespace EraDream.Core.Extensions
{
    /// <summary>
    /// 扩展包逻辑入口接口。
    /// 仅玩法扩展包 (Gameplay Pack) 的 DLL 实现此接口会被加载。
    /// </summary>
    public interface IUmaPlugin
    {
        /// <summary>
        /// 插件加载时调用，用于注册属性 ID
        /// </summary>
        void OnLoad();

        /// <summary>
        /// 插件卸载时调用，用于清理资源、断开信号等
        /// </summary>
        void OnUnload();

        /// <summary>
        /// 剧本开始时调用
        /// </summary>
        void OnScenarioStart();

        /// <summary>
        /// 回合开始前的钩子
        /// </summary>
        void OnTurnStart(int turn);

        /// <summary>
        /// 回合结束（推进前）的钩子
        /// </summary>
        void OnTurnEnd(int turn);
    }
}
