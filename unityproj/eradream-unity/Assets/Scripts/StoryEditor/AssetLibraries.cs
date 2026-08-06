using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EraDream.StoryEditor
{
    // 静态资源扫描辅助工具 (音频、背景图、角色立绘)
    public static class AudioLibrary
    {
        public static List<string> ScanAudioFiles(string directory)
        {
            var list = new List<string>();
            if (!Directory.Exists(directory)) return list;
            string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                string ext = Path.GetExtension(f).ToLower();
                if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                {
                    list.Add(f);
                }
            }
            return list;
        }
    }

    public static class BackgroundLibrary
    {
        public static List<string> ScanBackgroundFiles(string directory)
        {
            var list = new List<string>();
            if (!Directory.Exists(directory)) return list;
            string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                string ext = Path.GetExtension(f).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".webp")
                {
                    list.Add(f);
                }
            }
            return list;
        }
    }

    public static class SpriteLibrary
    {
        public static List<string> ScanSpriteFiles(string directory)
        {
            return BackgroundLibrary.ScanBackgroundFiles(directory);
        }
    }
}
