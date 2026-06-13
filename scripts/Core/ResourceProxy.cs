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
        /// 从项目文件夹（外部文件）加载音频流，支持 mp3/ogg/wav
        /// 不能使用 GD.Load，外部音频文件必须通过 FileAccess 手动解析
        /// </summary>
        public static AudioStream LoadAudioFromProject(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            // 使用 / 拼接 Godot 内部路径，避免 Path.Combine 在 Windows 产生反斜杠
            string path = ProjectManager.IsProjectOpened
                ? $"{ProjectManager.AudioDir}/{fileName}"
                : $"res://audio/{fileName}";

            return LoadAudioFromAbsPath(path, fileName);
        }

        /// <summary>
        /// 从绝对文件系统路径或 res:// 路径加载音频流
        /// </summary>
        public static AudioStream LoadAudioFromAbsPath(string fullPath, string fileName)
        {
            if (!Godot.FileAccess.FileExists(fullPath))
            {
                GD.PrintErr($"[ResourceProxy] 音频文件不存在: {fullPath}");
                return null;
            }

            try
            {
                using var fa = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read);
                byte[] data = fa.GetBuffer((long)fa.GetLength());
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
        /// 从项目背景目录加载图片为 ImageTexture
        /// </summary>
        public static ImageTexture LoadBackgroundTexture(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            string path = ProjectManager.IsProjectOpened
                ? $"{ProjectManager.BackgroundDir}/{fileName}"
                : $"res://backgrounds/{fileName}";

            return LoadImageTexture(path);
        }

        /// <summary>
        /// 从任意路径加载图片为 ImageTexture（自动识别格式）
        /// </summary>
        public static ImageTexture LoadImageTexture(string fullPath)
        {
            if (!Godot.FileAccess.FileExists(fullPath))
            {
                GD.PrintErr($"[ResourceProxy] 图片文件不存在: {fullPath}");
                return null;
            }

            try
            {
                using var fa = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read);
                byte[] data = fa.GetBuffer((long)fa.GetLength());
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
        /// 从项目立绘目录加载立绘图片
        /// </summary>
        public static ImageTexture LoadSpriteTexture(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            string path = ProjectManager.IsProjectOpened
                ? $"{ProjectManager.SpriteDir}/{fileName}"
                : $"res://sprites/{fileName}";

            return LoadImageTexture(path);
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
