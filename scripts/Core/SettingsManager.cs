using Godot;
using System;
using System.Text.Json;

public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    private const string SettingsFilePath = "user://settings.json";
    private AppSettings _currentSettings = new AppSettings();

    public event Action<bool> OnDarkModeChanged;

    public bool IsDarkMode
    {
        get => _currentSettings.IsDarkMode;
        set
        {
            if (_currentSettings.IsDarkMode != value)
            {
                _currentSettings.IsDarkMode = value;
                OnDarkModeChanged?.Invoke(value);
                SaveSettings();
            }
        }
    }

    public bool IsEmbeddedSubwindows
    {
        get => _currentSettings.IsEmbeddedSubwindows;
        set
        {
            if (_currentSettings.IsEmbeddedSubwindows != value)
            {
                _currentSettings.IsEmbeddedSubwindows = value;
                ApplyWindowSettings();
                SaveSettings();
            }
        }
    }

    public override void _Ready()
    {
        Instance = this;
        LoadSettings();
        // 延迟调用以确保树已准备好
        CallDeferred(MethodName.ApplyWindowSettings);
    }

    private void ApplyWindowSettings()
    {
        GetTree().Root.GuiEmbedSubwindows = _currentSettings.IsEmbeddedSubwindows;
    }

    private void LoadSettings()
    {
        try
        {
            if (FileAccess.FileExists(SettingsFilePath))
            {
                using var file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    _currentSettings = loaded;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to load settings: {ex.Message}");
        }
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
            using var file = FileAccess.Open(SettingsFilePath, FileAccess.ModeFlags.Write);
            file.StoreString(json);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to save settings: {ex.Message}");
        }
    }
}
