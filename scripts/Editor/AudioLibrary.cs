using Godot;
using System.Collections.Generic;
using System.Linq;

public static class AudioLibrary
{
    private static string _audioPath = "res://audio/";

    /// <summary>
    /// 获取所有可用的音频文件列表
    /// </summary>
    public static List<string> GetAudioFileList()
    {
        var files = new List<string>();
        if (!DirAccess.DirExistsAbsolute(_audioPath))
        {
            DirAccess.MakeDirAbsolute(_audioPath); // 自动创建目录
            return files;
        }

        using var dir = DirAccess.Open(_audioPath);
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (!dir.CurrentIsDir() && (fileName.EndsWith(".ogg") || fileName.EndsWith(".mp3") || fileName.EndsWith(".wav")))
                {
                    files.Add(fileName);
                }
                fileName = dir.GetNext();
            }
        }
        return files;
    }

    /// <summary>
    /// 统一的下拉框填充逻辑
    /// </summary>
    public static void PopulateOptionButton(OptionButton btn, string currentSelection)
    {
        btn.Clear();
        var files = GetAudioFileList();
        
        if (files.Count == 0)
        {
            btn.AddItem("⚠️ No Audio Files Found! (Please put files in res://audio/)");
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
