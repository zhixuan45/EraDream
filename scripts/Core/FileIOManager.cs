using Godot;
using System;
using System.Linq;
using System.Text.Json;

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
                    string realPath = ConvertContentUriToPath(paths[0]);
                    onFileSelected?.Invoke(realPath);
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
                    string realPath = ConvertContentUriToPath(paths[0]);
                    onFileSelected?.Invoke(realPath);
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
                    string realPath = ConvertContentUriToPath(paths[0]);
                    onFolderSelected?.Invoke(realPath);
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
        /// 泛型保存 JSON 数据到指定的虚拟路径
        /// </summary>
        public static void SaveJson<T>(string path, T data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
                if (file != null)
                {
                    file.StoreString(json);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[FileIOManager] SaveJson failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 泛型从指定的虚拟路径读取 JSON 数据
        /// </summary>
        public static T LoadJson<T>(string path)
        {
            try
            {
                if (FileAccess.FileExists(path))
                {
                    using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                    if (file != null)
                    {
                        string json = file.GetAsText();
                        return JsonSerializer.Deserialize<T>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[FileIOManager] LoadJson failed: {ex.Message}");
            }
            return default;
        }

        /// <summary>
        /// 泛型保存数据为压缩二进制格式
        /// </summary>
        public static void SaveBinary<T>(string path, T data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                
                // 使用 Zstd 压缩保存，既是二进制也减小体积
                using var file = FileAccess.OpenCompressed(path, FileAccess.ModeFlags.Write, FileAccess.CompressionMode.Zstd);
                if (file != null)
                {
                    file.StoreBuffer(bytes);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[FileIOManager] SaveBinary failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 泛型从压缩二进制格式读取数据
        /// </summary>
        public static T LoadBinary<T>(string path)
        {
            try
            {
                if (FileAccess.FileExists(path))
                {
                    using var file = FileAccess.OpenCompressed(path, FileAccess.ModeFlags.Read, FileAccess.CompressionMode.Zstd);
                    if (file != null)
                    {
                        byte[] bytes = file.GetBuffer((long)file.GetLength());
                        string json = System.Text.Encoding.UTF8.GetString(bytes);
                        return JsonSerializer.Deserialize<T>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[FileIOManager] LoadBinary failed: {ex.Message}");
            }
            return default;
        }

        /// <summary>
        /// 将安卓 SAF 返回的 content:// URI 转换为真实文件系统路径
        /// 支持 /tree/ 和 /document/ 格式
        /// </summary>
        private static string ConvertContentUriToPath(string uri)
        {
            // 如果已经是普通路径，直接返回
            if (!uri.StartsWith("content://"))
                return uri;

            try
            {
                // SAF URI 结尾的编码部分总是我们需要的实体 ID
                // 例如: .../document/primary%3Auma%2Ftest
                int lastSlash = uri.LastIndexOf('/');
                if (lastSlash < 0) return uri;

                string encoded = uri.Substring(lastSlash + 1);
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
                else if (storageId.Equals("image", StringComparison.OrdinalIgnoreCase) || 
                         storageId.Equals("video", StringComparison.OrdinalIgnoreCase) || 
                         storageId.Equals("audio", StringComparison.OrdinalIgnoreCase) ||
                         storageId.Equals("msf", StringComparison.OrdinalIgnoreCase))
                {
                    // MediaStore 媒体库对应的 URI，直接返回以便给 Godot.FileAccess 使用
                    return uri;
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
