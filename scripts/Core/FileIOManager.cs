using Godot;
using System;

namespace UmaArchive.Core
{
    public static class FileIOManager
    {
        /// <summary>
        /// 唤醒平台原生保存对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="defaultName">默认文件名</param>
        /// <param name="filter">文件过滤器 (如 *.json)</param>
        /// <param name="onFileSelected">用户选择路径后的回调</param>
        public static void OpenSaveDialog(string title, string defaultName, string filter, Action<string> onFileSelected)
        {
            // 定义过滤器回调
            string[] filters = { filter };
            
            // 使用 Callable.From 包装 C# Action 供 Godot 信号/回调使用
            Callable callback = Callable.From((string path) => {
                if (!string.IsNullOrEmpty(path))
                {
                    onFileSelected?.Invoke(path);
                }
            });

            // 唤醒原生对话框 (Godot 4.x API)
            DisplayServer.FileDialogShow(
                title,
                ProjectSettings.GlobalizePath("user://"), // 初始目录
                defaultName,
                false, // show_hidden
                DisplayServer.FileDialogMode.SaveFile,
                filters,
                callback
            );
        }

        /// <summary>
        /// 唤醒平台原生打开对话框
        /// </summary>
        public static void OpenLoadDialog(string title, string filter, Action<string> onFileSelected)
        {
            string[] filters = { filter };
            Callable callback = Callable.From((string path) => {
                if (!string.IsNullOrEmpty(path))
                {
                    onFileSelected?.Invoke(path);
                }
            });

            DisplayServer.FileDialogShow(
                title,
                ProjectSettings.GlobalizePath("user://"),
                "",
                false, // show_hidden
                DisplayServer.FileDialogMode.OpenFile,
                filters,
                callback
            );
        }
    }
}
