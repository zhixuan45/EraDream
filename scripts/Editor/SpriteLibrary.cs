using Godot;
using System.Collections.Generic;

public static class SpriteLibrary
{
    public static List<string> GetSpriteFileList()
    {
        var files = new List<string>();
        string path = ProjectManager.IsProjectOpened ? ProjectManager.SpriteDir : "res://sprites/";

        if (!DirAccess.DirExistsAbsolute(path)) return files;

        using var dir = DirAccess.Open(path);
        if (dir != null)
        {
            foreach (string file in dir.GetFiles())
            {
                string ext = file.GetExtension().ToLower();
                if (ext == "png" || ext == "webp")
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
        var files = GetSpriteFileList();
        btn.AddItem("-- 选择立绘 --");
        foreach (var file in files)
        {
            btn.AddItem(file);
            if (file == currentSelection) btn.Selected = btn.GetItemCount() - 1;
        }
    }
}
