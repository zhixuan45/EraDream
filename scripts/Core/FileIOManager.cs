using Godot;
using System;
using System.Linq;

namespace UmaArchive.Core
{
    public static class FileIOManager
    {
        /// <summary>
        /// 唤醒平台原生保存对话框
        /// </summary>
        public static void OpenSaveDialog(string title, string defaultName, string filter, Action<string> onFileSelected)
        {
            string[] filters = { filter };
            
            // Godot 4 FileDialogShow 的回调签名必须是 (bool status, string[] paths, int index)
            Callable callback = Callable.From((bool status, string[] paths, int index) => {
                if (status && paths.Length > 0)
                {
                    onFileSelected?.Invoke(paths[0]);
                }
            });

            DisplayServer.FileDialogShow(
                title,
                ProjectSettings.GlobalizePath("user://"),
                defaultName,
                false,
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
            Callable callback = Callable.From((bool status, string[] paths, int index) => {
                if (status && paths.Length > 0)
                {
                    onFileSelected?.Invoke(paths[0]);
                }
            });

            DisplayServer.FileDialogShow(
                title,
                ProjectSettings.GlobalizePath("user://"),
                "",
                false,
                DisplayServer.FileDialogMode.OpenFile,
                filters,
                callback
            );
        }

        /// <summary>
        /// 唤醒平台原生选择文件夹对话框
        /// </summary>
        public static void OpenFolderDialog(string title, Action<string> onFolderSelected)
        {
            Callable callback = Callable.From((bool status, string[] paths, int index) => {
                if (status && paths.Length > 0)
                {
                    onFolderSelected?.Invoke(paths[0]);
                }
            });

            DisplayServer.FileDialogShow(
                title,
                ProjectSettings.GlobalizePath("user://"),
                "",
                false,
                DisplayServer.FileDialogMode.OpenDir,
                new string[] { },
                callback
            );
        }
    }
}
