using System;
using System.IO;
using UnityEngine;
using EraDream.Services;

namespace EraDream.StoryEditor
{
    // 剧情工程管理者 (创建、保存、加载、导出 .era 包)
    public class ProjectManager : MonoBehaviour
    {
        public static ProjectManager Instance { get; private set; }

        public string CurrentProjectPath { get; private set; } = "";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void CreateNewProject(string projectDirName)
        {
            string baseDir = Path.Combine(Application.persistentDataPath, "Projects");
            string projectPath = Path.Combine(baseDir, projectDirName);

            if (!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
                Directory.CreateDirectory(Path.Combine(projectPath, "Backgrounds"));
                Directory.CreateDirectory(Path.Combine(projectPath, "Sprites"));
                Directory.CreateDirectory(Path.Combine(projectPath, "Audio"));
            }

            CurrentProjectPath = projectPath;
            Debug.Log($"[ProjectManager] 新建工程目录成功: {projectPath}");
        }

        public bool ExportEraPackage(string outputFilePath)
        {
            if (string.IsNullOrEmpty(CurrentProjectPath) || !Directory.Exists(CurrentProjectPath))
            {
                Debug.LogError("[ProjectManager] 当前未打开任何有效工程目录!");
                return false;
            }

            return FileIOManager.ExportProjectPack(CurrentProjectPath, outputFilePath);
        }

        public bool ImportEraPackage(string eraFilePath, string extractProjectName)
        {
            string baseDir = Path.Combine(Application.persistentDataPath, "Projects");
            string extractPath = Path.Combine(baseDir, extractProjectName);

            bool success = FileIOManager.ImportProjectPack(eraFilePath, extractPath);
            if (success)
            {
                CurrentProjectPath = extractPath;
            }
            return success;
        }
    }
}
