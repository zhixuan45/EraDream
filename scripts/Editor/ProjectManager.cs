using Godot;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class ProjectMetadata
{
    public string Title { get; set; } = "新剧情项目";
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "Unknown";
    public string LastModified { get; set; } = System.DateTime.Now.ToString();
}

public static class ProjectManager
{
    public static string CurrentProjectRoot { get; private set; } = "";
    public static ProjectMetadata Metadata { get; private set; } = new ProjectMetadata();

    public static string ProjectFileName => "project.uma";
    public static string StoryFile => Path.Combine(CurrentProjectRoot, "story.json");
    public static string CharacterFile => Path.Combine(CurrentProjectRoot, "characters.json");
    
    public static string AudioDir => Path.Combine(CurrentProjectRoot, "audio");
    public static string BackgroundDir => Path.Combine(CurrentProjectRoot, "backgrounds");
    public static string SpriteDir => Path.Combine(CurrentProjectRoot, "sprites");

    public static bool IsProjectOpened => !string.IsNullOrEmpty(CurrentProjectRoot) && Directory.Exists(CurrentProjectRoot);

    public static void CreateNewProject(string folderPath)
    {
        // 确保路径是绝对路径并规范化
        CurrentProjectRoot = Path.GetFullPath(ProjectSettings.GlobalizePath(folderPath));
        
        Directory.CreateDirectory(AudioDir);
        Directory.CreateDirectory(BackgroundDir);
        Directory.CreateDirectory(SpriteDir);
        
        Metadata = new ProjectMetadata { Title = Path.GetFileName(CurrentProjectRoot) };
        SaveMetadata();

        File.WriteAllText(StoryFile, "[]");
        File.WriteAllText(CharacterFile, "[]");
        
        GD.Print($"Project Created and Initialized at: {CurrentProjectRoot}");
    }

    public static bool OpenProject(string umaFilePath)
    {
        string absolutePath = Path.GetFullPath(ProjectSettings.GlobalizePath(umaFilePath));
        if (!File.Exists(absolutePath)) return false;

        CurrentProjectRoot = Path.GetDirectoryName(absolutePath);
        
        try {
            string json = File.ReadAllText(absolutePath);
            Metadata = JsonSerializer.Deserialize<ProjectMetadata>(json) ?? new ProjectMetadata();
            GD.Print($"Project Loaded: {Metadata.Title} at {CurrentProjectRoot}");
            return true;
        } catch {
            return false;
        }
    }

    public static void SaveMetadata()
    {
        if (!IsProjectOpened) return;
        Metadata.LastModified = System.DateTime.Now.ToString();
        string json = JsonSerializer.Serialize(Metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(CurrentProjectRoot, ProjectFileName), json);
    }

    public static string ImportFile(string sourcePath, string targetSubDir)
    {
        if (!IsProjectOpened) {
            GD.PrintErr("Import failed: Project not opened.");
            return "";
        }
        
        string absoluteTargetDir = ProjectSettings.GlobalizePath(Path.Combine(CurrentProjectRoot, targetSubDir));
        Directory.CreateDirectory(absoluteTargetDir);
        
        string fileName = Path.GetFileName(sourcePath);
        string destPath = Path.Combine(absoluteTargetDir, fileName);
        
        try {
            File.Copy(sourcePath, destPath, true);
            return fileName;
        } catch (System.Exception e) {
            GD.PrintErr($"Import Error: {e.Message}");
            return "";
        }
    }
}
