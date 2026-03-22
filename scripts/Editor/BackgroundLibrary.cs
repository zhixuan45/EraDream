using Godot;
using System.Collections.Generic;

public static class BackgroundLibrary
{
    /// <summary>
    /// 获取所有可用的背景图片文件列表 (项目目录下)
    /// </summary>
    public static List<string> GetBackgroundFileList()
    {
        var files = new List<string>();
        string path = ProjectManager.IsProjectOpened ? ProjectManager.BackgroundDir : "res://backgrounds/";
        
        if (!DirAccess.DirExistsAbsolute(path)) {
            if (!path.StartsWith("res://") && ProjectManager.IsProjectOpened) {
                ProjectManager.EnsureDir("backgrounds");
            }
            return files;
        }

        using var dir = DirAccess.Open(path);
        if (dir != null)
        {
            foreach (string file in dir.GetFiles())
            {
                string ext = file.GetExtension().ToLower();
                if (ext == "png" || ext == "jpg" || ext == "webp")
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
        var files = GetBackgroundFileList();
        
        if (files.Count == 0)
        {
            btn.AddItem("⚠️ No Backgrounds Found!");
            btn.Disabled = true;
            return;
        }

        btn.Disabled = false;
        btn.AddItem("-- Select Background --");
        foreach (var file in files)
        {
            btn.AddItem(file);
            if (file == currentSelection)
                btn.Selected = btn.GetItemCount() - 1;
        }
    }
}
