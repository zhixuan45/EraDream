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

            // 检查当前子窗口是否被嵌入游戏视口内
            bool isEmbedded = true;
            var window = caller.GetWindow();
            if (window != null)
            {
                isEmbedded = window.GetTree().Root.GuiEmbedSubwindows;
            }

            if (isEmbedded)
            {
                // 嵌入模式下，直接使用当前视口相对鼠标坐标
                Vector2 mousePos = caller.GetViewport().GetMousePosition();
                menu.Popup(new Rect2I((Vector2I)mousePos, Vector2I.Zero));
            }
            else
            {
                // 原生非嵌入窗口模式下，PopupMenu 视为独立系统窗口，必须使用屏幕绝对鼠标位置
                Vector2I screenMousePos = DisplayServer.MouseGetPosition();
                menu.Popup(new Rect2I(screenMousePos, Vector2I.Zero));
            }
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

            Vector2 targetPos = target.GetGlobalRect().Position;
            bool isEmbedded = true;
            var window = target.GetWindow();
            if (window != null)
            {
                isEmbedded = window.GetTree().Root.GuiEmbedSubwindows;
            }

            // 非嵌入模式下，额外加上游戏窗口在操作系统屏幕上的物理绝对偏移
            if (!isEmbedded && window != null)
            {
                targetPos += window.Position;
            }

            menu.Popup(new Rect2I((Vector2I)targetPos + offset, Vector2I.Zero));
        }
    }
}
