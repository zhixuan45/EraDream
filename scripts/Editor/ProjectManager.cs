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
    public static string StickerFile => CurrentProjectRoot.PathJoin("stickers.json");

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
        WriteAllText(StickerFile, "[]");
        
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
        if (sourcePath.StartsWith("content://"))
        {
            fileName = System.Uri.UnescapeDataString(fileName).Replace(":", "_");
            fileName = fileName.GetFile();
        }
        
        // 尝试使用 Godot.FileAccess 读取文件内容 (原生支持 content:// 读取)
        byte[] fileData = FileAccess.GetFileAsBytes(sourcePath);
        if (fileData != null && fileData.Length > 0)
        {
            string ext = System.IO.Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext))
            {
                string detectedExt = GetExtensionFromMagicBytes(fileData);
                if (!string.IsNullOrEmpty(detectedExt))
                {
                    fileName += detectedExt;
                }
            }

            string destPath = targetDir.PathJoin(fileName);
            using var file = FileAccess.Open(destPath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreBuffer(fileData);
                return fileName;
            }
        }

        // 回退逻辑: 直接使用 CopyAbsolute
        string altDestPath = targetDir.PathJoin(fileName);
        Error err = DirAccess.CopyAbsolute(sourcePath, altDestPath);
        if (err == Error.Ok) {
            return fileName;
        } else {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode<ErrorNotifier>("ErrorNotifier").ShowToast($"Import Error: {(fileData == null || fileData.Length == 0 ? "ReadFailed" : err.ToString())}");
            return "";
        }
    }

    private static string GetExtensionFromMagicBytes(byte[] magic)
    {
        if (magic == null || magic.Length < 12) return "";
        // PNG: 89 50 4E 47
        if (magic[0] == 0x89 && magic[1] == 0x50 && magic[2] == 0x4E && magic[3] == 0x47) return ".png";
        // JPG: FF D8 FF
        if (magic[0] == 0xFF && magic[1] == 0xD8 && magic[2] == 0xFF) return ".jpg";
        // WEBP: 52 49 46 46 ... 57 45 42 50
        if (magic[0] == 0x52 && magic[1] == 0x49 && magic[2] == 0x46 && magic[3] == 0x46 &&
            magic[8] == 0x57 && magic[9] == 0x45 && magic[10] == 0x42 && magic[11] == 0x50) return ".webp";
        // OGG: 4F 67 67 53
        if (magic[0] == 0x4F && magic[1] == 0x67 && magic[2] == 0x67 && magic[3] == 0x53) return ".ogg";
        // MP3: ID3 or start with FF FB / FF FA
        if (magic[0] == 0x49 && magic[1] == 0x44 && magic[2] == 0x33) return ".mp3";
        if (magic[0] == 0xFF && (magic[1] == 0xFB || magic[1] == 0xFA || magic[1] == 0xF3 || magic[1] == 0xF2)) return ".mp3";
        // WAV: 52 49 46 46 ... 57 41 56 45
        if (magic[0] == 0x52 && magic[1] == 0x49 && magic[2] == 0x46 && magic[3] == 0x46 &&
            magic[8] == 0x57 && magic[9] == 0x41 && magic[10] == 0x56 && magic[11] == 0x45) return ".wav";
        
        return "";
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
