using Godot;
using System.Collections.Generic;
using System.IO;

public static class AudioLibrary
{
    /// <summary>
    /// 获取所有可用的音频文件列表 (项目目录下)
    /// </summary>
    public static List<string> GetAudioFileList()
    {
        var files = new List<string>();
        string path = ProjectManager.IsProjectOpened ? ProjectManager.AudioDir : "res://audio/";
        
        // 关键修复：将 Godot 虚拟路径转换为系统绝对路径
        string absolutePath = ProjectSettings.GlobalizePath(path);

        if (!Directory.Exists(absolutePath)) {
            // 如果是 res:// 路径，在运行时通常是只读的，不建议创建目录
            if (!path.StartsWith("res://")) {
                Directory.CreateDirectory(absolutePath);
            }
            return files;
        }

        foreach (string file in Directory.GetFiles(absolutePath))
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext == ".ogg" || ext == ".mp3" || ext == ".wav")
            {
                files.Add(Path.GetFileName(file));
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
