using Godot;
using System;

namespace UmaEraArchive.Core
{
    /// <summary>
    /// UI 通用工具类，处理跨平台的 UI 兼容性逻辑
    /// </summary>
    public static class UIUtils
    {
        /// <summary>
        /// 在鼠标当前位置弹出右键菜单，自动处理窗口嵌入模式下的坐标转换
        /// </summary>
        /// <param name="menu">要弹出的 PopupMenu 节点</param>
        /// <param name="caller">调用者节点（用于获取 Viewport）</param>
        public static void ShowContextMenu(PopupMenu menu, Control caller)
        {
            if (menu == null || caller == null) return;

            // 获取当前 Viewport 的鼠标位置（像素坐标）
            Vector2 mousePos = caller.GetViewport().GetMousePosition();
            
            // 使用 Popup(Rect2I) 确保在 embed_subwindows=false 时也能正确显示
            // Rect2I 的 size 设置为 Zero 即可，PopupMenu 会自动计算自身大小
            menu.Popup(new Rect2I((Vector2I)mousePos, Vector2I.Zero));
        }

        /// <summary>
        /// 在指定按钮或控件下方弹出菜单
        /// </summary>
        /// <param name="menu">PopupMenu 节点</param>
        /// <param name="target">对齐的目标控件</param>
        /// <param name="offset">相对偏移</param>
        public static void ShowMenuAtControl(PopupMenu menu, Control target, Vector2I offset = default)
        {
            if (menu == null || target == null) return;

            // 获取目标控件在窗口内的全局位置
            Vector2 targetPos = target.GetGlobalRect().Position;
            menu.Popup(new Rect2I((Vector2I)targetPos + offset, Vector2I.Zero));
        }
    }
}
