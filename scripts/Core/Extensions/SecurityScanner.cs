using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Godot;

namespace EraDream.Core.Extensions
{
    /// <summary>
    /// 静态 IL 安全扫描器，用于检测 DLL 中的高危调用
    /// </summary>
    public static class SecurityScanner
    {
        // 高危命名空间与类型黑名单
        private static readonly Dictionary<string, string> Blacklist = new()
        {
            { "System.IO", "文件系统访问" },
            { "System.Net", "网络通信" },
            { "System.Diagnostics.Process", "进程管理" },
            { "System.Runtime.InteropServices.DllImportAttribute", "底层 DLL 调用 (P/Invoke)" }
        };

        /// <summary>
        /// 扫描指定的 DLL 文件，寻找黑名单中的引用
        /// </summary>
        /// <param name="dllPath">DLL 绝对路径</param>
        /// <returns>检测到的权限/警告列表</returns>
        public static List<string> Scan(string dllPath)
        {
            if (!File.Exists(dllPath)) return new List<string>();

            try
            {
                using var fs = new System.IO.FileStream(dllPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
                return Scan(fs);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SecurityScanner] Error opening {dllPath}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 扫描 DLL 流
        /// </summary>
        public static List<string> Scan(Stream stream)
        {
            List<string> detectedPermissions = new();
            try
            {
                using var peReader = new PEReader(stream);
                
                if (!peReader.HasMetadata) return detectedPermissions;

                var metadataReader = peReader.GetMetadataReader();
                
                // 1. 扫描类型引用 (TypeReferences)
                foreach (var handle in metadataReader.TypeReferences)
                {
                    var typeRef = metadataReader.GetTypeReference(handle);
                    string ns = metadataReader.GetString(typeRef.Namespace);
                    string name = metadataReader.GetString(typeRef.Name);
                    string fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

                    CheckAndAdd(fullName, detectedPermissions);
                }

                // 2. 扫描成员引用 (MemberReferences)
                foreach (var handle in metadataReader.MemberReferences)
                {
                    var memberRef = metadataReader.GetMemberReference(handle);
                    if (memberRef.Parent.Kind == HandleKind.TypeReference)
                    {
                        var typeRef = metadataReader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
                        string ns = metadataReader.GetString(typeRef.Namespace);
                        string name = metadataReader.GetString(typeRef.Name);
                        string fullName = string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";

                        CheckAndAdd(fullName, detectedPermissions);
                    }
                }

                // 3. 特殊处理 DllImport (在 MethodDefinition 中)
                foreach (var handle in metadataReader.MethodDefinitions)
                {
                    var methodDef = metadataReader.GetMethodDefinition(handle);
                    if ((methodDef.Attributes & System.Reflection.MethodAttributes.PinvokeImpl) != 0)
                    {
                        string warn = Blacklist["System.Runtime.InteropServices.DllImportAttribute"];
                        if (!detectedPermissions.Contains(warn))
                        {
                            detectedPermissions.Add(warn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SecurityScanner] Error scanning stream: {ex.Message}");
            }

            return detectedPermissions;
        }

        private static void CheckAndAdd(string fullName, List<string> detected)
        {
            foreach (var entry in Blacklist)
            {
                if (fullName.StartsWith(entry.Key))
                {
                    if (!detected.Contains(entry.Value))
                    {
                        detected.Add(entry.Value);
                    }
                }
            }
        }
    }
}
