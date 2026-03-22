using Godot;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Json;
using FileAccess = Godot.FileAccess;
using DirAccess = Godot.DirAccess;

public class ProjectMetadata
{
    public string Title { get; set; } = "新剧情项目";
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "Unknown";
    public string Description { get; set; } = "";
    public string LastModified { get; set; } = System.DateTime.Now.ToString();
}

public static class ProjectManager
{
    public static string CurrentProjectRoot { get; private set; } = "";
    public static ProjectMetadata Metadata { get; private set; } = new ProjectMetadata();

    public static string ProjectFileName => "project.uma";
    public static string StoryFile => CurrentProjectRoot.PathJoin("story.json");
    public static string CharacterFile => CurrentProjectRoot.PathJoin("characters.json");
    
    public static string AudioDir => CurrentProjectRoot.PathJoin("audio");
    public static string BackgroundDir => CurrentProjectRoot.PathJoin("backgrounds");
    public static string SpriteDir => CurrentProjectRoot.PathJoin("sprites");

    public static bool IsProjectOpened => !string.IsNullOrEmpty(CurrentProjectRoot) && DirAccess.DirExistsAbsolute(CurrentProjectRoot);

    public static void EnsureDir(string subDir)
    {
        using var dir = DirAccess.Open(CurrentProjectRoot);
        if (dir != null)
        {
            if (!dir.DirExists(subDir))
            {
                Error err = dir.MakeDir(subDir);
                if (err != Error.Ok)
                {
                    GD.PrintErr($"Failed to create directory {subDir}: {err}");
                }
            }
        }
        else
        {
            DirAccess.MakeDirRecursiveAbsolute(CurrentProjectRoot.PathJoin(subDir));
        }
    }

    public static void CreateNewProject(string folderPath)
    {
        CurrentProjectRoot = folderPath;
        
        EnsureDir("audio");
        EnsureDir("backgrounds");
        EnsureDir("sprites");
        
        Metadata = new ProjectMetadata { Title = CurrentProjectRoot.GetFile() };
        SaveMetadata();

        WriteAllText(StoryFile, "[]");
        WriteAllText(CharacterFile, "[]");
        
        GD.Print($"Project Created and Initialized at: {CurrentProjectRoot}");
    }

    public static bool OpenProject(string umaFilePath)
    {
        if (!FileAccess.FileExists(umaFilePath)) return false;

        CurrentProjectRoot = umaFilePath.GetBaseDir();
        
        try {
            using var file = FileAccess.Open(umaFilePath, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
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
        WriteAllText(CurrentProjectRoot.PathJoin(ProjectFileName), json);
    }

    public static string ImportFile(string sourcePath, string targetSubDir)
    {
        if (!IsProjectOpened) {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode<ErrorNotifier>("ErrorNotifier").ShowToast("Import failed: Project not opened.");
            return "";
        }
        
        EnsureDir(targetSubDir);
        string targetDir = CurrentProjectRoot.PathJoin(targetSubDir);
        
        string fileName = sourcePath.GetFile();
        string destPath = targetDir.PathJoin(fileName);
        
        Error err = DirAccess.CopyAbsolute(sourcePath, destPath);
        if (err == Error.Ok) {
            return fileName;
        } else {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode<ErrorNotifier>("ErrorNotifier").ShowToast($"Import Error: {err}");
            return "";
        }
    }

    public static void ExportAsEra(string destinationPath)
    {
        if (!IsProjectOpened) return;

        var packer = new PckPacker();
        packer.PckStart(destinationPath, 32);

        AddDirectoryToPacker(packer, CurrentProjectRoot, "");

        packer.Flush();
        GD.Print($"Project Exported as ERA Package: {destinationPath}");
    }

    private static void AddDirectoryToPacker(PckPacker packer, string rootDir, string subDir)
    {
        string currentPath = string.IsNullOrEmpty(subDir) ? rootDir : rootDir.PathJoin(subDir);
        
        using var dir = DirAccess.Open(currentPath);
        if (dir == null) return;

        foreach (string file in dir.GetFiles())
        {
            if (file.StartsWith(".") || file.EndsWith(".tmp")) continue;

            string internalPath = "res://" + (string.IsNullOrEmpty(subDir) ? file : subDir.PathJoin(file).Replace("\\", "/"));
            packer.AddFile(internalPath, currentPath.PathJoin(file));
        }

        foreach (string d in dir.GetDirectories())
        {
            if (d.StartsWith(".")) continue;
            AddDirectoryToPacker(packer, rootDir, string.IsNullOrEmpty(subDir) ? d : subDir.PathJoin(d));
        }
    }

    public static void ExportProject(string destinationZipPath)
    {
        if (!IsProjectOpened) return;

        try
        {
            if (FileAccess.FileExists(destinationZipPath))
            {
                DirAccess.RemoveAbsolute(destinationZipPath);
            }
            // ZipFile requires global paths
            string globalSource = ProjectSettings.GlobalizePath(CurrentProjectRoot);
            string globalDest = ProjectSettings.GlobalizePath(destinationZipPath);
            ZipFile.CreateFromDirectory(globalSource, globalDest, CompressionLevel.Optimal, false);
            GD.Print($"Project Exported Successfully to: {destinationZipPath}");
        }
        catch (System.Exception ex)
        {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode<ErrorNotifier>("ErrorNotifier").ShowErrorDialog("导出失败", $"Export Project Error: {ex.Message}");
        }
    }

    private static void WriteAllText(string path, string content)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(content);
        }
    }
}
