using System.Collections.Generic;

namespace UmaEraArchive.Core.Mods
{
    /// <summary>
    /// 表示一个模组提供的内容元数据声明
    /// </summary>
    public class ModContentDeclaration
    {
        public List<string> Components { get; set; } = new List<string>();
        public List<string> Stats { get; set; } = new List<string>();
        public List<string> Scenarios { get; set; } = new List<string>();
    }

    /// <summary>
    /// Mod 主类必须实现的接口
    /// </summary>
    public interface IMod
    {
        /// <summary>
        /// 声明该 Mod 提供的额外内容（组件、数值、养成剧本等）
        /// </summary>
        ModContentDeclaration GetContentDeclaration();

        /// <summary>
        /// 初始化 Mod
        /// </summary>
        void Initialize();

        /// <summary>
        /// 卸载 Mod（用于热重载或停用）
        /// </summary>
        void Unload();
    }
}
