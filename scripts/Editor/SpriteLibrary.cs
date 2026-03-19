using Godot;
using System.Collections.Generic;
using System.IO;

public static class SpriteLibrary
{
    public static List<string> GetSpriteFileList()
    {
        var files = new List<string>();
        string path = ProjectManager.IsProjectOpened ? ProjectManager.SpriteDir : "res://sprites/";
        string absolutePath = ProjectSettings.GlobalizePath(path);

        if (!Directory.Exists(absolutePath)) return files;

        foreach (string file in Directory.GetFiles(absolutePath))
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext == ".png" || ext == ".webp")
            {
                files.Add(Path.GetFileName(file));
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
