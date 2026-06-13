using Godot;
using System.Collections.Generic;

public static class AudioLibrary
{
    /// <summary>
    /// 获取所有可用的音频文件列表 (项目目录下)
    /// </summary>
    public static List<string> GetAudioFileList()
    {
        var files = new List<string>();
        string path = ProjectManager.IsProjectOpened ? ProjectManager.AudioDir : "res://audio/";
        
        if (!DirAccess.DirExistsAbsolute(path)) {
            // 如果是 res:// 路径，在运行时通常是只读的，不建议创建目录
            if (!path.StartsWith("res://") && ProjectManager.IsProjectOpened) {
                ProjectManager.EnsureDir("audio");
            }
            return files;
        }

        using var dir = DirAccess.Open(path);
        if (dir != null)
        {
            foreach (string file in dir.GetFiles())
            {
                string ext = file.GetExtension().ToLower();
                if (ext == "ogg" || ext == "mp3" || ext == "wav")
                {
                    files.Add(file);
                }
            }
        }
        return files;
    }

    public static void PopulateOptionButton(OptionButton btn, string currentSelection)
    {
        btn.Clear();
        var files = GetAudioFileList();
        
        if (files.Count == 0)
        {
            btn.AddItem("⚠️ No Audio Files Found!");
            btn.Disabled = true;
            return;
        }

        btn.Disabled = false;
        btn.AddItem("-- Select Audio --");
        foreach (var file in files)
        {
            btn.AddItem(file);
            if (file == currentSelection)
                btn.Selected = btn.GetItemCount() - 1;
        }
    }
}
