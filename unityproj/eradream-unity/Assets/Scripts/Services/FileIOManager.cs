using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using UnityEngine;

namespace EraDream.Services
{
    // 跨平台文件 IO 与 .era 工程打包/解包管理器
    public static class FileIOManager
    {
        /// <summary>
        /// 将指定工程目录打包压缩为 .era / .zip 格式
        /// </summary>
        public static bool ExportProjectPack(string sourceDir, string destinationZipPath)
        {
            try
            {
                if (!Directory.Exists(sourceDir))
                {
                    Debug.LogError($"[FileIOManager] 源目录不存在: {sourceDir}");
                    return false;
                }

                if (File.Exists(destinationZipPath))
                {
                    File.Delete(destinationZipPath);
                }

                ZipFile.CreateFromDirectory(sourceDir, destinationZipPath, CompressionLevel.Optimal, false);
                Debug.Log($"[FileIOManager] 成功打包导出至: {destinationZipPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileIOManager] 打包导出失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 解压 .era / .zip 包至解压目标目录
        /// </summary>
        public static bool ImportProjectPack(string zipPath, string extractToDir)
        {
            try
            {
                if (!File.Exists(zipPath))
                {
                    Debug.LogError($"[FileIOManager] 文件不存在: {zipPath}");
                    return false;
                }

                if (Directory.Exists(extractToDir))
                {
                    Directory.Delete(extractToDir, true);
                }
                Directory.CreateDirectory(extractToDir);

                ZipFile.ExtractToDirectory(zipPath, extractToDir);
                Debug.Log($"[FileIOManager] 成功解压至: {extractToDir}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileIOManager] 解压导入失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 安全序列化并写入 JSON 文件
        /// </summary>
        public static bool SaveJson<T>(string filePath, T data)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileIOManager] 保存 JSON 失败 ({filePath}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 安全反序列化并读取 JSON 文件
        /// </summary>
        public static T LoadJson<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return default;
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileIOManager] 读取 JSON 失败 ({filePath}): {ex.Message}");
                return default;
            }
        }
    }
}
