using Godot;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode<ErrorNotifier>("ErrorNotifier").ShowToast("Import failed: Project not opened.");
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
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode<ErrorNotifier>("ErrorNotifier").ShowToast($"Import Error: {e.Message}");
            return "";
        }
    }

    public static void ExportAsEra(string destinationPath)
    {
        if (!IsProjectOpened) return;

        var packer = new PckPacker();
        // 开始打包，32位对齐
        packer.PckStart(destinationPath, 32);

        // 递归添加项目下所有有效文件
        AddDirectoryToPacker(packer, CurrentProjectRoot, "");

        packer.Flush();
        GD.Print($"Project Exported as ERA Package: {destinationPath}");
    }

    private static void AddDirectoryToPacker(PckPacker packer, string rootDir, string subDir)
    {
        string currentPath = string.IsNullOrEmpty(subDir) ? rootDir : Path.Combine(rootDir, subDir);
        
        // 遍历所有文件
        foreach (string file in Directory.GetFiles(currentPath))
        {
            string fileName = Path.GetFileName(file);
            // 排除系统文件和临时文件
            if (fileName.StartsWith(".") || fileName.EndsWith(".tmp")) continue;

            // 内部路径映射到 res:// 根目录
            string internalPath = "res://" + (string.IsNullOrEmpty(subDir) ? fileName : Path.Combine(subDir, fileName).Replace("\\", "/"));
            packer.AddFile(internalPath, file);
        }

        // 递归处理子目录
        foreach (string dir in Directory.GetDirectories(currentPath))
        {
            string dirName = Path.GetFileName(dir);
            AddDirectoryToPacker(packer, rootDir, string.IsNullOrEmpty(subDir) ? dirName : Path.Combine(subDir, dirName));
        }
    }

    public static void ExportProject(string destinationZipPath)
    {
        if (!IsProjectOpened) return;

        try
        {
            if (File.Exists(destinationZipPath))
            {
                File.Delete(destinationZipPath);
            }
            ZipFile.CreateFromDirectory(CurrentProjectRoot, destinationZipPath, CompressionLevel.Optimal, false);
            GD.Print($"Project Exported Successfully to: {destinationZipPath}");
        }
        catch (System.Exception ex)
        {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode<ErrorNotifier>("ErrorNotifier").ShowErrorDialog("导出失败", $"Export Project Error: {ex.Message}");
        }
    }
}
