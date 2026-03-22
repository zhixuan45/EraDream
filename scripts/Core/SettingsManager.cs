using Godot;
using System;
using System.Text.Json;

public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    private const string SettingsFilePath = "user://settings.json";
    private AppSettings _currentSettings = new AppSettings();

    public event Action<bool> OnDarkModeChanged;
    public event Action<float> OnSafeAreaPaddingChanged;

    public float SafeAreaPadding
    {
        get => _currentSettings.SafeAreaPadding;
        set
        {
            if (_currentSettings.SafeAreaPadding != value)
            {
                _currentSettings.SafeAreaPadding = value;
                OnSafeAreaPaddingChanged?.Invoke(value);
                SaveSettings();
            }
        }
    }

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

    public bool ShowMouseCursor
    {
        get => _currentSettings.ShowMouseCursor;
        set
        {
            if (_currentSettings.ShowMouseCursor != value)
            {
                _currentSettings.ShowMouseCursor = value;
                ApplyMouseSettings();
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
        CallDeferred(MethodName.ApplyMouseSettings);
    }

    private void ApplyMouseSettings()
    {
        Input.MouseMode = _currentSettings.ShowMouseCursor ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Hidden;
    }

    private void ApplyWindowSettings()
    {
        string osName = OS.GetName();
        if (osName == "Android" || osName == "iOS")
        {
            GetTree().Root.GuiEmbedSubwindows = true;
        }
        else
        {
            GetTree().Root.GuiEmbedSubwindows = _currentSettings.IsEmbeddedSubwindows;
        }
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
