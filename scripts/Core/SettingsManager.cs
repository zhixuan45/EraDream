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

    public string LastSavePath
    {
        get => _currentSettings.LastSavePath;
        set
        {
            if (_currentSettings.LastSavePath != value)
            {
                _currentSettings.LastSavePath = value;
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
        var loaded = UmaEraArchive.Core.FileIOManager.LoadJson<AppSettings>(SettingsFilePath);
        if (loaded != null)
        {
            _currentSettings = loaded;
        }
    }

    public void SaveSettings()
    {
        UmaEraArchive.Core.FileIOManager.SaveJson(SettingsFilePath, _currentSettings);
    }
}
