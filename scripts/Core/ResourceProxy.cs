using Godot;
using System;
using System.IO;

namespace EraDream.Core
{
    /// <summary>
    /// 统一的资源调用代理，集中处理 res:// 与用户外部文件的路径解析
    /// 解决 GD.Load(res://...) 与项目外部文件混用导致的加载错误
    /// </summary>
    public static class ResourceProxy
    {
        // ========== Shader ==========

        /// <summary>
        /// 安全加载 Shader，如果路径不存在则返回 null（不抛异常）
        /// </summary>
        public static Shader LoadShader(string resPath)
        {
            if (!ResourceLoader.Exists(resPath)) return null;
            return ResourceLoader.Load<Shader>(resPath);
        }

        /// <summary>
        /// 加载 blur overlay shader，自动适配项目内实际路径
        /// </summary>
        public static Shader LoadBlurOverlayShader()
        {
            // 优先使用 resources 目录，降级到 Shaders 目录
            string[] candidates = {
                "res://resources/shaders/blur_overlay.gdshader",
                "res://Shaders/blur_shader.gdshader",
                "res://Shaders/blur_glass_shader.gdshader",
            };
            foreach (string path in candidates)
            {
                if (ResourceLoader.Exists(path))
                    return ResourceLoader.Load<Shader>(path);
            }
            GD.PrintErr("[ResourceProxy] 未找到 blur overlay shader，遮罩层将无模糊效果");
            return null;
        }

        // ========== 音频 ==========

        /// <summary>
        /// 从项目文件夹（外部文件）加载音频流，支持 mp3/ogg/wav，自动识别扩展包音频
        /// 不能使用 GD.Load，外部音频文件必须通过 FileAccess 手动解析
        /// </summary>
        public static AudioStream LoadAudioFromProject(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            // 优先解析扩展包相对路径中的音频，避免硬编码物理路径丢失
            string resolvedPath = TryResolveExtensionResource(fileName);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                return LoadAudioFromAbsPath(resolvedPath, fileName);
            }

            string path = ProjectManager.IsProjectOpened
                ? $"{ProjectManager.AudioDir}/{fileName}"
                : $"res://audio/{fileName}";

            return LoadAudioFromAbsPath(path, fileName);
        }

        /// <summary>
        /// 从绝对文件系统路径或 res:// 路径加载音频流（支持物理绝对路径与虚拟路径）
        /// </summary>
        public static AudioStream LoadAudioFromAbsPath(string fullPath, string fileName)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;

            bool isGodotPath = fullPath.StartsWith("res://") || fullPath.StartsWith("user://");
            if (isGodotPath)
            {
                if (!Godot.FileAccess.FileExists(fullPath))
                {
                    GD.PrintErr($"[ResourceProxy] 音频文件不存在: {fullPath}");
                    return null;
                }
            }
            else
            {
                if (!System.IO.File.Exists(fullPath))
                {
                    GD.PrintErr($"[ResourceProxy] 音频物理文件不存在: {fullPath}");
                    return null;
                }
            }

            try
            {
                byte[] data;
                if (isGodotPath)
                {
                    using var fa = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read);
                    data = fa.GetBuffer((long)fa.GetLength());
                }
                else
                {
                    // 物理绝对路径时，通过 System.IO 进行读取，避免 Godot 虚拟系统限制。
                    data = System.IO.File.ReadAllBytes(fullPath);
                }

                string ext = System.IO.Path.GetExtension(fileName).ToLower();

