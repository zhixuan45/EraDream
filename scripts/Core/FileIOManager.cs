using Godot;
using System;
using System.Linq;

namespace UmaEraArchive.Core
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
            if (OS.HasFeature("android"))
            {
                var fileDialog = new Godot.FileDialog();
                fileDialog.Title = title;
                fileDialog.Access = Godot.FileDialog.AccessEnum.Filesystem;
                fileDialog.FileMode = Godot.FileDialog.FileModeEnum.OpenDir;
                fileDialog.UseNativeDialog = true;
                
                var root = ((SceneTree)Engine.GetMainLoop()).Root;
                root.AddChild(fileDialog);
                
                // SAF 返回 content:// URI，需转换为真实路径
                fileDialog.DirSelected += (string dir) => {
                    string realPath = ConvertContentUriToPath(dir);
                    GD.Print($"[FileIO] SAF raw: {dir} -> resolved: {realPath}");
                    onFolderSelected?.Invoke(realPath);
                    fileDialog.QueueFree();
                };
                fileDialog.Canceled += () => {
                    fileDialog.QueueFree();
                };
                
                // 延迟弹出，等 FileDialog 完全加入场景树后再显示
                fileDialog.CallDeferred("popup_centered");
                return;
            }

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

        /// <summary>
        /// 将安卓 SAF 返回的 content:// URI 转换为真实文件系统路径
        /// 例: content://com.android.externalstorage.documents/tree/primary%3Auma%2Ftest
        ///   -> /storage/emulated/0/uma/test
        /// </summary>
        private static string ConvertContentUriToPath(string uri)
        {
            // 如果已经是普通路径，直接返回
            if (!uri.StartsWith("content://"))
                return uri;

            try
            {
                // 提取 tree/ 后面的编码路径部分
                string treePrefix = "/tree/";
                int treeIdx = uri.IndexOf(treePrefix, StringComparison.Ordinal);
                if (treeIdx < 0) return uri;

                string encoded = uri.Substring(treeIdx + treePrefix.Length);
                // URL 解码: %3A -> :, %2F -> /
                string decoded = System.Uri.UnescapeDataString(encoded);

                // 格式为 "primary:path/to/folder" 或 "XXXX-XXXX:path"
                int colonIdx = decoded.IndexOf(':');
                if (colonIdx < 0) return uri;

                string storageId = decoded.Substring(0, colonIdx);
                string subPath = decoded.Substring(colonIdx + 1);

                // primary 对应内部存储根目录
                if (storageId.Equals("primary", StringComparison.OrdinalIgnoreCase))
                {
                    return "/storage/emulated/0/" + subPath;
                }
                else
                {
                    // SD 卡或其他外部存储卷
                    return "/storage/" + storageId + "/" + subPath;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[FileIO] Failed to parse content URI: {uri}, error: {ex.Message}");
                return uri;
            }
        }
    }
}
