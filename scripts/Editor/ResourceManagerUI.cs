using Godot;
using System;
using System.IO;
using UmaArchive.Core;

public static class ResourceManagerUI
{
    public enum ResourceType { Background, Audio, Sprite }

    public static void OpenImportDialog(ResourceType type)
    {
        if (!ProjectManager.IsProjectOpened)
        {
            GD.PrintErr("Must open a project before importing resources.");
            return;
        }

        string title = "导入资源";
        string filter = "";
        string targetSubDir = "";

        switch (type)
        {
            case ResourceType.Background:
                title = "导入背景图";
                filter = "*.png,*.jpg,*.webp";
                targetSubDir = "backgrounds";
                break;
            case ResourceType.Audio:
                title = "导入音频文件";
                filter = "*.mp3,*.ogg,*.wav";
                targetSubDir = "audio";
                break;
            case ResourceType.Sprite:
                title = "导入角色立绘";
                filter = "*.png,*.webp";
                targetSubDir = "sprites";
                break;
        }

        FileIOManager.OpenLoadDialog(title, filter, (sourcePath) => {
            ImportProcess(sourcePath, targetSubDir);
        });
    }

    private static void ImportProcess(string sourcePath, string targetSubDir)
    {
        // 确保路径规范
        string absoluteSource = ProjectSettings.GlobalizePath(sourcePath);
        string fileName = ProjectManager.ImportFile(absoluteSource, targetSubDir);

        if (!string.IsNullOrEmpty(fileName))
        {
            GD.Print($"Successfully imported: {fileName} to {targetSubDir}");
        }
        else
        {
            GD.PrintErr($"Failed to import: {sourcePath}");
        }
    }
}