                return ext switch
                {
                    ".mp3" => new AudioStreamMP3 { Data = data },
                    ".ogg" => AudioStreamOggVorbis.LoadFromBuffer(data),
                    ".wav" => CreateWav(data),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ResourceProxy] 加载音频失败: {fullPath} => {ex.Message}");
                return null;
            }
        }

        // ========== 背景图 ==========

        /// <summary>
        /// 从项目背景目录加载图片为 ImageTexture，自动识别扩展包背景图
        /// </summary>
        public static ImageTexture LoadBackgroundTexture(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            // 优先解析扩展包相对路径中的背景图资产，避免硬编码物理路径丢失
            string resolvedPath = TryResolveExtensionResource(fileName);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                return LoadImageTexture(resolvedPath);
            }

            string path = ProjectManager.IsProjectOpened
                ? $"{ProjectManager.BackgroundDir}/{fileName}"
                : $"res://backgrounds/{fileName}";

            return LoadImageTexture(path);
        }

        /// <summary>
        /// 从任意路径加载图片为 ImageTexture（自动识别格式，支持操作系统物理绝对路径）
        /// </summary>
        public static ImageTexture LoadImageTexture(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;

            // 智能根据当前播放的剧本路径尝试解析扩展包全局演出资源
            string resolvedPath = TryResolveExtensionResource(fullPath);
            if (!string.IsNullOrEmpty(resolvedPath))
            {
                fullPath = resolvedPath;
            }

            bool isGodotPath = fullPath.StartsWith("res://") || fullPath.StartsWith("user://");
            if (isGodotPath)
            {
                if (!Godot.FileAccess.FileExists(fullPath))
                {
                    GD.PrintErr($"[ResourceProxy] 图片文件不存在: {fullPath}");
                    return null;
                }
            }
            else
            {
                if (!System.IO.File.Exists(fullPath))
                {
                    GD.PrintErr($"[ResourceProxy] 图片物理文件不存在: {fullPath}");
                    return null;
                }
            }

            try
            {
                byte[] data;
                if (isGodotPath)
                {
                    using var fa = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read);
                    data = fa.GetBuffer((long)fa.GetLength());
                }
                else
                {
                    // 物理绝对路径时，通过 System.IO 进行读取，避免 Godot 虚拟系统限制。
                    data = System.IO.File.ReadAllBytes(fullPath);
                }

                string ext = System.IO.Path.GetExtension(fullPath).ToLower();
                var image = new Image();
                Error err = ext switch
                {
                    ".jpg" or ".jpeg" => image.LoadJpgFromBuffer(data),
                    ".webp" => image.LoadWebpFromBuffer(data),
                    _ => image.LoadPngFromBuffer(data)
                };

                // 格式识别失败时轮流尝试
                if (err != Error.Ok) err = image.LoadJpgFromBuffer(data);
                if (err != Error.Ok) err = image.LoadPngFromBuffer(data);
                if (err != Error.Ok) err = image.LoadWebpFromBuffer(data);

                if (err == Error.Ok)
                    return ImageTexture.CreateFromImage(image);

                GD.PrintErr($"[ResourceProxy] 无法解析图片: {fullPath}");
                return null;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ResourceProxy] 加载图片异常: {fullPath} => {ex.Message}");
                return null;
            }
        }

        // ========== 立绘 ==========

        /// <summary>
        /// 从项目立绘目录加载立绘图片（自动识别扩展包马娘角色立绘定位）
        /// </summary>
        public static ImageTexture LoadSpriteTexture(string fileName, string actorId = null)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            // 优先在路径字典中查找该马娘所在的扩展包物理根路径，并拼接绝对路径
            if (!string.IsNullOrEmpty(actorId) &&
                CharacterManager.ActorToExtensionPathMap.TryGetValue(actorId, out string extRoot))
            {
                string absPath = System.IO.Path.Combine(extRoot, fileName);
                return LoadImageTexture(absPath);
            }

            string path = ProjectManager.IsProjectOpened
                ? $"{ProjectManager.SpriteDir}/{fileName}"
                : $"res://sprites/{fileName}";

            return LoadImageTexture(path);
        }

        // ========== 扩展包资源解析辅助 ==========

        /// <summary>
        /// 尝试根据当前播放的剧本路径，解析并拼接扩展包内相对演出资源的物理绝对路径
        /// </summary>
        private static string TryResolveExtensionResource(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            // 如果已经是绝对物理路径，或者是以 res:// 或 user:// 开头的 Godot 虚拟路径，则无需处理
            if (fileName.StartsWith("res://") || fileName.StartsWith("user://") || System.IO.Path.IsPathRooted(fileName))
            {
                return null;
            }

            string currentStoryPath = "";
            try
            {
                // 用反射或直接访问 StoryPlayerEngine.CurrentStoryPath，避免编译期静态引用循环
                currentStoryPath = StoryPlayerEngine.CurrentStoryPath;
            }
            catch {}

            if (string.IsNullOrEmpty(currentStoryPath)) return null;

            // 检查当前剧本是否在 Story 目录下（扩展包的剧本存放于 Story 目录下，上一级即为扩展包根目录）
            string globalizedStoryPath = ProjectSettings.GlobalizePath(currentStoryPath);
            string storyDir = System.IO.Path.GetDirectoryName(globalizedStoryPath);
            if (string.IsNullOrEmpty(storyDir)) return null;

            string extRoot = System.IO.Path.GetDirectoryName(storyDir);
            if (string.IsNullOrEmpty(extRoot)) return null;

            string candidatePath = System.IO.Path.Combine(extRoot, fileName);
            if (System.IO.File.Exists(candidatePath))
            {
                return candidatePath;
            }

            return null;
        }

        // ========== 工具方法 ==========

        private static AudioStreamWav CreateWav(byte[] data)
        {
            var wav = new AudioStreamWav();
            wav.Data = data;
            return wav;
        }
    }
}
