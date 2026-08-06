using System;
using System.IO;
using System.Text.Json;
using EraDream.Core;
using UnityEngine;

namespace EraDream.Services
{
    // 应用偏好设置管理服务 (JSON 文件存储在 Unity persistentDataPath 下)
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public AppSettings CurrentSettings { get; private set; } = new AppSettings();

        private string SavePath => Path.Combine(Application.persistentDataPath, "settings.json");

        public event Action<AppSettings> OnSettingsChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    CurrentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    CurrentSettings = new AppSettings();
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsManager] 无法读取设置，使用默认配置: {ex.Message}");
                CurrentSettings = new AppSettings();
            }
            ApplySettings();
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsManager] 写入设置失败: {ex.Message}");
            }
            ApplySettings();
        }

        public void ApplySettings()
        {
            Screen.fullScreen = CurrentSettings.IsFullscreen;
            AudioListener.volume = CurrentSettings.MasterVolume;
            OnSettingsChanged?.Invoke(CurrentSettings);
        }
    }
}
